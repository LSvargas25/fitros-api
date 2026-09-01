using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Nutrition;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Foods.CreateFood;

public sealed class CreateFoodHandler
    : IRequestHandler<CreateFoodCommand, CreateFoodResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateFoodHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CreateFoodResponse> Handle(
        CreateFoodCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var normalizedName = command.Name.Trim().ToLowerInvariant();

        var exists = await _context.Foods
            .AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken);

        if (exists)
            throw new DomainException("Food already exists.");

        var food = Food.Create(
            command.Name,
            command.Category,
            command.CaloriesPer100g,
            command.ProteinPer100g,
            command.CarbsPer100g,
            command.FatPer100g,
            command.ServingSizeGrams,
            _currentUser.GymId);

        _context.Foods.Add(food);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateFoodResponse(food.Id);
    }
}
