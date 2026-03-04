using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;

namespace FitRos.Tests.TestDoubles;

public class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid gymId)
    {
        GymId = gymId;
    }

    public Guid? UserId { get; set; }

    public UserRole Role { get; set; }

    public Guid? GymId { get; set; }

    public bool IsAuthenticated { get; set; } = true;
}