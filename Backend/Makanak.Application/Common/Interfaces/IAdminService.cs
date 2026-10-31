using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Admin;

namespace Makanak.Application.Common.Interfaces;

public interface IAdminService
{
    Task<PagedResult<AdminUserDto>> ListUsersAsync(AdminUserQuery query);
    Task<AdminUserDetailDto> GetUserAsync(Guid userId);
    Task<AdminUserDto> DeactivateUserAsync(Guid adminId, Guid userId);
    Task<AdminUserDto> ReactivateUserAsync(Guid userId);
    Task<PagedResult<AdminPropertyDto>> ListPropertiesAsync(AdminPropertyQuery query);
    Task<AdminPropertyDto> DeactivatePropertyAsync(Guid propertyId);
    Task<AdminPropertyDto> ReactivatePropertyAsync(Guid propertyId);
    Task<PlatformStatsDto> GetStatsAsync();
}
