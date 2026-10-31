using System.Data;
using Makanak.Application.Common.Exceptions;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MakanakDbContext _context;

    public UnitOfWork(MakanakDbContext context)
    {
        _context = context;
        Properties = new PropertyRepository(context);
        PropertyImages = new GenericRepository<PropertyImage>(context);
        Offers = new OfferRepository(context);
        NegotiationHistories = new GenericRepository<NegotiationHistory>(context);
    }

    public IPropertyRepository Properties { get; }
    public IRepository<PropertyImage> PropertyImages { get; }
    public IOfferRepository Offers { get; }
    public IRepository<NegotiationHistory> NegotiationHistories { get; }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was modified by another request. Reload the latest version and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("A conflicting record already exists.");
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Start every attempt from a clean slate so a retry never sees half-applied tracked state.
            _context.ChangeTracker.Clear();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var result = await action();
            await transaction.CommitAsync();
            return result;
        });
    }

    public void SetOriginalRowVersion<TEntity>(TEntity entity, byte[] rowVersion) where TEntity : class
        => _context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
