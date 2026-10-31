using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;
using Makanak.Application.DTOs.User;

namespace Makanak.Application.Common.Interfaces;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<OwnerDashboardDto> GetOwnerDashboardAsync(Guid ownerId, PagedQuery paging);
    Task<BuyerDashboardDto> GetBuyerDashboardAsync(Guid seekerId, OfferListQuery query);
}
