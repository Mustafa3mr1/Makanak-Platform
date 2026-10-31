using FluentValidation;
using Makanak.Application.Common.Exceptions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Admin;
using Makanak.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Application.Services;

public sealed class AdminService : IAdminService
{
    private readonly IUserAccountGateway _users;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<AdminUserQuery> _userQueryValidator;
    private readonly IValidator<AdminPropertyQuery> _propertyQueryValidator;

    public AdminService(IUserAccountGateway users, IUnitOfWork uow,
        IValidator<AdminUserQuery> userQueryValidator, IValidator<AdminPropertyQuery> propertyQueryValidator)
    {
        _users = users;
        _uow = uow;
        _userQueryValidator = userQueryValidator;
        _propertyQueryValidator = propertyQueryValidator;
    }

    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(AdminUserQuery query)
    {
        await _userQueryValidator.ValidateAndThrowAsync(query);
        var (items, total) = await _users.ListAsync(query.Search?.Trim(), query.UserType, query.IsActive, query.PageNumber, query.PageSize);
        return PagedResult<AdminUserDto>.Create(items.Select(ToDto).ToList(), total, query.PageNumber, query.PageSize);
    }

    public async Task<AdminUserDetailDto> GetUserAsync(Guid userId)
    {
        var user = await _users.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");

        var properties = await _uow.Properties.QueryNoTracking().CountAsync(p => p.OwnerId == userId);
        var submitted = await _uow.Offers.QueryNoTracking().CountAsync(o => o.SeekerId == userId);
        var received = await _uow.Offers.QueryNoTracking().CountAsync(o => o.Property.OwnerId == userId);

        return new AdminUserDetailDto(ToDto(user), properties, submitted, received);
    }

    public async Task<AdminUserDto> DeactivateUserAsync(Guid adminId, Guid userId)
    {
        if (adminId == userId)
            throw new BusinessRuleException("You cannot deactivate your own account.");

        var user = await _users.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        if (user.UserType == UserType.Admin)
            throw new ForbiddenException("Admin accounts cannot be deactivated through the API.");

        if (user.IsActive)
        {
            await _users.SetActiveAsync(userId, false);

            if (user.UserType == UserType.Owner)
            {
                var listings = await _uow.Properties.Query()
                    .Where(p => p.OwnerId == userId && p.Status == PropertyStatus.Available)
                    .ToListAsync();
                foreach (var p in listings) p.Status = PropertyStatus.Inactive;
                if (listings.Count > 0) await _uow.SaveChangesAsync();
            }
        }

        return ToDto((await _users.FindByIdAsync(userId))!);
    }

    public async Task<AdminUserDto> ReactivateUserAsync(Guid userId)
    {
        var user = await _users.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        if (!user.IsActive) await _users.SetActiveAsync(userId, true);
        return ToDto((await _users.FindByIdAsync(userId))!);
    }

    public async Task<PagedResult<AdminPropertyDto>> ListPropertiesAsync(AdminPropertyQuery query)
    {
        await _propertyQueryValidator.ValidateAndThrowAsync(query);

        var q = _uow.Properties.QueryNoTracking();
        if (query.Status.HasValue) { var s = query.Status.Value; q = q.Where(p => p.Status == s); }
        if (query.OwnerId.HasValue) { var o = query.OwnerId.Value; q = q.Where(p => p.OwnerId == o); }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(p => p.Title.Contains(k) || p.City.Contains(k) || p.Address.Contains(k));
        }

        var total = await q.CountAsync();
        var rows = await q.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id)
            .ToPage(query)
            .Select(p => new AdminPropertyDto(
                p.Id, p.OwnerId, string.Empty, p.Title, p.City, p.Price, p.PropertyType, p.ListingType, p.Status,
                p.Offers.Count(), p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync();

        var names = await _users.GetNamesAsync(rows.Select(r => r.OwnerId).Distinct());
        var items = rows.Select(r => r with { OwnerName = names.TryGetValue(r.OwnerId, out var n) ? n : "Unknown" }).ToList();
        return PagedResult<AdminPropertyDto>.Create(items, total, query.PageNumber, query.PageSize);
    }

    public async Task<AdminPropertyDto> DeactivatePropertyAsync(Guid propertyId)
    {
        var property = await _uow.Properties.GetByIdAsync(propertyId) ?? throw new NotFoundException("Property not found.");
        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
            throw new BusinessRuleException($"A {property.Status} property is part of a completed deal and cannot be deactivated.");

        if (property.Status != PropertyStatus.Inactive)
        {
            property.Status = PropertyStatus.Inactive;
            await _uow.SaveChangesAsync();
        }
        return await LoadPropertyAsync(propertyId);
    }

    public async Task<AdminPropertyDto> ReactivatePropertyAsync(Guid propertyId)
    {
        var property = await _uow.Properties.GetByIdAsync(propertyId) ?? throw new NotFoundException("Property not found.");
        if (property.Status != PropertyStatus.Inactive)
            throw new BusinessRuleException("Only Inactive properties can be reactivated.");

        var owner = await _users.FindByIdAsync(property.OwnerId);
        if (owner is null || !owner.IsActive)
            throw new BusinessRuleException("The owner's account is deactivated. Reactivate the owner first.");

        property.Status = PropertyStatus.Available;
        await _uow.SaveChangesAsync();
        return await LoadPropertyAsync(propertyId);
    }

    public async Task<PlatformStatsDto> GetStatsAsync()
    {
        var counts = await _users.GetCountsAsync();

        var propRows = await _uow.Properties.QueryNoTracking().GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var offerRows = await _uow.Offers.QueryNoTracking().GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync();

        var since = DateTime.UtcNow.AddDays(-7);
        var recentOffers = await _uow.Offers.QueryNoTracking().CountAsync(o => o.CreatedAtUtc >= since);

        return new PlatformStatsDto(
            counts.Total, counts.Active,
            Enum.GetValues<UserType>().ToDictionary(t => t.ToString(), t => counts.ByType.TryGetValue(t, out var c) ? c : 0),
            propRows.Sum(r => r.Count),
            Enum.GetValues<PropertyStatus>().ToDictionary(s => s.ToString(), s => propRows.FirstOrDefault(r => r.Key == s)?.Count ?? 0),
            offerRows.Sum(r => r.Count),
            Enum.GetValues<OfferStatus>().ToDictionary(s => s.ToString(), s => offerRows.FirstOrDefault(r => r.Key == s)?.Count ?? 0),
            recentOffers, DateTime.UtcNow);
    }

    private async Task<AdminPropertyDto> LoadPropertyAsync(Guid id)
    {
        var row = await _uow.Properties.QueryNoTracking().Where(p => p.Id == id)
            .Select(p => new AdminPropertyDto(
                p.Id, p.OwnerId, string.Empty, p.Title, p.City, p.Price, p.PropertyType, p.ListingType, p.Status,
                p.Offers.Count(), p.CreatedAtUtc, p.UpdatedAtUtc))
            .FirstAsync();
        var names = await _users.GetNamesAsync(new[] { row.OwnerId });
        return row with { OwnerName = names.TryGetValue(row.OwnerId, out var n) ? n : "Unknown" };
    }

    private static AdminUserDto ToDto(UserAccountInfo u) => new(u.Id, u.FullName, u.Email, u.UserType, u.IsActive, u.CreatedAtUtc);
}
