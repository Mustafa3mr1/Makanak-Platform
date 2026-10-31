using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Makanak.Infrastructure.Identity;

public sealed class UserAccountGateway : IUserAccountGateway
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccountGateway(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<UserCreationResult> CreateAsync(string fullName, string email, string password, UserType userType)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            UserType = userType,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded) return Failure(create);

        var addRole = await _userManager.AddToRoleAsync(user, RoleNames.For(userType));
        if (!addRole.Succeeded)
        {
            // Keep UserType and roles consistent: never leave a user without its role.
            await _userManager.DeleteAsync(user);
            return Failure(addRole);
        }

        return new UserCreationResult(true, Map(user), Array.Empty<IdentityFailure>());
    }

    public async Task<CredentialCheckResult> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return new(CredentialStatus.InvalidCredentials, null);

        if (await _userManager.IsLockedOutAsync(user)) return new(CredentialStatus.LockedOut, null);

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return new(CredentialStatus.InvalidCredentials, null);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (!user.IsActive) return new(CredentialStatus.Inactive, null);

        // Fail closed if the stored UserType and the Identity role ever diverge.
        if (!await _userManager.IsInRoleAsync(user, RoleNames.For(user.UserType)))
            return new(CredentialStatus.InvalidCredentials, null);

        return new(CredentialStatus.Success, Map(user));
    }

    public async Task<UserAccountInfo?> FindByIdAsync(Guid id)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> ids)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return new Dictionary<Guid, string>();

        return await _userManager.Users.AsNoTracking()
            .Where(u => list.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
    }

    public async Task<(IReadOnlyList<UserAccountInfo> Items, int Total)> ListAsync(
        string? search, UserType? userType, bool? isActive, int pageNumber, int pageSize)
    {
        var q = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.FullName.Contains(search) || (u.Email != null && u.Email.Contains(search)));
        if (userType.HasValue) q = q.Where(u => u.UserType == userType.Value);
        if (isActive.HasValue) q = q.Where(u => u.IsActive == isActive.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(u => u.CreatedAtUtc).ThenBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(u => new UserAccountInfo(u.Id, u.FullName, u.Email ?? string.Empty, u.UserType, u.IsActive, u.CreatedAtUtc))
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> UpdateFullNameAsync(Guid id, string fullName)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return false;
        user.FullName = fullName;
        return (await _userManager.UpdateAsync(user)).Succeeded;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return false;
        user.IsActive = isActive;
        return (await _userManager.UpdateAsync(user)).Succeeded;
    }

    public async Task<UserCounts> GetCountsAsync()
    {
        var rows = await _userManager.Users.AsNoTracking()
            .GroupBy(u => u.UserType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();
        var active = await _userManager.Users.AsNoTracking().CountAsync(u => u.IsActive);

        return new UserCounts(rows.ToDictionary(r => r.Type, r => r.Count), active, rows.Sum(r => r.Count));
    }

    private static UserCreationResult Failure(IdentityResult result)
        => new(false, null, result.Errors.Select(e => new IdentityFailure(e.Code, e.Description)).ToList());

    private static UserAccountInfo Map(ApplicationUser u)
        => new(u.Id, u.FullName, u.Email ?? string.Empty, u.UserType, u.IsActive, u.CreatedAtUtc);
}
