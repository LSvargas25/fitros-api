using FitRos.Domain.Entities.Client;
using FitRos.Domain.Events;
using Xunit;

namespace FitRos.Tests.Domain;

public class ClientProfileTests
{
    [Fact]
    public void AddMeasure_Should_Add_New_PhysicalMeasure()
    {
        var profile = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());
        profile.AddMeasure(80, 20, 35, 85, 95, 35);
        Assert.Single(profile.Measures);
    }

    [Fact]
    public void Create_Should_Raise_ClientProfileCreatedDomainEvent()
    {
        var profile = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.Single(profile.DomainEvents);
        Assert.IsType<ClientProfileCreatedDomainEvent>(profile.DomainEvents.First());
    }

    [Fact]
    public void AddMeasure_Should_Raise_PhysicalMeasureAddedDomainEvent()
    {
        var profile = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());
        profile.AddMeasure(80, 20, 35, 85, 95, 35);

        Assert.Equal(2, profile.DomainEvents.Count);
        Assert.Contains(profile.DomainEvents, e => e is PhysicalMeasureAddedDomainEvent);
    }
}