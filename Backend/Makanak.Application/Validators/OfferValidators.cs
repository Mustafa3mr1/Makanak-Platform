using FluentValidation;
using Makanak.Application.DTOs.Offers;

namespace Makanak.Application.Validators;

public class CreateOfferRequestValidator : AbstractValidator<CreateOfferRequestDto>
{
    public CreateOfferRequestValidator()
    {
        RuleFor(x => x.PropertyPublicId)
            .NotEmpty().WithMessage("Property public identifier is required.");

        RuleFor(x => x.InitialPrice)
            .GreaterThan(0).WithMessage("Initial offer price must be greater than zero.");
    }
}

public class CounterOfferRequestValidator : AbstractValidator<CounterOfferRequestDto>
{
    public CounterOfferRequestValidator()
    {
        RuleFor(x => x.CounterPrice)
            .GreaterThan(0).WithMessage("Counter offer price must be greater than zero.");
    }
}
