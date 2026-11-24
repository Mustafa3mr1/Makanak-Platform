using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Dashboard;

namespace Makanak.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OwnerDashboardDto> GetOwnerDashboardAsync(long ownerId, CancellationToken cancellationToken = default)
    {
        var (totalProps, availableProps) = await _unitOfWork.Properties.GetOwnerPropertyCountsAsync(ownerId, cancellationToken);
        var (pendingOffers, totalOffers) = await _unitOfWork.Offers.GetOwnerOfferCountsAsync(ownerId, cancellationToken);

        return new OwnerDashboardDto(
            TotalProperties: totalProps,
            AvailableProperties: availableProps,
            PendingOffers: pendingOffers,
            TotalOffers: totalOffers
        );
    }

    public async Task<SeekerDashboardDto> GetSeekerDashboardAsync(long seekerId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Offers.GetSeekerDashboardCountsAsync(seekerId, cancellationToken);
    }
}
