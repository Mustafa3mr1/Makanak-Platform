using Microsoft.EntityFrameworkCore;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Repositories;

public sealed class OfferRepository : IOfferRepository
{
    private readonly ApplicationDbContext _context;

    public OfferRepository(ApplicationDbContext context) => _context = context;

    public async Task<Offer?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
    }

    public async Task<Offer?> GetByPublicIdWithDetailsAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Include(o => o.Property)
                .ThenInclude(p => p.Images)
            .Include(o => o.Property)
                .ThenInclude(p => p.Owner)
            .Include(o => o.Seeker)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
    }

    public async Task<bool> HasActiveOfferAsync(long propertyId, long seekerId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .AnyAsync(o => o.PropertyId == propertyId &&
                           o.SeekerId == seekerId &&
                           (o.Status == OfferStatus.Pending || o.Status == OfferStatus.Countered),
                      cancellationToken);
    }

    public async Task<IReadOnlyList<Offer>> GetPendingOffersByPropertyIdAsync(
        long propertyId,
        long excludeOfferId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .Where(o => o.PropertyId == propertyId &&
                        o.Id != excludeOfferId &&
                        (o.Status == OfferStatus.Pending || o.Status == OfferStatus.Countered))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<OfferListItemDto>> GetSeekerOffersAsync(
        long seekerId,
        OfferStatus? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Offers
            .AsNoTracking()
            .Where(o => o.SeekerId == seekerId);

        if (statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == statusFilter.Value);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? PagedResult<OfferListItemDto>.DefaultPageSize : Math.Min(pageSize, PagedResult<OfferListItemDto>.MaxPageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OfferListItemDto(
                o.PublicId,
                new OfferPropertySummaryDto(
                    o.Property.PublicId,
                    o.Property.Title,
                    o.Property.City,
                    o.Property.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                        ?? o.Property.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    o.Property.ListingType,
                    o.Property.Status
                ),
                new OfferCounterpartyDto(
                    o.Property.Owner.PublicId,
                    o.Property.Owner.FullName
                ),
                o.InitialPrice,
                o.CounterPrice ?? o.InitialPrice,
                o.RoundNumber,
                o.Status,
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<OfferListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<OfferListItemDto>> GetOwnerIncomingOffersAsync(
        long ownerId,
        OfferStatus? statusFilter,
        Guid? propertyPublicId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Offers
            .AsNoTracking()
            .Where(o => o.Property.OwnerId == ownerId);

        if (statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == statusFilter.Value);
        }

        if (propertyPublicId.HasValue)
        {
            query = query.Where(o => o.Property.PublicId == propertyPublicId.Value);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? PagedResult<OfferListItemDto>.DefaultPageSize : Math.Min(pageSize, PagedResult<OfferListItemDto>.MaxPageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OfferListItemDto(
                o.PublicId,
                new OfferPropertySummaryDto(
                    o.Property.PublicId,
                    o.Property.Title,
                    o.Property.City,
                    o.Property.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                        ?? o.Property.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    o.Property.ListingType,
                    o.Property.Status
                ),
                new OfferCounterpartyDto(
                    o.Seeker.PublicId,
                    o.Seeker.FullName
                ),
                o.InitialPrice,
                o.CounterPrice ?? o.InitialPrice,
                o.RoundNumber,
                o.Status,
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<OfferListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<(int PendingOffers, int TotalOffers)> GetOwnerOfferCountsAsync(long ownerId, CancellationToken cancellationToken = default)
    {
        var query = _context.Offers.AsNoTracking().Where(o => o.Property.OwnerId == ownerId && !o.Property.IsDeleted);
        var total = await query.CountAsync(cancellationToken);
        var pending = await query.Where(o => o.Status == OfferStatus.Pending).CountAsync(cancellationToken);
        return (pending, total);
    }

    public async Task<Makanak.Application.DTOs.Dashboard.SeekerDashboardDto> GetSeekerDashboardCountsAsync(long seekerId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.Offers.AsNoTracking()
            .Where(o => o.SeekerId == seekerId)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int pending = counts.FirstOrDefault(x => x.Status == OfferStatus.Pending)?.Count ?? 0;
        int countered = counts.FirstOrDefault(x => x.Status == OfferStatus.Countered)?.Count ?? 0;
        int accepted = counts.FirstOrDefault(x => x.Status == OfferStatus.Accepted)?.Count ?? 0;
        int rejected = counts.FirstOrDefault(x => x.Status == OfferStatus.Rejected)?.Count ?? 0;
        int total = pending + countered + accepted + rejected;

        return new Makanak.Application.DTOs.Dashboard.SeekerDashboardDto(
            TotalOffers: total,
            Pending: pending,
            Countered: countered,
            Accepted: accepted,
            Rejected: rejected
        );
    }

    public async Task AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await _context.Offers.AddAsync(offer, cancellationToken);
    }

    public void Update(Offer offer)
    {
        _context.Offers.Update(offer);
    }
}
