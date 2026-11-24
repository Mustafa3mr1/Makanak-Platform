namespace Makanak.Application.DTOs.Dashboard;

public sealed record OwnerDashboardDto(
    int TotalProperties,
    int AvailableProperties,
    int PendingOffers,
    int TotalOffers
);

public sealed record SeekerDashboardDto(
    int TotalOffers,
    int Pending,
    int Countered,
    int Accepted,
    int Rejected
);
