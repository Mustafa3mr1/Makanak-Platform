using Makanak.API.Extensions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;
using Makanak.Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Makanak.API.Controllers;

[ApiController]
[Route("api/user")]
[Produces("application/json")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _users;

    public UserController(IUserService users) => _users = users;

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me()
        => Ok(await _users.GetProfileAsync(User.GetUserId()));

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe([FromBody] UpdateProfileRequest request)
        => Ok(await _users.UpdateProfileAsync(User.GetUserId(), request));


    [HttpGet("owner/dashboard")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<OwnerDashboardDto>> OwnerDashboard([FromQuery] PagedQuery paging)
        => Ok(await _users.GetOwnerDashboardAsync(User.GetUserId(), paging));

    [HttpGet("buyer/dashboard")]
    [Authorize(Roles = RoleNames.BuyerTenant)]
    public async Task<ActionResult<BuyerDashboardDto>> BuyerDashboard([FromQuery] OfferListQuery query)
        => Ok(await _users.GetBuyerDashboardAsync(User.GetUserId(), query));
}
