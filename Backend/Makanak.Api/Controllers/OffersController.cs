using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Enums;

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
    private readonly IValidator<CreateOfferRequestDto> _createOfferValidator;
    private readonly IValidator<CounterOfferRequestDto> _counterOfferValidator;

    public OffersController(
        INegotiationService negotiationService,
        ICurrentUserService currentUserService,
        IValidator<CreateOfferRequestDto> createOfferValidator,
        IValidator<CounterOfferRequestDto> counterOfferValidator)
    {
        _negotiationService = negotiationService;
        _currentUserService = currentUserService;
        _createOfferValidator = createOfferValidator;
        _counterOfferValidator = counterOfferValidator;
    }

    /// <summary>
    /// Gets all offers submitted by the authenticated seeker.
    /// Supports pagination and optional status filter.
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedResult<OfferListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOffers(
        [FromQuery] OfferStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var seekerInternalId = _currentUserService.GetCurrentUserId();
        var result = await _negotiationService.GetSeekerOffersAsync(seekerInternalId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets all incoming offers on properties owned by the authenticated owner.
    /// Supports pagination and optional status and property filters.
    /// </summary>
    [HttpGet("incoming")]
    [ProducesResponseType(typeof(PagedResult<OfferListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIncomingOffers(
        [FromQuery] OfferStatus? status,
        [FromQuery] Guid? propertyPublicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var result = await _negotiationService.GetOwnerIncomingOffersAsync(ownerInternalId, status, propertyPublicId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of an offer, including computed allowedActions and full audit history.
    /// ONLY accessible to the offer's seeker or property's owner. Everyone else gets 404 (IDOR protection).
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(OfferDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOfferById(
        [FromRoute] Guid publicId,
        CancellationToken cancellationToken)
    {
        var callerInternalId = _currentUserService.GetCurrentUserId();
        var offer = await _negotiationService.GetOfferDetailsAsync(publicId, callerInternalId, cancellationToken);
        return Ok(offer);
    }

    /// <summary>
    /// Creates an initial offer on an available property.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OfferDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateInitialOffer(
        [FromBody] CreateOfferRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _createOfferValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var seekerInternalId = _currentUserService.GetCurrentUserId();
        var offer = await _negotiationService.CreateInitialOfferAsync(request, seekerInternalId, cancellationToken);
        return CreatedAtAction(nameof(GetOfferById), new { publicId = offer.PublicId }, offer);
    }

    /// <summary>
    /// Submits a counter-offer. Enforces maximum 2-round negotiation invariant.
    /// </summary>
    [HttpPut("{publicId:guid}/counter")]
    [ProducesResponseType(typeof(OfferDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CounterOffer(
        [FromRoute] Guid publicId,
        [FromBody] CounterOfferRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _counterOfferValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var actorInternalId = _currentUserService.GetCurrentUserId();
        var updatedOffer = await _negotiationService.CounterOfferAsync(publicId, request, actorInternalId, cancellationToken);
        return Ok(updatedOffer);
    }

    /// <summary>
    /// Accepts a specific offer under strict Unit of Work transactional isolation.
    /// Atomically marks property as Sold and bulk-rejects competing offers.
    /// </summary>
    [HttpPut("{publicId:guid}/accept")]
    [ProducesResponseType(typeof(OfferDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptOffer(
        [FromRoute] Guid publicId,
        CancellationToken cancellationToken)
    {
        var actorInternalId = _currentUserService.GetCurrentUserId();
        var acceptedOffer = await _negotiationService.AcceptOfferAsync(publicId, actorInternalId, cancellationToken);
        return Ok(acceptedOffer);
    }

    /// <summary>
    /// Explicitly rejects an offer (or withdraws if seeker on pending offer).
    /// </summary>
    [HttpPut("{publicId:guid}/reject")]
    [ProducesResponseType(typeof(OfferDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectOffer(
        [FromRoute] Guid publicId,
        CancellationToken cancellationToken)
    {
        var actorInternalId = _currentUserService.GetCurrentUserId();
        var rejectedOffer = await _negotiationService.RejectOfferAsync(publicId, actorInternalId, cancellationToken);
        return Ok(rejectedOffer);
    }
}
