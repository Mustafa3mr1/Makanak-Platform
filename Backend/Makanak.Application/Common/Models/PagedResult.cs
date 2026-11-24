namespace Makanak.Application.Common.Models;

/// <summary>
/// Generic container for server-side paged query results.
/// PageSize is capped at 50, default is 12.
/// </summary>
/// <typeparam name="T">Type of items contained in the page.</typeparam>
public class PagedResult<T>
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 50;

    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page <= 0 ? 1 : page;
        PageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
