using MediatR;

namespace FitRos.Application.Features.Exercises.ArchiveExercise;

public record ArchiveExerciseCommand(Guid Id) : IRequest;