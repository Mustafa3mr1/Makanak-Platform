using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Api.Controllers;

/// <summary>
/// Property Management API: CRUD, search, filtering, and owner management.
/// Uses Hybrid ID pattern: external PublicId (Guid) exclusively.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private const int MaxImageCount = 10;
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IPropertyService _propertyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreatePropertyRequestDto> _createValidator;
    private readonly IValidator<UpdatePropertyRequestDto> _updateValidator;

    public PropertiesController(
        IPropertyService propertyService,
        ICurrentUserService currentUserService,
        IValidator<CreatePropertyRequestDto> createValidator,
        IValidator<UpdatePropertyRequestDto> updateValidator)
    {
        _propertyService = propertyService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Searches and filters publicly available property listings.
    /// Supports pagination, sorting, and multi-faceted filtering.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<PropertyListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProperties(
        [FromQuery] PropertyFilterQueryDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetPropertiesAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a property by PublicId.
    /// Returns 404 if the property is non-available and the caller is not the owner (IDOR protection).
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PropertyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPropertyById(
        [FromRoute] Guid publicId,
        CancellationToken cancellationToken)
    {
        long? callerId = _currentUserService.IsAuthenticated
            ? _currentUserService.GetCurrentUserId()
            : null;

        var property = await _propertyService.GetPropertyByPublicIdAsync(publicId, callerId, cancellationToken);
        if (property == null)
        {
            return NotFound(new { code = "not_found", message = $"Property '{publicId}' was not found." });
        }

        return Ok(property);
    }

    /// <summary>
    /// Retrieves properties owned by the authenticated owner.
    /// Supports optional status filter and pagination.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<PropertyListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProperties(
        [FromQuery] PropertyStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var result = await _propertyService.GetOwnerPropertiesAsync(ownerInternalId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new property listing.
    /// Only authenticated users (Owners) may create listings.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PropertyDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProperty(
        [FromBody] CreatePropertyRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var created = await _propertyService.CreatePropertyAsync(request, ownerInternalId, cancellationToken);
        return CreatedAtAction(nameof(GetPropertyById), new { publicId = created.PublicId }, created);
    }

    /// <summary>
    /// Updates an existing property listing owned by the caller.
    /// Validates RowVersion for optimistic concurrency (returns 409 on conflict).
    /// </summary>
    [HttpPut("{publicId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PropertyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProperty(
        [FromRoute] Guid publicId,
        [FromBody] UpdatePropertyRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var updated = await _propertyService.UpdatePropertyAsync(publicId, request, ownerInternalId, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Soft deletes a property listing owned by the caller.
    /// Preserves audit logs and negotiation history.
    /// </summary>
    [HttpDelete("{publicId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProperty(
        [FromRoute] Guid publicId,
        CancellationToken cancellationToken)
    {
        var ownerInternalId = _currentUserService.GetCurrentUserId();
        await _propertyService.SoftDeletePropertyAsync(publicId, ownerInternalId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Uploads up to 10 images for a property (jpg, png, webp, max 5 MB each).
    /// Content-type and magic bytes are verified.
    /// </summary>
    [HttpPost("{publicId:guid}/images")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyImageItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImages(
        [FromRoute] Guid publicId,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            throw new DomainException("No files were provided for upload.", "validation_failed");
        }

        if (files.Count > MaxImageCount)
        {
            throw new DomainException($"Cannot upload more than {MaxImageCount} images at once.", "validation_failed");
        }

        var validatedFiles = new List<(Stream Stream, string Extension)>();

        foreach (var file in files)
        {
            if (file.Length == 0)
                throw new DomainException("File cannot be empty.", "validation_failed");

            if (file.Length > MaxImageSizeBytes)
                throw new DomainException($"File '{file.FileName}' exceeds the maximum allowed size of 5 MB.", "validation_failed");

            var ext = ValidateAndGetExtension(file);
            validatedFiles.Add((file.OpenReadStream(), ext));
        }

        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var result = await _propertyService.AddImagesAsync(publicId, validatedFiles, ownerInternalId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sets a specific image as the primary image of the property.
    /// </summary>
    [HttpPut("{publicId:guid}/images/{imageId:guid}/primary")]
    [Authorize]
    [ProducesResponseType(typeof(PropertyImageItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(
        [FromRoute] Guid publicId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var ownerInternalId = _currentUserService.GetCurrentUserId();
        var result = await _propertyService.SetPrimaryImageAsync(publicId, imageId, ownerInternalId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes an image from a property.
    /// If the primary image is deleted, another image is automatically promoted.
    /// </summary>
    [HttpDelete("{publicId:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(
        [FromRoute] Guid publicId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var ownerInternalId = _currentUserService.GetCurrentUserId();
        await _propertyService.DeleteImageAsync(publicId, imageId, ownerInternalId, cancellationToken);
        return NoContent();
    }

    private static string ValidateAndGetExtension(IFormFile file)
    {
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new DomainException($"Unsupported content-type '{file.ContentType}'. Allowed types: JPEG, PNG, WebP.", "validation_failed");
        }

        // Read magic bytes
        using var stream = file.OpenReadStream();
        var buffer = new byte[12];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        if (bytesRead < 8)
        {
            throw new DomainException("Invalid file format: header is too short.", "validation_failed");
        }

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
        {
            return ".jpg";
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
            buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
        {
            return ".png";
        }

        // WebP: RIFF (bytes 0-3) and WEBP (bytes 8-11)
        if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
            buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
        {
            return ".webp";
        }

        throw new DomainException("File signature (magic bytes) does not match JPEG, PNG, or WebP format.", "validation_failed");
    }
}
