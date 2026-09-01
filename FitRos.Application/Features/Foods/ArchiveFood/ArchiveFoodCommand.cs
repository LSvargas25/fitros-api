using MediatR;

namespace FitRos.Application.Features.Foods.ArchiveFood;

public record ArchiveFoodCommand(Guid Id) : IRequest;
