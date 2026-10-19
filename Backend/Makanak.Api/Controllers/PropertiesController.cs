using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Properties;

namespace Makanak.Api.Controllers;

/// <summary>
/// Geospatial Property Management Controller for property listings and nearby searches.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IUnitOfWork _unitOfWork;

    public PropertiesController(IPropertyService propertyService, IUnitOfWork unitOfWork)
    {
        _propertyService = propertyService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lists a new real estate property with NetTopologySuite geospatial Point coordinates (SRID 4326).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProperty(
        [FromBody] CreatePropertyRequestDto request,
        [FromHeader(Name = "X-User-Id")] Guid? headerOwnerId,
        CancellationToken cancellationToken)
    {
        // Owner ID retrieved from Claims/Auth context or fallback demo header
        var ownerId = headerOwnerId ?? Guid.NewGuid();

        var created = await _propertyService.CreatePropertyAsync(request, ownerId, cancellationToken);
        return CreatedAtAction(nameof(GetPropertyById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Performs a high-performance spatial proximity query finding available properties within a radius (e.g. 5km).
    /// </summary>
    /// <param name="lat">User Latitude in WGS 84</param>
    /// <param name="lng">User Longitude in WGS 84</param>
    /// <param name="radiusKm">Search radius in Kilometers (default: 5.0 km)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearbyProperties(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 5.0,
        CancellationToken cancellationToken = default)
    {
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            return BadRequest("Latitude must be between -90 and 90, and Longitude between -180 and 180.");
        }

        var nearbyProperties = await _propertyService.GetNearbyPropertiesAsync(lat, lng, radiusKm, cancellationToken);
        return Ok(nearbyProperties);
    }

    /// <summary>
    /// Retrieves a single property by its unique Guid identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPropertyById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var property = await _propertyService.GetPropertyByIdAsync(id, cancellationToken);
        if (property == null)
        {
            return NotFound($"Property with Id '{id}' not found.");
        }

        return Ok(property);
    }
}
