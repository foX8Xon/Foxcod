using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FoxCod.Core
{
    public sealed class FoxEngine
    {
        public FoxEngine(Action<string> log, Func<int, bool>? clickCallback = null)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            ClickCallback = clickCallback;
        }

        public Action<string> Log { get; }
        private Func<int, bool>? ClickCallback { get; }

        public bool ExecuteScript(string scriptPath, bool requireClick = true)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                Log("[Error] Путь к скрипту не указан");
                return false;
            }

            if (!scriptPath.EndsWith(".fc", StringComparison.OrdinalIgnoreCase))
            {
                Log("[Error] FoxCod принимает только расширение .fc");
                return false;
            }

            if (!File.Exists(scriptPath))
            {
                Log($"[Error] Файл {scriptPath} не найден");
                return false;
            }

            var lines = File.ReadAllLines(scriptPath);
            Log($"[Info] Выполнение скрипта {Path.GetFileName(scriptPath)} ({lines.Length} строк)");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (!ExecuteCommand(line, i + 1, requireClick))
                    return false;
            }

            Log("[Info] Скрипт выполнен");
            return true;
        }

        private bool ExecuteCommand(string line, int lineNumber, bool requireClick)
        {
            // try to detect two main command shapes: вписать and очистить
            var writeMatch = Regex.Match(line, @"^\s*(?<cmd>\w+)\s+в\s+""(?<file>[^""]+)""(?:\s+@(?:(?:""(?<path>[^""]+)"")|(?<path>[^()]+?)))?\s*\(\s*(?<text>.*?)\s*\)\s*$", RegexOptions.IgnoreCase);
            if (writeMatch.Success)
            {
                var command = writeMatch.Groups["cmd"].Value.ToLowerInvariant().Trim();
                var fileName = writeMatch.Groups["file"].Value.Trim();
                var atPath = writeMatch.Groups["path"].Success ? writeMatch.Groups["path"].Value.Trim() : string.Empty;
                var text = writeMatch.Groups["text"].Value;

                if (command != "вписать")
                {
                    Log($"[Error] Строка {lineNumber}: неизвестная команда {command} (500)");
                    return false;
                }

                // Interpret "/n" marker as a newline directive inside parentheses
                text = text.Replace("/n", Environment.NewLine);

                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(text))
                {
                    Log($"[Error] Строка {lineNumber}: отсутствует имя файла или текст (500)");
                    return false;
                }

                if (requireClick && !WaitForClick(lineNumber))
                {
                    Log($"[Info] Строка {lineNumber}: выполнение отменено пользователем");
                    return false;
                }

                var fullPath = BuildOutputPath(atPath, fileName);
                if (fullPath == null)
                {
                    Log($"[Error] Строка {lineNumber}: некорректный путь вывода (501)");
                    return false;
                }

                try
                {
                    var directory = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    // Read existing to decide separator behavior
                    var separator = string.Empty;
                    if (File.Exists(fullPath))
                    {
                        var existing = File.ReadAllText(fullPath, Encoding.UTF8);
                        if (!string.IsNullOrEmpty(existing))
                        {
                            var lastChar = existing[^1];
                            if (!char.IsWhiteSpace(lastChar) && lastChar != '.' && lastChar != '!' && lastChar != '?')
                                separator = " ";
                            else if (!char.IsWhiteSpace(lastChar))
                                separator = Environment.NewLine;
                        }
                    }

                    File.AppendAllText(fullPath, separator + text, Encoding.UTF8);
                    Log($"[Success] Вписано в {fileName}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"[Error] Строка {lineNumber}: {ex.Message} (501)");
                    return false;
                }
            }

            // очистить command handling
            var clearMatch = Regex.Match(line, @"^\s*(?<cmd>очистить)\s+в\s+""(?<file>[^""]+)""(?:\s+(?<rest>.+))?$", RegexOptions.IgnoreCase);
            if (clearMatch.Success)
            {
                var command = clearMatch.Groups["cmd"].Value.ToLowerInvariant().Trim();
                var fileName = clearMatch.Groups["file"].Value.Trim();
                var rest = clearMatch.Groups["rest"].Success ? clearMatch.Groups["rest"].Value.Trim() : string.Empty;

                if (command != "очистить")
                {
                    Log($"[Error] Строка {lineNumber}: неизвестная команда {command} (500)");
                    return false;
                }

                if (requireClick && !WaitForClick(lineNumber))
                {
                    Log($"[Info] Строка {lineNumber}: выполнение отменено пользователем");
                    return false;
                }

                var fullPath = BuildOutputPath(string.Empty, fileName);
                if (fullPath == null)
                {
                    Log($"[Error] Строка {lineNumber}: некорректный путь вывода (501)");
                    return false;
                }

                try
                {
                    if (!File.Exists(fullPath))
                    {
                        Log($"[Info] Строка {lineNumber}: файл {fileName} не найден, нечего очищать");
                        return true;
                    }

                    var encoding = Encoding.UTF8;
                    var text = File.ReadAllText(fullPath, encoding);

                    // If no rest specified => clear whole file
                    if (string.IsNullOrEmpty(rest))
                    {
                        File.WriteAllText(fullPath, string.Empty, encoding);
                        Log($"[Success] Очистка: файл {fileName} полностью очищен");
                        return true;
                    }

                    // parse different variants
                    var mLineSingle = Regex.Match(rest, @"^строку\s+(?<num>\d+)$", RegexOptions.IgnoreCase);
                    var mLineRange = Regex.Match(rest, @"^строки\s+(?<dir>после|до)\s+(?<num>\d+)$", RegexOptions.IgnoreCase);
                    var mWords = Regex.Match(rest, @"^(?<num>\d+)\s+слова?$", RegexOptions.IgnoreCase);
                    var mSentences = Regex.Match(rest, @"^(?<num>\d+)\s+последних\s+предложений?$", RegexOptions.IgnoreCase);

                    if (mLineSingle.Success)
                    {
                        int num = int.Parse(mLineSingle.Groups["num"].Value);
                        var lines = File.ReadAllLines(fullPath, encoding).ToList();
                        if (num < 1 || num > lines.Count)
                        {
                            Log($"[Error 103] Строка {lineNumber}: неверный номер строки ({num})");
                            return false;
                        }
                        lines.RemoveAt(num - 1);
                        File.WriteAllLines(fullPath, lines, encoding);
                        Log($"[Success] Очистка: удалена строка {num} из {fileName}");
                        return true;
                    }

                    if (mLineRange.Success)
                    {
                        var dir = mLineRange.Groups["dir"].Value.ToLowerInvariant();
                        int num = int.Parse(mLineRange.Groups["num"].Value);
                        var lines = File.ReadAllLines(fullPath, encoding).ToList();
                        if (num < 1 || num > lines.Count)
                        {
                            Log($"[Error 103] Строка {lineNumber}: неверный диапазон ({num})");
                            return false;
                        }
                        if (dir == "после")
                        {
                            if (num < lines.Count)
                                lines.RemoveRange(num, lines.Count - num);
                            else
                                lines.RemoveRange(num, 0);
                        }
                        else // до
                        {
                            if (num > 1)
                                lines.RemoveRange(0, num - 1);
                            else
                                lines.RemoveRange(0, 0);
                        }
                        File.WriteAllLines(fullPath, lines, encoding);
                        Log($"[Success] Очистка: удалены строки {dir} {num} в {fileName}");
                        return true;
                    }

                    if (mWords.Success)
                    {
                        int num = int.Parse(mWords.Groups["num"].Value);
                        if (num < 0)
                        {
                            Log($"[Error 103] Строка {lineNumber}: неверный параметр слов ({num})");
                            return false;
                        }
                        var words = SplitWordsPreserve(text);
                        if (num >= words.Count)
                        {
                            File.WriteAllText(fullPath, string.Empty, encoding);
                            Log($"[Success] Очистка: удалено {num} слов (весь файл) в {fileName}");
                            return true;
                        }
                        var remaining = words.Skip(num).ToList();
                        var rebuilt = string.Join(" ", remaining);
                        File.WriteAllText(fullPath, rebuilt, encoding);
                        Log($"[Success] Очистка: удалены первые {num} слов в {fileName}");
                        return true;
                    }

                    if (mSentences.Success)
                    {
                        int num = int.Parse(mSentences.Groups["num"].Value);
                        if (num < 0)
                        {
                            Log($"[Error 103] Строка {lineNumber}: неверный параметр предложений ({num})");
                            return false;
                        }
                        var sentences = SplitIntoSentences(text).ToList();
                        if (num >= sentences.Count)
                        {
                            File.WriteAllText(fullPath, string.Empty, encoding);
                            Log($"[Success] Очистка: удалено {num} предложений (весь файл) в {fileName}");
                            return true;
                        }
                        var remaining = sentences.Take(sentences.Count - num).ToArray();
                        var rebuilt = string.Join(" ", remaining).Trim();
                        File.WriteAllText(fullPath, rebuilt, encoding);
                        Log($"[Success] Очистка: удалено {num} последних предложений в {fileName}");
                        return true;
                    }

                    Log($"[Error 103] Строка {lineNumber}: нарушена структура формулы очистки ({rest})");
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"[Error] Строка {lineNumber}: {ex.Message} (501)");
                    return false;
                }
            }

            Log($"[Error] Строка {lineNumber}: неизвестная или неверная структура команды (500)");
            return false;
        }

        private string? BuildOutputPath(string atPath, string fileName)
        {
            string fullPath;
            if (!string.IsNullOrEmpty(atPath))
            {
                var path = atPath.Trim();
                var normalized = path.ToLowerInvariant();
                if (normalized == "рабочий стол" || normalized == "desktop" || normalized == "рабочий_стол" || normalized == "рабочийстол")
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                else if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), path);
                }

                if (Path.HasExtension(path) || path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                {
                    fullPath = path;
                }
                else
                {
                    fullPath = Path.Combine(path, fileName);
                }
            }
            else
            {
                fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            }

            if (!fullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
                !fullPath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".txt";
            }

            return fullPath;
        }

        private bool WaitForClick(int lineNumber)
        {
            if (ClickCallback != null)
                return ClickCallback(lineNumber);

            Log($"[Click] Строка {lineNumber}: нажмите Enter, когда клик выполнен");
            Console.ReadLine();
            Log($"[Click] Отклик получен");
            return true;
        }

        private IList<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var parts = Regex.Split(text, "(?<=[.!?])\\s+(?=[A-ZА-ЯЁ])");
            if (parts.Length == 1)
            {
                parts = Regex.Split(text, "(?<=[.!?])\\s+");
            }

            return parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
        }

        private IList<string> SplitWordsPreserve(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();
            var words = Regex.Split(text.Trim(), "\\s+").Where(w => w.Length > 0).ToList();
            return words;
        }
    }
}