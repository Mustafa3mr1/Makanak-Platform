using Makanak.Application.Common.Interfaces;
using Makanak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly MakanakDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(MakanakDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public IQueryable<T> Query() => Set;

    public IQueryable<T> QueryNoTracking() => Set.AsNoTracking();

    public async Task<T?> GetByIdAsync(Guid id)
        => await Set.FindAsync(new object[] { id });

    public async Task AddAsync(T entity)
        => await Set.AddAsync(entity);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
