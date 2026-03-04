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

    private string? GetClaim(string type)
    {
        return User?.FindFirstValue(type);
    }

    public Guid? UserId
    {
        get
        {
            var sub = GetClaim(JwtRegisteredClaimNames.Sub)
                      ?? GetClaim(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(sub))
                return null;

            return Guid.Parse(sub);
        }
    }

    public Guid? GymId
    {
        get
        {
            var gymClaim = GetClaim("gymId");

            if (string.IsNullOrWhiteSpace(gymClaim))
                return null;

            return Guid.Parse(gymClaim);
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = GetClaim(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(roleClaim))
                return default;

            return Enum.Parse<UserRole>(roleClaim);
        }
    }

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}