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
            decimal arms,
            DateTime recordedAt)
        {
            Id = id;
            ClientProfileId = clientProfileId;

            Weight = weight;
            BodyFatPercentage = bodyFatPercentage;
            MuscleMass = muscleMass;

            Waist = waist;
            Chest = chest;
            Arms = arms;

            RecordedAt = recordedAt;
        }

        /// <summary>
        /// <paramref name="recordedAt"/> lets a caller back-date a forgotten
        /// entry; omit it for "now". It may not be in the future (finer window
        /// limits live in the command validator).
        /// </summary>
        public static PhysicalMeasure Create(
            Guid clientProfileId,
            decimal weight,
            decimal bodyFatPercentage,
            decimal muscleMass,
            decimal waist,
            decimal chest,
            decimal arms,
            DateTime? recordedAt = null)
        {
            if (weight <= 0)
                throw new DomainException("Weight must be greater than zero.");

            var when = recordedAt ?? DateTime.UtcNow;

            if (when > DateTime.UtcNow.AddMinutes(1))
                throw new DomainException("A measurement cannot be recorded in the future.");

            return new PhysicalMeasure(
                Guid.NewGuid(),
                clientProfileId,
                weight,
                bodyFatPercentage,
                muscleMass,
                waist,
                chest,
                arms,
                when);
        }
    }
}
