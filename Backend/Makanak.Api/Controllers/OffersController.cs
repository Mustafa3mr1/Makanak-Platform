using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;

namespace Makanak.Api.Controllers;

/// <summary>
/// Negotiation Engine API: Handles real-time property offers, counter-offers, acceptances, and rejections.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OffersController : ControllerBase
{
    private readonly INegotiationService _negotiationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _notificationService;

    public OffersController(
        INegotiationService negotiationService,
        IUnitOfWork unitOfWork,
        ISignalRNotificationService notificationService)
    {
        _negotiationService = negotiationService;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Creates an initial offer on an available property.
    /// ARCHITECTURAL NOTE: Dispatches real-time alerts via ISignalRNotificationService to the property owner.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInitialOffer(
        [FromBody] CreateOfferRequestDto request,
        [FromHeader(Name = "X-User-Id")] Guid? headerSeekerId,
        CancellationToken cancellationToken)
    {
        var seekerId = headerSeekerId ?? Guid.NewGuid();
        var offer = await _negotiationService.CreateInitialOfferAsync(request, seekerId, cancellationToken);
        return CreatedAtAction(nameof(CreateInitialOffer), new { id = offer.Id }, offer);
    }

    /// <summary>
    /// Submits a counter-offer from Property Owner or Seeker.
    /// ARCHITECTURAL NOTE: 
    /// Enforces the strict negotiation invariant: RoundCount cannot exceed 2.
    /// Fires real-time SignalR notifications upon successful persistence.
    /// </summary>
    [HttpPut("{id:guid}/counter")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CounterOffer(
        [FromRoute] Guid id,
        [FromBody] CounterOfferRequestDto request,
        [FromHeader(Name = "X-User-Id")] Guid? headerActorId,
        CancellationToken cancellationToken)
    {
        var actorId = headerActorId ?? Guid.NewGuid();
        var updatedOffer = await _negotiationService.CounterOfferAsync(id, request, actorId, cancellationToken);
        return Ok(updatedOffer);
    }

    /// <summary>
    /// Accepts a specific offer under strict Unit of Work transactional isolation.
    /// ARCHITECTURAL NOTE (ATOMIC TRANSACTION & SIGNALR LIFECYCLE):
    /// 1. Transaction Start: Begins an isolated DbContext transaction via IUnitOfWork.
    /// 2. Offer Acceptance: Sets the target offer state to 'Accepted'.
    /// 3. Property Lock: Updates the Property Status to 'Unavailable'.
    /// 4. Competing Rejection: Concurrently bulk-rejects all other pending offers for this property.
    /// 5. Transaction Commit: Persists all state mutations atomically (all-or-nothing).
    /// 6. Post-Commit SignalR: Dispatches real-time broadcast alerts to the Property group via SignalR.
    /// </summary>
    [HttpPut("{id:guid}/accept")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptOffer(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-User-Id")] Guid? headerActorId,
        CancellationToken cancellationToken)
    {
        var actorId = headerActorId ?? Guid.NewGuid();
        var acceptedOffer = await _negotiationService.AcceptOfferAsync(id, actorId, cancellationToken);
        return Ok(acceptedOffer);
    }

    /// <summary>
    /// Rejects an offer explicitly.
    /// ARCHITECTURAL NOTE: Dispatches real-time SignalR alerts to the offer author.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(typeof(OfferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectOffer(
        [FromRoute] Guid id,
        [FromHeader(Name = "X-User-Id")] Guid? headerActorId,
        CancellationToken cancellationToken)
    {
        var actorId = headerActorId ?? Guid.NewGuid();
        var rejectedOffer = await _negotiationService.RejectOfferAsync(id, actorId, cancellationToken);
        return Ok(rejectedOffer);
    }
}
