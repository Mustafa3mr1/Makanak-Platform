using FluentValidation;
using Makanak.Application.Common.Exceptions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Application.Services;

public sealed class OfferService : IOfferService
{
    private readonly IUnitOfWork _uow;
    private readonly IUserAccountGateway _users;
    private readonly IValidator<SubmitOfferRequest> _submitValidator;
    private readonly IValidator<CounterOfferRequest> _counterValidator;
    private readonly IValidator<OfferListQuery> _listValidator;

    public OfferService(
        IUnitOfWork uow, IUserAccountGateway users,
        IValidator<SubmitOfferRequest> submitValidator,
        IValidator<CounterOfferRequest> counterValidator,
        IValidator<OfferListQuery> listValidator)
    {
        _uow = uow;
        _users = users;
        _submitValidator = submitValidator;
        _counterValidator = counterValidator;
        _listValidator = listValidator;
    }

    // ---------------------------------------------------------------- Submit

    public async Task<OfferDetailDto> SubmitAsync(Guid propertyId, Guid seekerId, SubmitOfferRequest request)
    {
        await _submitValidator.ValidateAndThrowAsync(request);

        var property = await _uow.Properties.QueryNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new { p.Id, p.OwnerId, p.Status })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Property not found.");

        if (property.OwnerId == seekerId)
            throw new ForbiddenException("You cannot submit an offer on your own property.");

        if (property.Status != PropertyStatus.Available)
            throw new BusinessRuleException("This property is not available for offers.");

        var hasOpenOffer = await _uow.Offers.QueryNoTracking().AnyAsync(o =>
            o.PropertyId == propertyId && o.SeekerId == seekerId &&
            (o.Status == OfferStatus.PendingOwnerResponse || o.Status == OfferStatus.PendingBuyerResponse));
        if (hasOpenOffer)
            throw new BusinessRuleException("You already have an open offer on this property. Continue that negotiation instead.");

        var now = DateTime.UtcNow;
        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            SeekerId = seekerId,
            CurrentPrice = request.Price,
            Status = OfferStatus.PendingOwnerResponse,
            RoundCount = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _uow.Offers.AddAsync(offer);
        await _uow.NegotiationHistories.AddAsync(NewHistory(offer.Id, seekerId, NegotiationActionType.InitialOffer, request.Price, now));

        await _uow.SaveChangesAsync();

        return await LoadDetailAsync(offer.Id);
    }

    // ------------------------------------------------------------------ Read

    public async Task<OfferDetailDto> GetByIdAsync(Guid offerId, Guid userId, bool isAdmin)
    {
        var offer = await _uow.Offers.QueryNoTracking().Include(o => o.Property)
            .FirstOrDefaultAsync(o => o.Id == offerId)
            ?? throw new NotFoundException("Offer not found.");

        EnsureCanView(offer, userId, isAdmin);
        return await MapDetailAsync(offer);
    }

    public async Task<IReadOnlyList<NegotiationHistoryDto>> GetHistoryAsync(Guid offerId, Guid userId, bool isAdmin)
    {
        var offer = await _uow.Offers.QueryNoTracking().Include(o => o.Property)
            .FirstOrDefaultAsync(o => o.Id == offerId)
            ?? throw new NotFoundException("Offer not found.");

        EnsureCanView(offer, userId, isAdmin);

        var rows = await _uow.NegotiationHistories.QueryNoTracking()
            .Where(h => h.OfferId == offerId)
            .OrderBy(h => h.Timestamp).ThenBy(h => h.Id)
            .ToListAsync();

        var names = await _users.GetNamesAsync(rows.Select(r => r.ActionByUserId).Distinct());
        return rows.Select(h => new NegotiationHistoryDto(
            h.Id, h.OfferId, h.ActionByUserId,
            names.TryGetValue(h.ActionByUserId, out var n) ? n : "Unknown",
            h.ActionType, h.ProposedPrice, h.Timestamp)).ToList();
    }

    public async Task<PagedResult<OfferListItemDto>> GetMyOffersAsync(Guid seekerId, OfferListQuery query)
    {
        await _listValidator.ValidateAndThrowAsync(query);
        return await ListAsync(_uow.Offers.QueryNoTracking().Where(o => o.SeekerId == seekerId), query);
    }

    public async Task<PagedResult<OfferListItemDto>> GetOwnerOffersAsync(Guid ownerId, OfferListQuery query)
    {
        await _listValidator.ValidateAndThrowAsync(query);
        return await ListAsync(_uow.Offers.QueryNoTracking().Where(o => o.Property.OwnerId == ownerId), query);
    }

    // --------------------------------------------------------------- Respond

    public async Task<OfferDetailDto> RejectAsync(Guid offerId, Guid userId)
    {
        var offer = await LoadTrackedAsync(offerId);
        var actor = ResolveParticipant(offer, userId);
        OfferWorkflow.EnsureCanRespond(offer, actor);

        var now = DateTime.UtcNow;
        offer.Status = OfferStatus.Rejected;
        await _uow.NegotiationHistories.AddAsync(NewHistory(offer.Id, userId, OfferWorkflow.RejectAction(actor), offer.CurrentPrice, now));

        // Offer.RowVersion is a concurrency token: a concurrent accept/counter turns this into a 409.
        await _uow.SaveChangesAsync();
        return await LoadDetailAsync(offerId);
    }

    public async Task<OfferDetailDto> CounterAsync(Guid offerId, Guid userId, CounterOfferRequest request)
    {
        await _counterValidator.ValidateAndThrowAsync(request);

        var offer = await LoadTrackedAsync(offerId);
        var actor = ResolveParticipant(offer, userId);
        OfferWorkflow.EnsureCanRespond(offer, actor);

        if (offer.Property.Status != PropertyStatus.Available)
            throw new BusinessRuleException("This property is no longer available.");
        if (offer.RoundCount >= OfferRules.MaxRounds)
            throw new BusinessRuleException($"The maximum of {OfferRules.MaxRounds} negotiation rounds has been reached. Accept or reject the offer.");
        if (request.Price == offer.CurrentPrice)
            throw new BusinessRuleException("A counter-offer must propose a different price than the current one.");

        var now = DateTime.UtcNow;
        offer.CurrentPrice = request.Price;
        offer.RoundCount += 1;
        offer.Status = OfferWorkflow.StatusAfterCounter(actor);
        await _uow.NegotiationHistories.AddAsync(NewHistory(offer.Id, userId, OfferWorkflow.CounterAction(actor), request.Price, now));

        await _uow.SaveChangesAsync();
        return await LoadDetailAsync(offerId);
    }

    public async Task<OfferDetailDto> AcceptAsync(Guid offerId, Guid userId)
    {
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var offer = await LoadTrackedAsync(offerId);
            var actor = ResolveParticipant(offer, userId);
            OfferWorkflow.EnsureCanRespond(offer, actor);

            if (offer.Property.Status != PropertyStatus.Available)
                throw new ConflictException("This property is no longer available.");

            var ownerId = offer.Property.OwnerId;
            var newStatus = offer.Property.ListingType == ListingType.Sale ? PropertyStatus.Sold : PropertyStatus.Rented;

            var claimed = await _uow.Offers.TryClaimPropertyAsync(offer.PropertyId, ownerId, newStatus);
            if (!claimed)
                throw new ConflictException("Another offer was accepted for this property first, or the property is no longer available.");

            var now = DateTime.UtcNow;
            offer.Status = OfferStatus.Accepted;
            await _uow.NegotiationHistories.AddAsync(
                NewHistory(offer.Id, userId, OfferWorkflow.AcceptAction(actor), offer.CurrentPrice, now));

            var competing = await _uow.Offers.Query()
                .Where(o => o.PropertyId == offer.PropertyId && o.Id != offer.Id &&
                            (o.Status == OfferStatus.PendingOwnerResponse || o.Status == OfferStatus.PendingBuyerResponse))
                .ToListAsync();

            foreach (var other in competing)
            {
                other.Status = OfferStatus.Closed;
                await _uow.NegotiationHistories.AddAsync(
                    NewHistory(other.Id, ownerId, NegotiationActionType.ClosedBecausePropertyUnavailable, other.CurrentPrice, now));
            }

            await _uow.SaveChangesAsync();
            return true;
        });

        return await LoadDetailAsync(offerId);
    }



    private async Task<Offer> LoadTrackedAsync(Guid offerId)
        => await _uow.Offers.Query().Include(o => o.Property).FirstOrDefaultAsync(o => o.Id == offerId)
           ?? throw new NotFoundException("Offer not found.");

    private static OfferParticipant ResolveParticipant(Offer offer, Guid userId)
    {
        if (offer.Property.OwnerId == userId) return OfferParticipant.Owner;
        if (offer.SeekerId == userId) return OfferParticipant.Seeker;
        throw new ForbiddenException("You are not a participant in this negotiation.");
    }

    private static void EnsureCanView(Offer offer, Guid userId, bool isAdmin)
    {
        if (isAdmin) return;
        if (offer.SeekerId != userId && offer.Property.OwnerId != userId)
            throw new ForbiddenException("You do not have access to this offer.");
    }

    private static NegotiationHistory NewHistory(Guid offerId, Guid actorId, NegotiationActionType type, decimal price, DateTime timestamp)
        => new()
        {
            Id = Guid.NewGuid(),
            OfferId = offerId,
            ActionByUserId = actorId,
            ActionType = type,
            ProposedPrice = price,
            Timestamp = timestamp
        };

    private async Task<OfferDetailDto> LoadDetailAsync(Guid offerId)
    {
        var offer = await _uow.Offers.QueryNoTracking().Include(o => o.Property)
            .FirstOrDefaultAsync(o => o.Id == offerId)
            ?? throw new NotFoundException("Offer not found.");
        return await MapDetailAsync(offer);
    }

    private async Task<OfferDetailDto> MapDetailAsync(Offer o)
    {
        var names = await _users.GetNamesAsync(new[] { o.Property.OwnerId, o.SeekerId });
        string Name(Guid id) => names.TryGetValue(id, out var n) ? n : "Unknown";

        return new OfferDetailDto(
            o.Id, o.PropertyId, o.Property.Title, o.Property.Status,
            o.Property.OwnerId, Name(o.Property.OwnerId), o.SeekerId, Name(o.SeekerId),
            o.Property.Price, o.CurrentPrice, o.Status, o.RoundCount,
            OfferWorkflow.AwaitingPartyLabel(o.Status),
            Convert.ToBase64String(o.RowVersion ?? Array.Empty<byte>()),
            o.CreatedAtUtc, o.UpdatedAtUtc);
    }

    private async Task<PagedResult<OfferListItemDto>> ListAsync(IQueryable<Offer> source, OfferListQuery query)
    {
        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            source = source.Where(o => o.Status == status);
        }

        var total = await source.CountAsync();

        var rows = await source
            .OrderByDescending(o => o.UpdatedAtUtc).ThenBy(o => o.Id)
            .ToPage(query)
            .Select(o => new OfferListItemDto(
                o.Id, o.PropertyId, o.Property.Title, o.Property.City, o.Property.Status,
                o.Property.OwnerId, o.SeekerId, string.Empty, o.Property.Price, o.CurrentPrice,
                o.Status, o.RoundCount, null, o.CreatedAtUtc, o.UpdatedAtUtc))
            .ToListAsync();

        var names = await _users.GetNamesAsync(rows.Select(r => r.SeekerId).Distinct());
        var items = rows.Select(r => r with
        {
            SeekerName = names.TryGetValue(r.SeekerId, out var n) ? n : "Unknown",
            AwaitingParty = OfferWorkflow.AwaitingPartyLabel(r.Status)
        }).ToList();

        return PagedResult<OfferListItemDto>.Create(items, total, query.PageNumber, query.PageSize);
    }
}
