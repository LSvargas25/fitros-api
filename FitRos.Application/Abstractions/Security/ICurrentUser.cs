using FitRos.Domain.Enums;

namespace FitRos.Application.Abstractions.Security;

public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}