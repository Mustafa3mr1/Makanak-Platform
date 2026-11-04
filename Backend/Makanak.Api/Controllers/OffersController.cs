using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;

namespace Makanak.Api.Controllers;

/// <summary>
/// Negotiation Engine API: Handles property offers, counter-offers, acceptances, and rejections.
/// Uses Hybrid ID pattern: routes and DTOs use only external PublicId (Guid).
/// Fully protected by JWT Authentication; user identity is derived safely from claims.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OffersController : ControllerBase
{
    private readonly INegotiationService _negotiationService;
    private readonly ICurrentUserService _currentUserService;

    public OffersController(
        INegotiationService negotiationService,
        ICurrentUserService currentUserService)
    {
        _negotiationService = negotiationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates an initial offer on an available property.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateInitialOffer(
        [FromBody] CreateOfferRequestDto request,
        CancellationToken cancellationToken)
    {
        var seekerInternalId = _currentUserService.GetCurrentUserId();
        var offer = await _negotiationService.CreateInitialOfferAsync(request, seekerInternalId, cancellationToken);
        return CreatedAtAction(nameof(CreateInitialOffer), new { id = offer.Id }, offer);
    }

    /// <summary>
    /// Submits a counter-offer. Enforces maximum round invariant.
    /// </summary>
    [HttpPut("{id:guid}/counter")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CounterOffer(
        [FromRoute] Guid id,
        [FromBody] CounterOfferRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorInternalId = _currentUserService.GetCurrentUserId();
        var updatedOffer = await _negotiationService.CounterOfferAsync(id, request, actorInternalId, cancellationToken);
        return Ok(updatedOffer);
    }

    /// <summary>
    /// Accepts a specific offer under strict Unit of Work transactional isolation.
    /// </summary>
    [HttpPut("{id:guid}/accept")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AcceptOffer(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var actorInternalId = _currentUserService.GetCurrentUserId();
        var acceptedOffer = await _negotiationService.AcceptOfferAsync(id, actorInternalId, cancellationToken);
        return Ok(acceptedOffer);
    }

    /// <summary>
    /// Explicitly rejects an offer.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RejectOffer(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var actorInternalId = _currentUserService.GetCurrentUserId();
        var rejectedOffer = await _negotiationService.RejectOfferAsync(id, actorInternalId, cancellationToken);
        return Ok(rejectedOffer);
    }
}
