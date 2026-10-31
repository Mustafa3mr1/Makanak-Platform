using FluentValidation;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Property;

namespace Makanak.Application.Validators;

public abstract class PropertyFieldsValidator<T> : AbstractValidator<T> where T : IPropertyFields
{
    protected PropertyFieldsValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Price).MoneyRules();
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PropertyType).IsInEnum();
        RuleFor(x => x.ListingType).IsInEnum();
    }
}

public sealed class CreatePropertyRequestValidator : PropertyFieldsValidator<CreatePropertyRequest> { }

public sealed class UpdatePropertyRequestValidator : PropertyFieldsValidator<UpdatePropertyRequest>
{
    public UpdatePropertyRequestValidator()
    {
        RuleFor(x => x.RowVersion).Must(CommonValidation.IsBase64).WithMessage("RowVersion must be a base64 string.");
    }
}

public sealed class ChangePropertyStatusRequestValidator : AbstractValidator<ChangePropertyStatusRequest>
{
    public ChangePropertyStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.RowVersion).Must(CommonValidation.IsBase64).WithMessage("RowVersion must be a base64 string.");
    }
}

public sealed class PropertySearchQueryValidator : PagedQueryValidator<PropertySearchQuery>
{
    public PropertySearchQueryValidator()
    {
        RuleFor(x => x.Keyword).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x).Must(x => x.MinPrice <= x.MaxPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithName("MinPrice").WithMessage("MinPrice must not be greater than MaxPrice.");
        RuleFor(x => x.PropertyType).IsInEnum();
        RuleFor(x => x.ListingType).IsInEnum();
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();
    }
}
