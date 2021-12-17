namespace sp.Core.Models
{
    public class PagingRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 200;

        public (int Skip, int Take) GetSkipTake()
        {
            if (Page <= 0)
                Page = 1;

            if (PageSize <= 0 || PageSize > 200)
                PageSize = 200;

            return ((Page - 1) * PageSize, PageSize);
        }

        //DTO
        public object Meta { get; set; }
    }
}