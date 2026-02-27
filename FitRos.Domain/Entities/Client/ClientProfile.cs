using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.Client
{
    public sealed class ClientProfile
    {
        private readonly List<PhysicalMeasure> _measures = new();

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid CoachId { get; private set; }

        public IReadOnlyCollection<PhysicalMeasure> Measures => _measures.AsReadOnly();

        public DateTime CreatedAt { get; private set; }

        private ClientProfile() { }

        private ClientProfile(Guid id, Guid userId, Guid coachId)
        {
            Id = id;
            UserId = userId;
            CoachId = coachId;
            CreatedAt = DateTime.UtcNow;
        }

        public static ClientProfile Create(Guid userId, Guid coachId)
        {
            if (userId == Guid.Empty)
                throw new DomainException("UserId cannot be empty.");

            if (coachId == Guid.Empty)
                throw new DomainException("CoachId cannot be empty.");

            return new ClientProfile(Guid.NewGuid(), userId, coachId);
        }

        public void AddMeasure(
            decimal weight,
            decimal bodyFatPercentage,
            decimal muscleMass,
            decimal waist,
            decimal chest,
            decimal arms)
        {
            var measure = PhysicalMeasure.Create(
                Id,
                weight,
                bodyFatPercentage,
                muscleMass,
                waist,
                chest,
                arms);

            _measures.Add(measure);
        }
    }
}