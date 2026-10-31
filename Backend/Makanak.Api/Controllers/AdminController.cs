using Makanak.API.Extensions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Makanak.API.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[Authorize(Roles = RoleNames.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;


    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> Users([FromQuery] AdminUserQuery query)
        => Ok(await _admin.ListUsersAsync(query));


    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetUser(Guid userId)
        => Ok(await _admin.GetUserAsync(userId));


    [HttpPost("users/{userId:guid}/deactivate")]
    public async Task<ActionResult<AdminUserDto>> DeactivateUser(Guid userId)
        => Ok(await _admin.DeactivateUserAsync(User.GetUserId(), userId));


    [HttpPost("users/{userId:guid}/reactivate")]
    public async Task<ActionResult<AdminUserDto>> ReactivateUser(Guid userId)
        => Ok(await _admin.ReactivateUserAsync(userId));

    [HttpGet("properties")]
    public async Task<ActionResult<PagedResult<AdminPropertyDto>>> Properties([FromQuery] AdminPropertyQuery query)
        => Ok(await _admin.ListPropertiesAsync(query));

    [HttpPost("properties/{propertyId:guid}/deactivate")]
    public async Task<ActionResult<AdminPropertyDto>> DeactivateProperty(Guid propertyId)
        => Ok(await _admin.DeactivatePropertyAsync(propertyId));

    [HttpPost("properties/{propertyId:guid}/reactivate")]
    public async Task<ActionResult<AdminPropertyDto>> ReactivateProperty(Guid propertyId)
        => Ok(await _admin.ReactivatePropertyAsync(propertyId));

    [HttpGet("stats")]
    public async Task<ActionResult<PlatformStatsDto>> Stats()
        => Ok(await _admin.GetStatsAsync());
}
