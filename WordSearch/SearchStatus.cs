namespace Tools.SearchService
{
    public class SearchStatus
    {
        public SearchStatus(bool done, int percent)
        {
            Done = done;
            Percent = percent;
        }
        public bool Done { get; }
        public int Percent { get; }
    }
}
