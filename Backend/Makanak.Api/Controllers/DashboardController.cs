using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Dashboard;

namespace Makanak.Api.Controllers;

/// <summary>
/// Dashboard aggregation endpoints for Owners and Seekers.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Gets aggregation statistics for the authenticated Property Owner.
    /// Returns total properties, available properties, pending offers, and total offers.
    /// </summary>
    [HttpGet("owner")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(OwnerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOwnerDashboard(CancellationToken cancellationToken)
    {
        var ownerId = _currentUserService.GetCurrentUserId();
        var result = await _dashboardService.GetOwnerDashboardAsync(ownerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets aggregation statistics for the authenticated Property Seeker.
    /// Returns total offers submitted and breakdown by status (pending, countered, accepted, rejected).
    /// </summary>
    [HttpGet("seeker")]
    [Authorize(Roles = "Seeker")]
    [ProducesResponseType(typeof(SeekerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSeekerDashboard(CancellationToken cancellationToken)
    {
        var seekerId = _currentUserService.GetCurrentUserId();
        var result = await _dashboardService.GetSeekerDashboardAsync(seekerId, cancellationToken);
        return Ok(result);
    }
}
