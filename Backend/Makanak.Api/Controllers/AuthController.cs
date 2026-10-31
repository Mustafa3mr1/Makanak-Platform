using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Makanak.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }


    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        => Ok(await _auth.LoginAsync(request));
}
