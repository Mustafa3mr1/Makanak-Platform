using FluentValidation;
using FluentValidation.Results;
using Makanak.Application.Common.Exceptions;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Auth;
using Makanak.Application.DTOs.User;
using Makanak.Domain.Enums;

namespace Makanak.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserAccountGateway _users;
    private readonly IJwtService _jwt;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(IUserAccountGateway users, IJwtService jwt,
        IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        _users = users;
        _jwt = jwt;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        await _registerValidator.ValidateAndThrowAsync(request);

        // Defence in depth: the validator already rejects Admin, but this is the security boundary.
        if (request.UserType is not (UserType.BuyerTenant or UserType.Owner))
            throw new ForbiddenException("Self-registration is only allowed as BuyerTenant or Owner.");

        var result = await _users.CreateAsync(request.FullName.Trim(), request.Email.Trim(), request.Password, request.UserType);

        if (!result.Succeeded || result.User is null)
        {
            if (result.IsDuplicate)
                throw new ConflictException("An account with this email already exists.");

            throw new ValidationException(result.Errors.Select(e =>
                new ValidationFailure(e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "Password" : "Registration", e.Description)));
        }

        return BuildResponse(result.User);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        await _loginValidator.ValidateAndThrowAsync(request);

        var check = await _users.ValidateCredentialsAsync(request.Email.Trim(), request.Password);

        return check.Status switch
        {
            CredentialStatus.Success when check.User is not null => BuildResponse(check.User),
            CredentialStatus.Inactive => throw new ForbiddenException("This account has been deactivated."),
            CredentialStatus.LockedOut => throw new UnauthorizedException("Too many failed attempts. Try again later."),
            _ => throw new UnauthorizedException("Invalid email or password.")
        };
    }

    private AuthResponse BuildResponse(UserAccountInfo user)
    {
        var token = _jwt.CreateToken(user);
        return new AuthResponse(token.Token, "Bearer", token.ExpiresAtUtc,
            new UserProfileDto(user.Id, user.FullName, user.Email, user.UserType, user.CreatedAtUtc));
    }
}
