using System.Collections.Generic;

namespace Tools.SearchService
{
    public class Result
    {
        public Dictionary<string, int> All { get; private set; }
        public Dictionary<string, Dictionary<string, int>> Groupped { get; private set; }
        public List<string> Errors { get; private set; }

        public Result(string[] words)
        {
            All = new Dictionary<string, int>();
            Groupped = new Dictionary<string, Dictionary<string, int>>();
            Errors = new List<string>();
            foreach (var word in words)
            {
                All.Add(word, 0);
                Groupped.Add(word, new Dictionary<string, int>());
            }
        }

        public void Add(string word, string file, int count)
        {
            All[word] += count;
            if (!Groupped[word].ContainsKey(file))
            {
                Groupped[word].Add(file, 0);
            }
            Groupped[word][file] += count;
        }
    }
}
