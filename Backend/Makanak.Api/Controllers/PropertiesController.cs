using Makanak.API.Extensions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Property;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Makanak.API.Controllers;

[ApiController]
[Route("api/properties")]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _properties;

    public PropertiesController(IPropertyService properties) => _properties = properties;

    [HttpPost]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PropertyDetailDto>> Create([FromBody] CreatePropertyRequest request)
    {
        var created = await _properties.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

 
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PropertyListItemDto>>> Search([FromQuery] PropertySearchQuery query)
        => Ok(await _properties.SearchAsync(query));


    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PropertyDetailDto>> GetById(Guid id)
        => Ok(await _properties.GetByIdAsync(id, User.TryGetUserId(), User.IsInRole(RoleNames.Admin)));


    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PropertyDetailDto>> Update(Guid id, [FromBody] UpdatePropertyRequest request)
        => Ok(await _properties.UpdateAsync(id, User.GetUserId(), request));

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PropertyDetailDto>> ChangeStatus(Guid id, [FromBody] ChangePropertyStatusRequest request)
        => Ok(await _properties.ChangeStatusAsync(id, User.GetUserId(), User.IsInRole(RoleNames.Admin), request));


    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Owner},{RoleNames.Admin}")]

    public async Task<IActionResult> Delete(Guid id)
    {
        await _properties.DeleteAsync(id, User.GetUserId(), User.IsInRole(RoleNames.Admin));
        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = RoleNames.Owner)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageRules.MaxRequestBytes)]
    public async Task<ActionResult<PropertyImageDto>> UploadImage(Guid id, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var image = await _properties.AddImageAsync(id, User.GetUserId(), stream, file.Length);
        return CreatedAtAction(nameof(GetById), new { id }, image);
    }


    [HttpPut("{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId)
    {
        await _properties.SetPrimaryImageAsync(id, imageId, User.GetUserId());
        return NoContent();
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {
        await _properties.DeleteImageAsync(id, imageId, User.GetUserId());
        return NoContent();
    }
}
