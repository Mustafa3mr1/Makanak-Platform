namespace Makanak.Application.Common.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        => new(items, pageNumber, pageSize, totalCount, totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
}
