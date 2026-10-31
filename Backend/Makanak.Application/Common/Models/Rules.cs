using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Models;

public static class RoleNames
{
    public const string BuyerTenant = "BuyerTenant";
    public const string Owner = "Owner";
    public const string Admin = "Admin";

    public static string For(UserType type) => type switch
    {
        UserType.BuyerTenant => BuyerTenant,
        UserType.Owner => Owner,
        UserType.Admin => Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Undefined user type.")
    };

    public static readonly string[] All = { BuyerTenant, Owner, Admin };
}

public static class PriceRules
{
    public const decimal MaxPrice = 1_000_000_000_000m;
}

public static class OfferRules
{
    public const int MaxRounds = 10;
}

public static class ImageRules
{
    public const int MaxImagesPerProperty = 10;
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    public const long MaxRequestBytes = MaxFileSizeBytes + 512 * 1024;
}
