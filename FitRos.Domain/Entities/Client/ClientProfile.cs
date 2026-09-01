using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Events;

namespace FitRos.Domain.Entities.Client
{
    public sealed class ClientProfile : AggregateRoot, ITenantEntity
    {
        private readonly List<PhysicalMeasure> _measures = new();

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid? CoachId { get; private set; }
        public ClientStatus Status { get; private set; }
        public Guid? GymId { get; private set; }

        Guid? ITenantEntity.GymId
        {
            get => GymId;
            set => GymId = value;
        }

        public IReadOnlyCollection<PhysicalMeasure> Measures => _measures.AsReadOnly();

        public DateTime? DeactivatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private ClientProfile() { }

        private ClientProfile(Guid? gymId, Guid id, Guid userId, Guid? coachId)
        {
            GymId = gymId;
            Id = id;
            UserId = userId;
            CoachId = coachId;
            Status = ClientStatus.Active;
        }

        public static ClientProfile Create(Guid gymId, Guid userId, Guid? coachId = null)
        {
            if (userId == Guid.Empty)
                throw new DomainException("UserId cannot be empty.");

            var client = new ClientProfile(gymId, Guid.NewGuid(), userId, coachId);
            client.AddDomainEvent(new ClientProfileCreatedDomainEvent(client.Id, gymId));
            return client;
        }

        public static ClientProfile Create(Guid userId, Guid? coachId = null)
            => Create(Guid.NewGuid(), userId, coachId);

        public static ClientProfile Create()
            => Create(Guid.NewGuid(), Guid.NewGuid(), null);

        /// <summary>
        /// A client who signed up on their own, outside of any gym - no
        /// coach, no gym, they manage their own training.
        /// </summary>
        public static ClientProfile CreateIndependent(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new DomainException("UserId cannot be empty.");

            var client = new ClientProfile(null, Guid.NewGuid(), userId, null);
            client.AddDomainEvent(new ClientProfileCreatedDomainEvent(client.Id, Guid.Empty));
            return client;
        }

        public void Deactivate()
        {
            if (Status != ClientStatus.Active)
                throw new DomainException("Only active clients can be deactivated.");

            Status = ClientStatus.Inactive;
            DeactivatedAt = DateTime.UtcNow;
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

            var measure = PhysicalMeasure.Create(Id, weight, bodyFatPercentage, muscleMass, waist, chest, arms);
            _measures.Add(measure);
            AddDomainEvent(new PhysicalMeasureAddedDomainEvent(Id, measure.Id));
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