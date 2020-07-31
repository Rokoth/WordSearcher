using System;
using System.Threading.Tasks;

namespace Tools.SearchService
{
    public interface ISearchService
    {
        Result GetSearchResult(Guid search);
        SearchStatus GetSearchStatus(Guid search);
        Result Search(string directory, string wordsFile, bool caseSensitive);
        Task<Guid> StartSearchAsync(string directory, string wordsFile, bool caseSensitive);
        Task StopSearchAsync(Guid search);
    }
}