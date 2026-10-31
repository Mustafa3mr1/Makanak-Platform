namespace Makanak.Application.Common.Models;

public static class QueryableExtensions
{
    public static IQueryable<T> ToPage<T>(this IQueryable<T> query, PagedQuery paging)
        => query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize);
}
