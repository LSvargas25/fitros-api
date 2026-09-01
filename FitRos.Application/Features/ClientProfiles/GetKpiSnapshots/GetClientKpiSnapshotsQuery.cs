using MediatR;

namespace FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;

public sealed record GetClientKpiSnapshotsQuery(Guid ClientProfileId) : IRequest<List<ClientKpiSnapshotDto>>;
