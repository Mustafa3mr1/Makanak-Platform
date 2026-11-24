using FluentValidation;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Enums;

namespace Makanak.Application.Validators;

public class CreatePropertyRequestValidator : AbstractValidator<CreatePropertyRequestDto>
{
    public CreatePropertyRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Property title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.ListingType)
            .IsInEnum().WithMessage("Invalid listing type.");

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Invalid property type.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(250).WithMessage("Address must not exceed 250 characters.");

        RuleFor(x => x.Bedrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bedrooms cannot be negative.");

        RuleFor(x => x.Bathrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bathrooms cannot be negative.");

        RuleFor(x => x.AreaSqm)
            .GreaterThan(0).WithMessage("Area in square meters must be greater than zero.");
    }
}

public class UpdatePropertyRequestValidator : AbstractValidator<UpdatePropertyRequestDto>
{
    public UpdatePropertyRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Property title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.ListingType)
            .IsInEnum().WithMessage("Invalid listing type.");

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Invalid property type.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(250).WithMessage("Address must not exceed 250 characters.");

        RuleFor(x => x.Bedrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bedrooms cannot be negative.");

        RuleFor(x => x.Bathrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bathrooms cannot be negative.");

        RuleFor(x => x.AreaSqm)
            .GreaterThan(0).WithMessage("Area in square meters must be greater than zero.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required for optimistic concurrency checks.");

        RuleFor(x => x.Status)
            .Must(s => !s.HasValue || s.Value == PropertyStatus.Available || s.Value == PropertyStatus.Pending)
            .WithMessage("Property status can only be toggled between Available and Pending.");
    }
}
