namespace ECommerce.Application.Common.Models;

public class PagedQuery
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    public string? SortBy { get; set; }

    // "asc" veya "desc"
    public string SortDir { get; set; } = "asc";

    public bool IsDescending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);

    public int Skip => (Page - 1) * PageSize;
}