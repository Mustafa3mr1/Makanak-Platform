using Makanak.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Makanak.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
