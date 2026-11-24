using FluentValidation;
using Makanak.Application.DTOs.Auth;
using Makanak.Domain.Enums;

namespace Makanak.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.UserType)
            .IsInEnum().WithMessage("Invalid user type.")
            .Must(type => type == UserType.Owner || type == UserType.Seeker)
            .WithMessage("User type must be either Owner or Seeker. Admin registration is prohibited.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
