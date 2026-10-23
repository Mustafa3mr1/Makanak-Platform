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
    Task<PropertyDto> CreatePropertyAsync(CreatePropertyRequestDto request, Guid ownerId, CancellationToken cancellationToken = default);
    Task<PropertyDto?> GetPropertyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PropertyDto>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);
}

public interface INegotiationService
{
    Task<OfferDto> CreateInitialOfferAsync(CreateOfferRequestDto request, Guid seekerId, CancellationToken cancellationToken = default);
    Task<OfferDto> CounterOfferAsync(Guid offerId, CounterOfferRequestDto request, Guid actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the atomic Unit of Work transaction:
    /// 1. Marks target offer as Accepted.
    /// 2. Updates Property Status to Unavailable.
    /// 3. Rejects all other pending offers for this property within a single DB transaction.
    /// Appends <see cref="NegotiationHistory"/> records for each action (FR-09).
    /// </summary>
    Task<OfferDto> AcceptOfferAsync(Guid offerId, Guid actorId, CancellationToken cancellationToken = default);

    Task<OfferDto> RejectOfferAsync(Guid offerId, Guid actorId, CancellationToken cancellationToken = default);
}

