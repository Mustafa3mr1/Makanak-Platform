namespace Makanak.Application.Common.Models;

public class PagedQuery
{
    public int PageNumber { get; set; } = PaginationRules.DefaultPageNumber;
    public int PageSize { get; set; } = PaginationRules.DefaultPageSize;
}

public static class PaginationRules
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
}
