namespace LaborBLL.ModelVM
{
    /// <summary>
    /// Generic paged result for pagination support
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// Current page items
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// First item index on current page (1-based)
        /// </summary>
        public int FirstItemIndex => (PageNumber - 1) * PageSize + 1;

        /// <summary>
        /// Last item index on current page (1-based)
        /// </summary>
        public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);
    }

    /// <summary>
    /// Pagination parameters for requests
    /// </summary>
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Items per page (max 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Min(value, MaxPageSize);
        }

        /// <summary>
        /// Sort column name
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort direction (asc/desc)
        /// </summary>
        public string? SortDirection { get; set; } = "asc";
    }
}
