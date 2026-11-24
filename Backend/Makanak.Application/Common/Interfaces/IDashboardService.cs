using Makanak.Application.DTOs.Dashboard;

namespace Makanak.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<OwnerDashboardDto> GetOwnerDashboardAsync(long ownerId, CancellationToken cancellationToken = default);
    Task<SeekerDashboardDto> GetSeekerDashboardAsync(long seekerId, CancellationToken cancellationToken = default);
}
