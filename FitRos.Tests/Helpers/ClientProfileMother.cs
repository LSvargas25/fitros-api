using FitRos.Domain.Entities.Client;
using System;

namespace FitRos.Tests.Helpers;

public static class ClientProfileMother
{
    public static ClientProfile Create(
        Guid? gymId = null,
        Guid? userId = null,
        Guid? coachId = null)
    {
        return ClientProfile.Create(
            gymId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            coachId ?? Guid.NewGuid());
    }

    public static ClientProfile ForCoach(Guid coachId, Guid? gymId = null, Guid? userId = null)
    {
        return ClientProfile.Create(
            gymId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            coachId);
    }

    public static ClientProfile ForOtherCoach(Guid otherCoachId, Guid? gymId = null, Guid? userId = null)
    {
        return ClientProfile.Create(
            gymId ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            otherCoachId);
    }
}