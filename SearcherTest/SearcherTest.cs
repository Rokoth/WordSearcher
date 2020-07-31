using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.SearchService;

namespace SearcherTest
{
    public class Tests
    {
        private const string TEST_PATH = "Test Directory";
        private const int deepPath = 2;
        private readonly string[] searchWords = new string[] { "Test1", "tEst2", "test3", "TEST4", "test5", "TesT6", "TeSt7", "TEst8" };
        private readonly string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private readonly string symbols = " .,:!?;";
        private readonly Random random = new Random();
        private List<(string word, string file, int pos)> testWords = new List<(string, string, int)>();


        [SetUp]
        public void Setup()
        {

        }

        private void FillDirectory(int nextDeep, string directory)
        {
            int filesCount = random.Next(1, 10);
            int dirsCount = random.Next(1, 10);
            for (int i = 1; i < filesCount; i++)
                CreateRandomFile(directory, i == filesCount / 2);

            if (nextDeep < deepPath)
            {
                for (int i = 1; i < dirsCount; i++)
                {
                    string dirName;
                    do
                        dirName = GetRandomString(8, letters);
                    while (Directory.Exists(Path.Combine(directory, dirName)));
                    Directory.CreateDirectory(Path.Combine(directory, dirName));
                    FillDirectory(nextDeep + 1, Path.Combine(directory, dirName));
                }
            }
        }

        private void CreateRandomFile(string path, bool addTestWord)
        {
            string fileName;
            do
                fileName = GetRandomFileName();
            while (File.Exists(Path.Combine(path, fileName)));

            int level = (int)Math.Pow(10, random.Next(1, 5));
            int fileLength = random.Next(1, 10) * level;

            string text = "";
            int length = 0;
            bool wordAdded = false;
            int wordsCount = random.Next(1, 200);
            while (length < fileLength)
            {

                string toAdd = GetRandomString(20, letters, true) + GetRandomString(3, symbols);
                if (addTestWord && !wordAdded && length > fileLength / 2)
                {
                    var word = searchWords[random.Next(1, searchWords.Length - 1)];
                    testWords.Add((word, Path.Combine(path, fileName), toAdd.Length));
                    toAdd += word + GetRandomString(3, symbols);
                    wordAdded = true;

                }
                length += toAdd.Length;
                if (wordsCount-- <= 0)
                {
                    wordsCount = random.Next(1, 200);
                    toAdd += "\r\n";
                }
                text += toAdd;
            }
            using var writer = new StreamWriter(Path.Combine(path, fileName));
            writer.Write(text);
        }

        private string GetRandomFileName()
        {
            return $"{GetRandomString(8, letters)}.txt";
        }

        private string GetRandomString(int count, string source, bool randCount = false)
        {
            if (randCount)
                count = random.Next(1, count);
            return string.Concat(Enumerable.Range(0, count)
                .Select(s => source[random.Next(1, source.Count() - 1)]));
        }

        [Test]
        public void SearcherTest()
        {
            PrepareTest(searchWords);
            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
            SearchService searchService = new SearchService(loggerMock.Object);
            var result = searchService.Search(TEST_PATH, "searchWords.txt", false);
            Assert.AreEqual(testWords.Count, result.Groupped.Values.Sum(s => s.Values.Sum()));
        }

        [Test]
        public void SearcherTestCase()
        {
            PrepareTest(searchWords.Select(s => s.ToLower()).ToArray());
            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
            SearchService searchService = new SearchService(loggerMock.Object);
            var result = searchService.Search(TEST_PATH, "searchWords.txt", true);
            Assert.AreEqual(testWords.Where(s => s.word.Equals(s.word.ToLower())).Count(), result.Groupped.Values.Sum(s => s.Values.Sum()));
        }

        private void PrepareTest(string[] searchwords)
        {
            if (Directory.Exists(TEST_PATH))
            {
                Directory.Delete(TEST_PATH, true);
            }

            Directory.CreateDirectory(TEST_PATH);
            FillDirectory(0, TEST_PATH);
            using (var writer = new StreamWriter("searchWords.txt", false))
            {
                writer.Write(string.Join(", ", searchwords));
            }
        }
    }
}