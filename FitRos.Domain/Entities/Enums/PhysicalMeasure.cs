using FitRos.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Domain.Entities.Enums
{
    public sealed class PhysicalMeasure
    {
        public Guid Id { get; private set; }
        public Guid ClientProfileId { get; private set; }

        public decimal Weight { get; private set; }
        public decimal BodyFatPercentage { get; private set; }
        public decimal MuscleMass { get; private set; }

        public decimal Waist { get; private set; }
        public decimal Chest { get; private set; }
        public decimal Arms { get; private set; }

        public DateTime RecordedAt { get; private set; }

        private PhysicalMeasure() { }

        private PhysicalMeasure(
            Guid id,
            Guid clientProfileId,
            decimal weight,
            decimal bodyFatPercentage,
            decimal muscleMass,
            decimal waist,
            decimal chest,
            decimal arms)
        {
            Id = id;
            ClientProfileId = clientProfileId;

            Weight = weight;
            BodyFatPercentage = bodyFatPercentage;
            MuscleMass = muscleMass;

            Waist = waist;
            Chest = chest;
            Arms = arms;

            RecordedAt = DateTime.UtcNow;
        }

        public static PhysicalMeasure Create(
            Guid clientProfileId,
            decimal weight,
            decimal bodyFatPercentage,
            decimal muscleMass,
            decimal waist,
            decimal chest,
            decimal arms)
        {
            if (weight <= 0)
                throw new DomainException("Weight must be greater than zero.");

            return new PhysicalMeasure(
                Guid.NewGuid(),
                clientProfileId,
                weight,
                bodyFatPercentage,
                muscleMass,
                waist,
                chest,
                arms);
        }
    }
}
