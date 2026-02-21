using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;

namespace FitRos.Tests.TestDoubles;

public class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public UserRole Role { get; set; }
    public bool IsAuthenticated { get; set; } = true;
}