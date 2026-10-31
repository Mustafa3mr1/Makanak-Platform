namespace Makanak.Application.Common.Interfaces;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
