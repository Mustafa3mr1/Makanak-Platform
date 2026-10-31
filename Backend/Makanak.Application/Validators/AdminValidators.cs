using FluentValidation;
using Makanak.Application.DTOs.Admin;

namespace Makanak.Application.Validators;

public sealed class AdminUserQueryValidator : PagedQueryValidator<AdminUserQuery>
{
    public AdminUserQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.UserType).IsInEnum();
    }
}

public sealed class AdminPropertyQueryValidator : PagedQueryValidator<AdminPropertyQuery>
{
    public AdminPropertyQueryValidator()
    {
        RuleFor(x => x.Keyword).MaximumLength(100);
        RuleFor(x => x.Status).IsInEnum();
    }
}
