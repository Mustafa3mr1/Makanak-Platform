using FluentValidation;
using Makanak.Application.Common.Exceptions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;
using Makanak.Application.DTOs.User;
using Makanak.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserAccountGateway _users;
    private readonly IUnitOfWork _uow;
    private readonly IOfferService _offers;
    private readonly IValidator<UpdateProfileRequest> _profileValidator;
    private readonly IValidator<PagedQuery> _pagingValidator;

    public UserService(IUserAccountGateway users, IUnitOfWork uow, IOfferService offers,
        IValidator<UpdateProfileRequest> profileValidator, IValidator<PagedQuery> pagingValidator)
    {
        _users = users;
        _uow = uow;
        _offers = offers;
        _profileValidator = profileValidator;
        _pagingValidator = pagingValidator;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _users.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        return ToProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        await _profileValidator.ValidateAndThrowAsync(request);

        // Only FullName is updatable. Role / UserType / Email cannot be changed here.
        if (!await _users.UpdateFullNameAsync(userId, request.FullName.Trim()))
            throw new NotFoundException("User not found.");

        return await GetProfileAsync(userId);
    }

    public async Task<OwnerDashboardDto> GetOwnerDashboardAsync(Guid ownerId, PagedQuery paging)
    {
        await _pagingValidator.ValidateAndThrowAsync(paging);

        var mine = _uow.Properties.QueryNoTracking().Where(p => p.OwnerId == ownerId);

        var statusRows = await mine.GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var byStatus = Enum.GetValues<PropertyStatus>()
            .ToDictionary(s => s.ToString(), s => statusRows.FirstOrDefault(r => r.Status == s)?.Count ?? 0);

        var awaiting = await _uow.Offers.QueryNoTracking()
            .CountAsync(o => o.Property.OwnerId == ownerId && o.Status == OfferStatus.PendingOwnerResponse);

        var total = await mine.CountAsync();
        var items = await mine
            .OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id)
            .ToPage(paging)
            .Select(p => new OwnerPropertySummaryDto(
                p.Id, p.Title, p.City, p.Price, p.Status, p.PropertyType, p.ListingType,
                p.Offers.Count(),
                p.Offers.Count(o => o.Status == OfferStatus.PendingOwnerResponse || o.Status == OfferStatus.PendingBuyerResponse),
                p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync();

        var recent = await _offers.GetOwnerOffersAsync(ownerId, new OfferListQuery { PageNumber = 1, PageSize = 10 });

        return new OwnerDashboardDto(byStatus, awaiting,
            PagedResult<OwnerPropertySummaryDto>.Create(items, total, paging.PageNumber, paging.PageSize),
            recent.Items);
    }

    public async Task<BuyerDashboardDto> GetBuyerDashboardAsync(Guid seekerId, OfferListQuery query)
    {
        var mine = _uow.Offers.QueryNoTracking().Where(o => o.SeekerId == seekerId);

        var rows = await mine.GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var byStatus = Enum.GetValues<OfferStatus>()
            .ToDictionary(s => s.ToString(), s => rows.FirstOrDefault(r => r.Status == s)?.Count ?? 0);

        var awaiting = byStatus[OfferStatus.PendingBuyerResponse.ToString()];
        var page = await _offers.GetMyOffersAsync(seekerId, query);

        return new BuyerDashboardDto(byStatus, awaiting, page);
    }

    private static UserProfileDto ToProfile(UserAccountInfo u)
        => new(u.Id, u.FullName, u.Email, u.UserType, u.CreatedAtUtc);
}
