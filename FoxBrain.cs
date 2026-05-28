using System;
using System.IO;
using System.Text.RegularExpressions;

class FoxBrain
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        SetConsoleColors();

        string файлСкрипта = "";

        if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
        {
            if (!args[0].EndsWith(".fc", StringComparison.OrdinalIgnoreCase))
            {
                Ошибка(401, "Аргумент скрипта", "FoxCod принимает только расширение .fc");
                return;
            }

            файлСкрипта = args[0];
        }
        else
        {
            файлСкрипта = "прогр.fc";
        }

        if (!File.Exists(файлСкрипта))
        {
            Ошибка(301, "Файл скрипта", $"Файл {файлСкрипта} не найден");
            return;
        }

        string[] строки = File.ReadAllLines(файлСкрипта);

        Успех("FoxCod Ядро запущен", "Сектор 1 — База");

        for (int i = 0; i < строки.Length; i++)
        {
            string строка = строки[i].Trim();
            if (string.IsNullOrEmpty(строка) || строка.StartsWith("#"))
                continue;

            ВыполнитьКоманду(строка, i + 1);
        }

        Успех("Выполнение завершено", "Все строки обработаны");
    }

    static void ВыполнитьКоманду(string строка, int номерСтроки)
    {
        // Парсинг по формуле: a b "f" (d) и @
        Match командаМатч = Regex.Match(строка, @"^(\w+)\s+(в\s+)?(.+?)\s*(?:@""(.*?)""|\@(.+?))?\s*""(.*?)""?\s*\((.*?)\)", RegexOptions.IgnoreCase);

        if (!командаМатч.Success)
        {
            Ошибка(103, $"Строка {номерСтроки}", "Нарушена структура формулы");
            return;
        }

        string a = командаМатч.Groups[1].Value.ToLower().Trim(); // команда
        string b = командаМатч.Groups[3].Value.Trim();           // объект
        string путьЧерезАт = (командаМатч.Groups[4].Success ? командаМатч.Groups[4].Value : 
                              командаМатч.Groups[5].Success ? командаМатч.Groups[5].Value : "").Trim();
        string f = командаМатч.Groups[6].Value.Trim();           // имя файла
        string d = командаМатч.Groups[7].Value.Trim();           // текст

        if (a != "вписать")
        {
            Ошибка(102, $"Строка {номерСтроки}", $"Неизвестная команда: {a}");
            return;
        }

        if (string.IsNullOrEmpty(f) || string.IsNullOrEmpty(d))
        {
            Ошибка(103, $"Строка {номерСтроки}", "Отсутствует имя файла или текст");
            return;
        }

        // Проверка на попытку записи в папку
        if (b.ToLower().Contains("папку"))
        {
            Ошибка(302, $"Строка {номерСтроки}", "Нельзя вписать текст в папку");
            return;
        }

        string полныйПуть;

        if (!string.IsNullOrEmpty(путьЧерезАт))
        {
            полныйПуть = путьЧерезАт;
            if (!Path.IsPathRooted(полныйПуть))
                полныйПуть = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), полныйПуть);
        }
        else
        {
            полныйПуть = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), f);
        }

        if (!полныйПуть.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && 
            !полныйПуть.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            полныйПуть += ".txt";
        }

        try
        {
            if (!Отклик(номерСтроки))
                return;

            string? директория = Path.GetDirectoryName(полныйПуть);
            if (!string.IsNullOrEmpty(директория) && !Directory.Exists(директория))
                Directory.CreateDirectory(директория);

            File.WriteAllText(полныйПуть, d);
            Успех($"Вписано в {f}", d.Length > 50 ? d.Substring(0, 47) + "..." : d);
        }
        catch (Exception ex)
        {
            Ошибка(301, $"Строка {номерСтроки}", ex.Message ?? "Неизвестная ошибка");
        }
    }

    static void Ошибка(int код, string место, string причина)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!  [Ошибка {код}] — {место} / {причина}");
        Console.ResetColor();
    }

    static void Успех(string действие, string детали)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✔️  {действие} — {детали}");
        Console.ResetColor();
    }

    static bool Отклик(int номерСтроки)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"?  Строка {номерСтроки}: ожидается отклик. Нажмите Enter, когда клик прошёл.");
        Console.ResetColor();
        Console.ReadLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"→ Отклик получен, продолжаем выполнение строки {номерСтроки}.");
        Console.ResetColor();
        return true;
    }

    static void SetConsoleColors()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();
    }
}