using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Foods.UpdateFood;

public sealed class UpdateFoodHandler
    : IRequestHandler<UpdateFoodCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateFoodHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateFoodCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var food = await _context.Foods
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (food is null)
            throw new NotFoundException("Food not found.");

        var normalizedName = command.Name.Trim().ToLowerInvariant();

        var nameTaken = await _context.Foods
            .AnyAsync(
                x => x.Id != command.Id && x.NormalizedName == normalizedName,
                cancellationToken);

        if (nameTaken)
            throw new DomainException("Another food with that name already exists.");

        food.Update(
            command.Name,
            command.Category,
            command.CaloriesPer100g,
            command.ProteinPer100g,
            command.CarbsPer100g,
            command.FatPer100g,
            command.ServingSizeGrams);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
