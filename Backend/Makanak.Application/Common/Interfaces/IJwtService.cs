using Makanak.Application.Common.Models;

namespace Makanak.Application.Common.Interfaces;

public interface IJwtService
{
    JwtTokenResult CreateToken(UserAccountInfo user);
}
