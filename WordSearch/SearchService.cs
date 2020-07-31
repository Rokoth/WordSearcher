using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.SearchService
{

    public class SearchService : ISearchService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<Guid, SearchServiceInternal> searches = new Dictionary<Guid, SearchServiceInternal>();
        public SearchService(ILogger logger)
        {
            _logger = logger;
        }

        public Result Search(string directory, string wordsFile, bool caseSensitive)
        {
            var service = new SearchServiceInternal(directory, wordsFile, caseSensitive, _logger);
            service.StartSearch().GetAwaiter().GetResult();
            while (!service.SearchingDone)
            {
                Thread.Sleep(100);
            }
            return service.GetResult();
        }

        public async Task<Guid> StartSearchAsync(string directory, string wordsFile, bool caseSensitive)
        {
            var search = Guid.NewGuid();
            var service = new SearchServiceInternal(directory, wordsFile, caseSensitive, _logger);
            searches.Add(search, service);
            await service.StartSearch();
            return search;
        }

        public async Task StopSearchAsync(Guid search)
        {
            if (searches.ContainsKey(search))
            {
                await searches[search].StopSearch();
                searches.Remove(search);
            }
            else throw new SearchServiceException("No such search");
        }

        public SearchStatus GetSearchStatus(Guid search)
        {
            if (searches.ContainsKey(search))
            {
                return new SearchStatus(searches[search].SearchingDone, searches[search].GetProcessedPercent());
            }
            else throw new SearchServiceException("No such search");
        }

        public Result GetSearchResult(Guid search)
        {
            if (searches.ContainsKey(search))
            {
                return searches[search].GetResult();
            }
            else throw new SearchServiceException("No such search");
        }
    }
}
