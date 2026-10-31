using FluentValidation;
using Makanak.Application.DTOs.Offer;

namespace Makanak.Application.Validators;

public sealed class SubmitOfferRequestValidator : AbstractValidator<SubmitOfferRequest>
{
    public SubmitOfferRequestValidator() => RuleFor(x => x.Price).MoneyRules();
}

public sealed class CounterOfferRequestValidator : AbstractValidator<CounterOfferRequest>
{
    public CounterOfferRequestValidator() => RuleFor(x => x.Price).MoneyRules();
}

public sealed class OfferListQueryValidator : PagedQueryValidator<OfferListQuery>
{
    public OfferListQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
