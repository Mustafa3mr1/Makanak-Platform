using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Auth;
using Makanak.Application.DTOs.Properties;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<UserProfileDto> GetCurrentUserProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateUserProfileAsync(long userId, UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default);
}

public interface IPropertyService
{
    Task<PropertyDetailsDto> CreatePropertyAsync(CreatePropertyRequestDto request, long ownerInternalId, CancellationToken cancellationToken = default);
    Task<PropertyDetailsDto?> GetPropertyByPublicIdAsync(Guid publicId, long? callerInternalId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PropertyListItemDto>> GetPropertiesAsync(PropertyFilterQueryDto filter, CancellationToken cancellationToken = default);
    Task<PagedResult<PropertyListItemDto>> GetOwnerPropertiesAsync(long ownerInternalId, PropertyStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PropertyDetailsDto> UpdatePropertyAsync(Guid publicId, UpdatePropertyRequestDto request, long ownerInternalId, CancellationToken cancellationToken = default);
    Task SoftDeletePropertyAsync(Guid publicId, long ownerInternalId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyImageItemDto>> AddImagesAsync(
        Guid propertyPublicId,
        IReadOnlyList<(Stream Stream, string Extension)> files,
        long ownerInternalId,
        CancellationToken cancellationToken = default);

    Task<PropertyImageItemDto> SetPrimaryImageAsync(
        Guid propertyPublicId,
        Guid imagePublicId,
        long ownerInternalId,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        Guid propertyPublicId,
        Guid imagePublicId,
        long ownerInternalId,
        CancellationToken cancellationToken = default);
}

public interface INegotiationService
{
    Task<OfferDetailsDto> CreateInitialOfferAsync(CreateOfferRequestDto request, long seekerInternalId, CancellationToken cancellationToken = default);
    Task<OfferDetailsDto> CounterOfferAsync(Guid offerPublicId, CounterOfferRequestDto request, long actorInternalId, CancellationToken cancellationToken = default);
    Task<OfferDetailsDto> AcceptOfferAsync(Guid offerPublicId, long actorInternalId, CancellationToken cancellationToken = default);
    Task<OfferDetailsDto> RejectOfferAsync(Guid offerPublicId, long actorInternalId, CancellationToken cancellationToken = default);
    Task<OfferDetailsDto> GetOfferDetailsAsync(Guid offerPublicId, long callerInternalId, CancellationToken cancellationToken = default);
    Task<PagedResult<OfferListItemDto>> GetSeekerOffersAsync(long seekerInternalId, OfferStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<OfferListItemDto>> GetOwnerIncomingOffersAsync(long ownerInternalId, OfferStatus? status, Guid? propertyPublicId, int page, int pageSize, CancellationToken cancellationToken = default);
}
