using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tools.SearchService
{
    internal class SearchServiceInternal
    {
        private struct ResultUnit
        {
            public string Word;
            public string FileName;
            public int Count;
        }

        private Result result { get; set; }
        private const string NotLetterPattern = "\\W";
        private int counter = 0;
        private readonly object lockObject = new object();
        private readonly ILogger _logger;

        private readonly List<ActionBlock<(string text, string name)>> actions = new List<ActionBlock<(string text, string name)>>();
        private readonly ConcurrentBag<ResultUnit> resultUnits = new ConcurrentBag<ResultUnit>();
        private readonly ConcurrentBag<string> errors = new ConcurrentBag<string>();
        private string _directory { get; set; }
        private string _wordsFile { get; set; }
        private bool _caseSensitive { get; set; }
        private readonly string[] words;

        private int filesCount;

        private Task waiter { get; set; }

        public Result GetResult()
        {
            if (SearchingDone) return result;
            return null;
        }

        public bool IsSearching { get; private set; }
        public bool SearchingDone { get; private set; }

        public async Task StopSearch()
        {
            List<Task> tasks = new List<Task>();
            foreach (var act in actions)
            {
                tasks.Add(CompleteAction(act));
            }
            await Task.WhenAll(tasks);
        }

        private async Task CompleteAction(ActionBlock<(string text, string name)> actionBlock)
        {
            actionBlock.Complete();
            await actionBlock.Completion;
        }

        public int GetProcessedPercent()
        {
            return (int)(((double)counter / filesCount * words.Length) * 100);
        }

        public SearchServiceInternal(string directory, string wordsFile, bool caseSensitive, ILogger logger)
        {
            _logger = logger;
            _caseSensitive = caseSensitive;
            PreparePaths(directory, wordsFile);
            using (var reader = new StreamReader(_wordsFile))
            {
                var matches = Regex.Matches(reader.ReadToEnd(), "\\w+");
                words = matches.Cast<Match>().Select(s => s.Value).ToArray();
            }
            result = new Result(words);
            PrepareActions();
        }

        public async Task StartSearch()
        {
            if (!IsSearching)
            {
                IsSearching = true;
                await SendFilesToActions();
                waiter = new Task(async () =>
                {
                    while (counter < filesCount * words.Length)
                    {
                        await Task.Delay(100);
                    }
                    foreach (var unit in resultUnits)
                    {
                        result.Add(unit.Word, unit.FileName, unit.Count);
                    }
                    foreach (var err in errors)
                    {
                        result.Errors.Add(err);
                    }
                    SearchingDone = true;
                });
                waiter.Start();
            }
        }

        protected async Task SendFilesToActions()
        {
            foreach (var file in Directory.EnumerateFiles(_directory, "*.*", SearchOption.AllDirectories))
                //TODO: Split big files (but not cut word!!!)
                using (var reader = new StreamReader(file))
                    await actions[0].SendAsync((reader.ReadToEnd(), file));
            filesCount = Directory.EnumerateFiles(_directory, "*.*", SearchOption.AllDirectories).Count();
        }

        protected void PrepareActions()
        {
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var nextNum = i + 1;
                var action = new ActionBlock<(string text, string name)>(async (file) =>
                {
                    await HandleFile(file, word, nextNum);
                }, new ExecutionDataflowBlockOptions()
                {
                    //TODO: вынести в конфиг
                    BoundedCapacity = 100,
                    MaxDegreeOfParallelism = 4,
                    MaxMessagesPerTask = 2
                });
                actions.Add(action);
            }
        }

        protected async Task HandleFile((string text, string name) file, string word, int nextNum)
        {
            try
            {
                RegexOptions opt = _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                var matches = Regex.Matches(file.text, NotLetterPattern + word + NotLetterPattern, opt);
                if (matches.Count > 0)
                {
                    resultUnits.Add(new ResultUnit()
                    {
                        FileName = file.name,
                        Word = word,
                        Count = matches.Count
                    });
                }

                if (nextNum < actions.Count)
                {
                    await actions[nextNum].SendAsync(file);
                }
            }
            catch (Exception ex)
            {
                var error = $"Error throws while find word {word} in file {file.name} : {ex.Message}";
                _logger?.LogError(error);
                errors.Add(error);
            }
            finally
            {
                lock (lockObject)
                {
                    counter++;
                }
            }
        }

        protected void PreparePaths(string directory, string wordsFile)
        {
            if (!Path.IsPathRooted(directory))
                _directory = Path.Combine(Directory.GetCurrentDirectory(), directory);
            else _directory = directory;

            if (!Path.IsPathRooted(wordsFile))
                _wordsFile = Path.Combine(Directory.GetCurrentDirectory(), wordsFile);
            else _wordsFile = wordsFile;

            if (!File.Exists(_wordsFile))
                throw new FileNotFoundException($"File {wordsFile} not found");

            if (!Directory.Exists(_directory))
                throw new FileNotFoundException($"Directory {directory} not found");
        }
    }
}
