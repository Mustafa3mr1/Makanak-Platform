using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Models;

public sealed record UserAccountInfo(Guid Id, string FullName, string Email, UserType UserType, bool IsActive, DateTime CreatedAtUtc);

public sealed record IdentityFailure(string Code, string Description);

public sealed record UserCreationResult(bool Succeeded, UserAccountInfo? User, IReadOnlyList<IdentityFailure> Errors)
{
    public bool IsDuplicate => Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
}

public enum CredentialStatus { Success, InvalidCredentials, LockedOut, Inactive }

public sealed record CredentialCheckResult(CredentialStatus Status, UserAccountInfo? User);

public sealed record UserCounts(IReadOnlyDictionary<UserType, int> ByType, int Active, int Total);

public sealed record JwtTokenResult(string Token, DateTime ExpiresAtUtc);
