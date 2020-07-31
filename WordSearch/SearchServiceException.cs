using System;

namespace Tools.SearchService
{
    public class SearchServiceException : Exception
    {
        public SearchServiceException(string message) : base(message)
        {
        }
    }
}
