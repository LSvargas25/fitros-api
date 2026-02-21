using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FitRos.Infrastructure.Security;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(sub))
                return Guid.Empty;

            return Guid.Parse(sub);
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = User?.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(roleClaim))
                return default;

            return Enum.Parse<UserRole>(roleClaim);
        }
    }

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    Guid? ICurrentUser.UserId => UserId;
}