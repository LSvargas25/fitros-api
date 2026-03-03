using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Events;

namespace FitRos.Domain.Entities.Client
{
    public sealed class ClientProfile : AggregateRoot
    {
        private readonly List<PhysicalMeasure> _measures = new();

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid CoachId { get; private set; }

        public ClientStatus Status { get; private set; }

        public IReadOnlyCollection<PhysicalMeasure> Measures => _measures.AsReadOnly();

    
        public DateTime? DeactivatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }
 

        private ClientProfile() { }

        private ClientProfile(Guid id, Guid userId, Guid coachId)
        {
            Id = id;
            UserId = userId;
            CoachId = coachId;
            Status = ClientStatus.Active;
        }
        public static ClientProfile Create(Guid userId, Guid coachId)
        {
            if (userId == Guid.Empty)
                throw new DomainException("UserId cannot be empty.");

            if (coachId == Guid.Empty)
                throw new DomainException("CoachId cannot be empty.");
 

            return new ClientProfile(Guid.NewGuid(), userId, coachId);
        }

        public void Deactivate()
        {
            if (Status != ClientStatus.Active)
                throw new DomainException("Only active clients can be deactivated.");

            Status = ClientStatus.Inactive;
            DeactivatedAt = DateTime.UtcNow;
        }

        public void SetModified(Guid userId)
        {
            ModifiedBy = userId;
            ModifiedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (Status != ClientStatus.Inactive)
                throw new DomainException("Only inactive clients can be activated.");

            Status = ClientStatus.Active;
            DeactivatedAt = null;
        }

        public void SoftDelete()
        {
            if (Status == ClientStatus.Deleted)
                throw new DomainException("Client already deleted.");

            Status = ClientStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
        }

        public void AddMeasure(
            decimal weight,
            decimal bodyFatPercentage,
            decimal muscleMass,
            decimal waist,
            decimal chest,
            decimal arms)
        {
            if (Status != ClientStatus.Active)
                throw new DomainException("Cannot add measures to inactive or deleted client.");

            var measure = PhysicalMeasure.Create(
                Id,
                weight,
                bodyFatPercentage,
                muscleMass,
                waist,
                chest,
                arms);

            _measures.Add(measure);

            AddDomainEvent(
                new PhysicalMeasureAddedDomainEvent(Id, measure.Id));
        }
        public void ReassignCoach(Guid newCoachId)
        {
            if (newCoachId == Guid.Empty)
                throw new DomainException("New coach id cannot be empty.");

            if (Status == ClientStatus.Deleted)
                throw new DomainException("Cannot reassign a deleted client.");

            if (CoachId == newCoachId)
                throw new DomainException("Client is already assigned to this coach.");

            CoachId = newCoachId;
        }
    }
}