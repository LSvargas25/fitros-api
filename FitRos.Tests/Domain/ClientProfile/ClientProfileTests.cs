using FitRos.Domain.Entities.Client;
using Xunit;

namespace FitRos.Tests.Domain
{
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
        public void AddMeasure_Should_Raise_DomainEvent()
        {
            var profile = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());

            profile.AddMeasure(80, 20, 35, 85, 95, 35);

            Assert.Single(profile.DomainEvents);
        }
    }
}