using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Properties;

namespace Makanak.Api.Controllers;

/// <summary>
/// Property Management API: CRUD for property listings (FR-01, FR-04 MVP scope).
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
    /// Creates a new property listing with City and Address location fields (MVP scope).
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
    /// Returns all property listings ordered by creation date (newest first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProperties(CancellationToken cancellationToken)
    {
        var properties = await _propertyService.GetAllPropertiesAsync(cancellationToken);
        return Ok(properties);
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
