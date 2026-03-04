using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Gyms.CreateGym;

public sealed class CreateGymHandler
{
    private readonly IFitRosDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateGymHandler(
        IFitRosDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        CreateGymCommand command,
        CancellationToken cancellationToken)
    {
        var gym = Gym.Create(
            command.Name,
            command.Address,
            command.PhoneNumber);

        _context.Gyms.Add(gym);

        User admin;

        if (command.ExistingAdminUserId.HasValue)
        {
            admin = await _context.Users
                .FirstAsync(
                    x => x.Id == command.ExistingAdminUserId,
                    cancellationToken);

            admin.ChangeRole(UserRole.Admin);
            admin.AssignToGym(gym.Id);
        }
        else
        {
            var passwordHash = _passwordHasher.Hash(command.AdminPassword!);

            admin = User.CreateForGym(
                gym.Id,
                command.AdminEmail!,
                command.AdminFirstName!,
                command.AdminLastName!,
                passwordHash,
                UserRole.Admin);

            _context.Users.Add(admin);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return gym.Id;
    }
}