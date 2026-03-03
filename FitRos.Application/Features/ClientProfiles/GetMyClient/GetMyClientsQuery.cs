using FitRos.Application.Features.ClientProfiles.GetMyClient;
using MediatR;

namespace FitRos.Application.Features.ClientProfiles.GetMyClients;

public sealed record GetMyClientsQuery : IRequest<IReadOnlyList<MyClientListItemResponse>>;