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
    }
}