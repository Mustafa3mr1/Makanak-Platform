using FluentValidation;
using Makanak.Application.DTOs.Auth;

namespace Makanak.Application.Validators;

public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileRequestDto>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
