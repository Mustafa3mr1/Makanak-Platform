using Makanak.API.Extensions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Makanak.API.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize]
public class OffersController : ControllerBase
{
    private const string Participants = $"{RoleNames.BuyerTenant},{RoleNames.Owner}";
    private readonly IOfferService _offers;

    public OffersController(IOfferService offers) => _offers = offers;

    [HttpPost("api/properties/{propertyId:guid}/offers")]
    [Authorize(Roles = RoleNames.BuyerTenant)]
    public async Task<ActionResult<OfferDetailDto>> Submit(Guid propertyId, [FromBody] SubmitOfferRequest request)
    {
        var offer = await _offers.SubmitAsync(propertyId, User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { offerId = offer.Id }, offer);
    }

    [HttpGet("api/offers/{offerId:guid}")]
    public async Task<ActionResult<OfferDetailDto>> GetById(Guid offerId)
        => Ok(await _offers.GetByIdAsync(offerId, User.GetUserId(), User.IsInRole(RoleNames.Admin)));

    [HttpGet("api/offers/{offerId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<NegotiationHistoryDto>>> GetHistory(Guid offerId)
        => Ok(await _offers.GetHistoryAsync(offerId, User.GetUserId(), User.IsInRole(RoleNames.Admin)));

    [HttpGet("api/offers/my-offers")]
    [Authorize(Roles = RoleNames.BuyerTenant)]

    public async Task<ActionResult<PagedResult<OfferListItemDto>>> MyOffers([FromQuery] OfferListQuery query)
        => Ok(await _offers.GetMyOffersAsync(User.GetUserId(), query));

    [HttpGet("api/owner/offers")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PagedResult<OfferListItemDto>>> OwnerOffers([FromQuery] OfferListQuery query)
        => Ok(await _offers.GetOwnerOffersAsync(User.GetUserId(), query));

    [HttpPost("api/offers/{offerId:guid}/accept")]
    [Authorize(Roles = Participants)]
    public async Task<ActionResult<OfferDetailDto>> Accept(Guid offerId)
        => Ok(await _offers.AcceptAsync(offerId, User.GetUserId()));


    [HttpPost("api/offers/{offerId:guid}/reject")]
    [Authorize(Roles = Participants)]
    public async Task<ActionResult<OfferDetailDto>> Reject(Guid offerId)
        => Ok(await _offers.RejectAsync(offerId, User.GetUserId()));

    [HttpPost("api/offers/{offerId:guid}/counter")]
    [Authorize(Roles = Participants)]

    public async Task<ActionResult<OfferDetailDto>> Counter(Guid offerId, [FromBody] CounterOfferRequest request)
        => Ok(await _offers.CounterAsync(offerId, User.GetUserId(), request));
}
