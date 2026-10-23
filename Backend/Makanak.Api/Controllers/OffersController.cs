using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;

namespace Makanak.Api.Controllers;

/// <summary>
/// Negotiation Engine API: Handles property offers, counter-offers, acceptances, and rejections.
/// Every state transition is recorded in NegotiationHistory (FR-09).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OffersController : ControllerBase
{
    private readonly INegotiationService _negotiationService;
    private readonly IUnitOfWork _unitOfWork;

    public OffersController(
        INegotiationService negotiationService,
        IUnitOfWork unitOfWork)
    {
        _negotiationService = negotiationService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates an initial offer on an available property.
    /// Appends a NegotiationHistory record with ActionType=Submitted (FR-09).
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
    /// Enforces the strict domain invariant: RoundCount cannot exceed 2.
    /// Appends a NegotiationHistory record with ActionType=Countered (FR-09).
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
    /// Atomic transaction:
    /// 1. Sets the target offer state to Accepted.
    /// 2. Updates Property Status to Unavailable.
    /// 3. Bulk-rejects all other pending offers for this property.
    /// 4. Appends NegotiationHistory rows for all actions (FR-09).
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
    /// Appends a NegotiationHistory record with ActionType=Rejected (FR-09).
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
