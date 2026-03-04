using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;

namespace FitRos.Infrastructure.Security;

public class DevelopmentCurrentUser : ICurrentUser
{
    public Guid? UserId =>
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public UserRole Role => UserRole.Admin;

    public Guid? GymId =>
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    public bool IsAuthenticated => true;
}