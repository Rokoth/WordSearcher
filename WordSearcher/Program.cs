using Microsoft.Extensions.Logging;
using System;
using Tools.SearchService;

namespace WordSearchConsole
{
    class Program
    {
        private const string ARGUMENT_COUNT_EXCEPTION_STRING = "Неверный вызов команды. Использование: \r\n" +
                                "1)WordSearcher.exe без аргументов - работа в интерактивном режиме\r\n" +
                                "2)WordSearcher.exe -d <directoryName> -f <fileName> [-c true|false] - поиск в файлах <directoryName> слов из файла <fileName> " +
                                  "с учетом/без учета регистра (по умолчанию false)";
        private const string INTERACTIVE_HELP_STRING_TITLE = "Введите данные для поиска. Для выхода введите \\exit";
        private const string INTERACTIVE_HELP_STRING1 = "Введите директорию для поиска: ";
        private const string INTERACTIVE_HELP_STRING2 = "Введите имя файла со словами для поиска: ";
        private const string INTERACTIVE_HELP_STRING3 = "Учитывать регистр? y/n [n]";
        private const string INPUT_EMPTY_ERROR = "Ошибка: пустая строка ввода";
        private const string INPUT_COUNT_ERROR = "Неверная строка ввода";
        private const string EXIT_STRING = "Для выхода нажмите любую клавишу...";
        private const string EXIT_COMMAND = "\\exit";

        private enum Mode
        {
            args, input
        }

        private enum OutMode
        {
            all = 0, groupped = 1
        }

        static void Main(string[] args)
        {
            Mode mode = Mode.input;
            string directory = null;
            string fileName = null;
            bool caseSens = false;
            try
            {
                if (args != null && args.Length > 0)
                {
                    if (args.Length != 4 && args.Length != 6) throw new ArgException(ARGUMENT_COUNT_EXCEPTION_STRING);
                    mode = Mode.args;
                    for (int i = 0; i < args.Length; i += 2)
                    {
                        switch (args[i])
                        {
                            case "-d":
                                directory = args[i + 1]; break;
                            case "-f":
                                fileName = args[i + 1]; break;
                            case "-c":
                                if (args[i + 1].Equals("true", StringComparison.InvariantCultureIgnoreCase)) caseSens = true;
                                break;
                            default: throw new ArgException(ARGUMENT_COUNT_EXCEPTION_STRING);
                        }
                    }
                }

                var logger = LoggerFactory.Create(builder =>
                {
                    builder.ClearProviders();
                    builder.AddConsole();
                }).CreateLogger<Program>();

                SearchService service = new SearchService(logger);
                switch (mode)
                {
                    case Mode.input:
                        while (true)
                        {
                            try
                            {
                                Console.WriteLine(INTERACTIVE_HELP_STRING_TITLE);
                                Console.Write(INTERACTIVE_HELP_STRING1);
                                directory = Console.ReadLine();
                                if (directory == EXIT_COMMAND) break;
                                if (string.IsNullOrEmpty(directory)) throw new ArgException(INPUT_EMPTY_ERROR);

                                Console.Write(INTERACTIVE_HELP_STRING2);
                                fileName = Console.ReadLine();
                                if (fileName == EXIT_COMMAND) break;
                                if (string.IsNullOrEmpty(fileName)) throw new ArgException(INPUT_EMPTY_ERROR);

                                Console.Write(INTERACTIVE_HELP_STRING3);
                                var caseSensText = Console.ReadLine();
                                if (caseSensText == EXIT_COMMAND) break;
                                if (caseSensText.Equals("y", StringComparison.InvariantCultureIgnoreCase)) caseSens = true;

                                var result = service.Search(directory, fileName, caseSens);
                                PrintResult(result);
                            }
                            catch (ArgException ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        break;
                    case Mode.args:

                        var res = service.Search(directory, fileName, caseSens);
                        PrintResult(res);

                        Console.WriteLine(EXIT_STRING);
                        Console.ReadKey();
                        break;
                    default: break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }

        }

        static void PrintResult(Result result)
        {
            OutMode outMode = OutMode.all;
            Console.Write("Поиск окончен. Выберите режим вывода результата: 0 - Общий, 1 - Подробный: [0]");
            var mode = Console.ReadLine();
            if (Enum.TryParse(mode, out OutMode outm)) outMode = outm;

            int wordCount = 0;

            foreach (var stat in result.All)
            {
                Console.WriteLine($"{wordCount++}) Слово {stat.Key}: всего {stat.Value}");
                if (outMode == OutMode.groupped)
                {
                    int fileCount = 0;
                    foreach (var statGr in result.Groupped[stat.Key])
                    {
                        Console.WriteLine($"    {fileCount++}. Файл {statGr.Key}: всего {statGr.Value}");
                    }
                }
            }
        }
    }

    internal class ArgException : Exception
    {
        public ArgException(string message) : base(message)
        {
        }
    }
}
