using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Properties;

namespace Makanak.Api.Controllers;

/// <summary>
/// Property Management API: CRUD for property listings.
/// Uses Hybrid ID pattern: external PublicId (Guid) for routes & DTOs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ICurrentUserService _currentUserService;

    public PropertiesController(
        IPropertyService propertyService,
        ICurrentUserService currentUserService)
    {
        _propertyService = propertyService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new property listing. Requires authentication.
    /// Owner identity is resolved securely from JWT claims.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProperty(
        [FromBody] CreatePropertyRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var created = await _propertyService.CreatePropertyAsync(request, ownerInternalId, cancellationToken);
        return CreatedAtAction(nameof(GetPropertyById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Returns all property listings. Publicly accessible.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProperties(CancellationToken cancellationToken)
    {
        var properties = await _propertyService.GetAllPropertiesAsync(cancellationToken);
        return Ok(properties);
    }

    /// <summary>
    /// Retrieves a single property by its unique external PublicId (Guid). Publicly accessible.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPropertyById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var property = await _propertyService.GetPropertyByPublicIdAsync(id, cancellationToken);
        if (property == null)
        {
            return NotFound($"Property with Id '{id}' not found.");
        }

        return Ok(property);
    }
}
