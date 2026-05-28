using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FoxCod.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var scriptDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        if (!Directory.Exists(scriptDir))
        {
            var candidate = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Scripts"));
            if (Directory.Exists(candidate))
                scriptDir = candidate;
        }
        var scripts = Directory.Exists(scriptDir) ? Directory.GetFiles(scriptDir, "*.fc").OrderBy(x => x).ToArray() : Array.Empty<string>();

        while (true)
        {
            DisplayMainMenu(scripts);
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (string.IsNullOrEmpty(input) || input.Length < 2)
                continue;

            var command = input.Last();
            if (!int.TryParse(input.Substring(0, input.Length - 1), out var index) || index < 1 || index > scripts.Length)
                continue;

            var scriptPath = scripts[index - 1];

            switch (command)
            {
                case 'a':
                    RunScript(scriptPath);
                    break;
                case 'b':
                    EditScript(scriptPath);
                    break;
                default:
                    continue;
            }
        }
    }

    static void DisplayMainMenu(string[] scripts)
    {
        Console.Clear();
        Console.WriteLine("FoxCod Launcher");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        // Left column: Scripts list
        Console.Write("Скрипты (.fc)".PadRight(30));
        Console.WriteLine("Команды");
        Console.WriteLine("───────────────────────────  ───────────────────");
        
        for (int i = 0; i < Math.Max(scripts.Length, 2); i++)
        {
            string scriptLine = "";
            if (i < scripts.Length)
            {
                scriptLine = $" {i + 1}. {Path.GetFileNameWithoutExtension(scripts[i])}";
                scriptLine = scriptLine.Length > 28 ? scriptLine.Substring(0, 25) + "..." : scriptLine;
            }

            string commandLine = "";
            if (i == 0) commandLine = " a - запустить";
            else if (i == 1) commandLine = " b - редактировать";

            Console.WriteLine(scriptLine.PadRight(30) + commandLine);
        }

        Console.WriteLine();
        Console.WriteLine("Введите номер и команду, например: 1a");
        Console.Write("> ");
    }

    static void RunScript(string scriptPath)
    {
        var logs = new List<string>();
        var engine = new FoxEngine(log => logs.Add(log));
        
        Console.Clear();
        Console.WriteLine("Выполнение скрипта...");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        bool success = engine.ExecuteScript(scriptPath, requireClick: true);
        
        DisplayStatusScreen(scriptPath, logs, success);
    }

    static void DisplayStatusScreen(string scriptPath, List<string> logs, bool success)
    {
        Console.Clear();
        Console.WriteLine($"Скрипт: {Path.GetFileName(scriptPath)}");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("Статус выполнения:");
        Console.WriteLine();

        foreach (var log in logs)
        {
            if (log.Contains("[Error]"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {log}");
                Console.ResetColor();
            }
            else if (log.Contains("[Success]"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ {log}");
                Console.ResetColor();
            }
            else if (log.Contains("[Click]"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⏳ {log}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"ℹ {log}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        if (!success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("! Скрипт не был полностью выполнен (код ошибки 500-501)");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("Нажмите Enter для возврата в меню...");
        Console.ReadLine();
    }

    static void EditScript(string scriptPath)
    {
        try
        {
            Console.Clear();
            Console.WriteLine($"Редактирование: {Path.GetFileName(scriptPath)}");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();
            
            var content = File.ReadAllText(scriptPath);
            Console.WriteLine(content);
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter, чтобы открыть файл в редакторе...");
            Console.ReadLine();
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(scriptPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter для возврата...");
            Console.ReadLine();
        }
    }
}
