using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
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

    public async Task<Property?> GetByPublicIdWithImagesAndOwnerAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Properties
            .Include(p => p.Images)
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.PublicId == publicId, cancellationToken);
    }

    public async Task<PagedResult<PropertyListItemDto>> GetPagedListAsync(
        PropertyFilterQueryDto filter,
        CancellationToken cancellationToken = default)
    {
        // Public listings: Available only, non-deleted (guaranteed by query filter)
        var query = _context.Properties
            .AsNoTracking()
            .Where(p => p.Status == PropertyStatus.Available);

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var searchTerm = filter.Q.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(searchTerm) ||
                p.City.ToLower().Contains(searchTerm) ||
                p.Address.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim().ToLower();
            query = query.Where(p => p.City.ToLower() == city);
        }

        if (filter.PropertyType != null && filter.PropertyType.Count > 0)
        {
            query = query.Where(p => filter.PropertyType.Contains(p.PropertyType));
        }

        if (filter.ListingType.HasValue)
        {
            query = query.Where(p => p.ListingType == filter.ListingType.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        // Sorting
        query = (filter.Sort?.ToLower()) switch
        {
            "priceasc" => query.OrderBy(p => p.Price).ThenByDescending(p => p.CreatedAt),
            "pricedesc" => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? PagedResult<PropertyListItemDto>.DefaultPageSize : Math.Min(filter.PageSize, PagedResult<PropertyListItemDto>.MaxPageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertyListItemDto(
                p.PublicId,
                p.Title,
                p.Price,
                p.ListingType,
                p.PropertyType,
                p.Status,
                p.City,
                p.Bedrooms,
                p.Bathrooms,
                p.AreaSqm,
                p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                    ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<PropertyListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<PropertyListItemDto>> GetOwnerPropertiesAsync(
        long ownerId,
        PropertyStatus? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Properties
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId);

        if (statusFilter.HasValue)
        {
            query = query.Where(p => p.Status == statusFilter.Value);
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? PagedResult<PropertyListItemDto>.DefaultPageSize : Math.Min(pageSize, PagedResult<PropertyListItemDto>.MaxPageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertyListItemDto(
                p.PublicId,
                p.Title,
                p.Price,
                p.ListingType,
                p.PropertyType,
                p.Status,
                p.City,
                p.Bedrooms,
                p.Bathrooms,
                p.AreaSqm,
                p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                    ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<PropertyListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<(int TotalProperties, int AvailableProperties)> GetOwnerPropertyCountsAsync(long ownerId, CancellationToken cancellationToken = default)
    {
        var query = _context.Properties.AsNoTracking().Where(p => p.OwnerId == ownerId);
        var total = await query.CountAsync(cancellationToken);
        var available = await query.Where(p => p.Status == PropertyStatus.Available).CountAsync(cancellationToken);
        return (total, available);
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
