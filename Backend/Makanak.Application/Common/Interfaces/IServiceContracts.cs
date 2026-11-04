using Makanak.Application.DTOs.Auth;
using Makanak.Application.DTOs.Properties;
using Makanak.Application.DTOs.Offers;

namespace Makanak.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

public interface IPropertyService
{
    /// <param name="ownerInternalId">Resolved from JWT claims — internal long Id of the authenticated owner.</param>
    Task<PropertyDto> CreatePropertyAsync(CreatePropertyRequestDto request, long ownerInternalId, CancellationToken cancellationToken = default);

    /// <param name="publicId">External Guid from API route — never the internal Id.</param>
    Task<PropertyDto?> GetPropertyByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyDto>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);
}

public interface INegotiationService
{
    /// <param name="seekerInternalId">Internal long Id resolved from JWT claims.</param>
    Task<OfferDto> CreateInitialOfferAsync(CreateOfferRequestDto request, long seekerInternalId, CancellationToken cancellationToken = default);

    /// <param name="offerPublicId">External Guid from API route.</param>
    /// <param name="actorInternalId">Internal long Id resolved from JWT claims.</param>
    Task<OfferDto> CounterOfferAsync(Guid offerPublicId, CounterOfferRequestDto request, long actorInternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomic Unit of Work transaction:
    /// 1. Marks target offer Accepted.
    /// 2. Marks Property Unavailable.
    /// 3. Bulk-rejects all competing Pending/Countered offers for this property.
    /// 4. Appends NegotiationHistory records for all actions (FR-09).
    /// </summary>
    Task<OfferDto> AcceptOfferAsync(Guid offerPublicId, long actorInternalId, CancellationToken cancellationToken = default);

    Task<OfferDto> RejectOfferAsync(Guid offerPublicId, long actorInternalId, CancellationToken cancellationToken = default);
}
