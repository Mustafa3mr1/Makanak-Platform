using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Infrastructure.Data;
using Makanak.Infrastructure.Repositories;

namespace Makanak.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    // Lazy-initialized repository instances — all share the same DbContext instance,
    // ensuring all changes are committed in a single SaveChangesAsync call.
    private IPropertyRepository? _properties;
    private IOfferRepository? _offers;
    private INegotiationHistoryRepository? _negotiationHistories;
    private IUserRepository? _users;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public IPropertyRepository Properties
        => _properties ??= new PropertyRepository(_context);

    public IOfferRepository Offers
        => _offers ??= new OfferRepository(_context);

    public INegotiationHistoryRepository NegotiationHistories
        => _negotiationHistories ??= new NegotiationHistoryRepository(_context);

    public IUserRepository Users
        => _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task<IDbTransactionProxy> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new DbTransactionProxy(transaction);
    }

    public async Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        // Uses EF Core's execution strategy to handle transient SQL Server failures with retry.
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
