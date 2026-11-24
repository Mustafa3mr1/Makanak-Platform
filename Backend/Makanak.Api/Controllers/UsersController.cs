using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Auth;

namespace Makanak.Api.Controllers;

/// <summary>
/// User profile endpoints for the currently authenticated user.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<UpdateUserProfileRequestDto> _updateValidator;

    public UsersController(
        IUserService userService,
        ICurrentUserService currentUserService,
        IValidator<UpdateUserProfileRequestDto> updateValidator)
    {
        _userService = userService;
        _currentUserService = currentUserService;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Gets the profile of the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var profile = await _userService.GetCurrentUserProfileAsync(currentUserId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// Updates the profile (full name, phone number) of the currently authenticated user.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCurrentUser(
        [FromBody] UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var currentUserId = _currentUserService.GetCurrentUserId();
        var updated = await _userService.UpdateUserProfileAsync(currentUserId, request, cancellationToken);
        return Ok(updated);
    }
}
