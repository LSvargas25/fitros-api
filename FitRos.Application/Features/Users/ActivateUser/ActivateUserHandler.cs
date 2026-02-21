using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Users.ActivateUser
{
    public sealed class ActivateUserHandler
    {
        private readonly IFitRosDbContext _context;

        public ActivateUserHandler(IFitRosDbContext context)
        {
            _context = context;
        }

        public async Task Handle(ActivateUserCommand command, CancellationToken ct)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == command.UserId, ct);

            if (user is null)
                throw new NotFoundException("User not found.");

            user.Activate();

            await _context.SaveChangesAsync(ct);
        }

    }
}