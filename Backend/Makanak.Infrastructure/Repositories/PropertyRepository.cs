using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Domain.Entities;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public sealed class PropertyRepository : IPropertyRepository
{
    private readonly ApplicationDbContext _context;

    public PropertyRepository(ApplicationDbContext context) => _context = context;

    public async Task<Property?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Properties
            .FirstOrDefaultAsync(p => p.PublicId == publicId, cancellationToken);
    }

    public async Task<Property?> GetByPublicIdWithImagesAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.PublicId == publicId, cancellationToken);
    }

    public async Task<IReadOnlyList<Property>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Properties
            .AsNoTracking()
            .Include(p => p.Images.Where(i => i.IsPrimary))  // Filtered include: only primary image
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Property property, CancellationToken cancellationToken = default)
    {
        await _context.Properties.AddAsync(property, cancellationToken);
    }

    public void Update(Property property)
    {
        _context.Properties.Update(property);
    }
}
