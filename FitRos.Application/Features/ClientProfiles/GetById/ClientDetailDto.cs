using FitRos.Domain.Entities.Enums;
using System;
using System.Collections.Generic;

namespace FitRos.Application.Features.ClientProfiles.GetById
{
    public sealed class ClientDetailDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public Guid CoachId { get; init; }
        public ClientStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }

        public IReadOnlyCollection<PhysicalMeasureDto> Measures { get; init; }
            = new List<PhysicalMeasureDto>();
    }

    public sealed class PhysicalMeasureDto
    {
        public Guid Id { get; init; }
        public decimal Weight { get; init; }
        public decimal BodyFatPercentage { get; init; }
        public decimal MuscleMass { get; init; }
        public decimal Waist { get; init; }
        public decimal Chest { get; init; }
        public decimal Arms { get; init; }
        public DateTime RecordedAt { get; init; }
    }
}