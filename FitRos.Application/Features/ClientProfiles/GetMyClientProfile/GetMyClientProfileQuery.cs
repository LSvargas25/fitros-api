using FitRos.Application.Features.ClientProfiles.GetById;
using MediatR;

namespace FitRos.Application.Features.ClientProfiles.GetMyClientProfile;

public sealed record GetMyClientProfileQuery : IRequest<ClientDetailDto>;
