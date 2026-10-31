using FluentValidation;
using Makanak.Application.Common.Models;

namespace Makanak.Application.Validators;

public static class CommonValidation
{
    public static bool IsBase64(string? value)
    {
        if (value is null) return true; // optional
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    public static IRuleBuilderOptions<T, decimal> MoneyRules<T>(this IRuleBuilder<T, decimal> rule)
        => rule.GreaterThan(0).WithMessage("Price must be greater than zero.")
               .LessThanOrEqualTo(PriceRules.MaxPrice).WithMessage($"Price must not exceed {PriceRules.MaxPrice:N0}.")
               .PrecisionScale(18, 2, false).WithMessage("Price may have at most 2 decimal places.");
}

public abstract class PagedQueryValidator<T> : AbstractValidator<T> where T : PagedQuery
{
    protected PagedQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be 1 or greater.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRules.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationRules.MaxPageSize}.");
    }
}
