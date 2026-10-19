using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IPropertyRepository? _properties;
    private IOfferRepository? _offers;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IPropertyRepository Properties => _properties ??= new PropertyRepository(_context);
    public IOfferRepository Offers => _offers ??= new OfferRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbTransactionProxy> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new DbTransactionProxy(transaction);
    }

    public async Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
