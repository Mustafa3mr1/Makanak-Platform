using Makanak.Application.Common.Models;
using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Interfaces;

public interface IUserAccountGateway
{
    Task<UserCreationResult> CreateAsync(string fullName, string email, string password, UserType userType);
    Task<CredentialCheckResult> ValidateCredentialsAsync(string email, string password);
    Task<UserAccountInfo?> FindByIdAsync(Guid id);
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> ids);
    Task<(IReadOnlyList<UserAccountInfo> Items, int Total)> ListAsync(string? search, UserType? userType, bool? isActive, int pageNumber, int pageSize);
    Task<bool> UpdateFullNameAsync(Guid id, string fullName);
    Task<bool> SetActiveAsync(Guid id, bool isActive);
    Task<UserCounts> GetCountsAsync();
}
