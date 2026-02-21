using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;

namespace FitRos.Infrastructure.Security;

public class DevelopmentCurrentUser : ICurrentUser
{
    public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");

    // temporal to test using admin when development, should be replaced with real implementation that gets the role from the database or token
    public UserRole Role => UserRole.Admin;

    public bool IsAuthenticated => true;
}