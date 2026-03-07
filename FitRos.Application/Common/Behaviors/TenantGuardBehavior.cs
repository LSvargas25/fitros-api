using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Common.Behaviors;

public sealed class TenantGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITenantGuardedRequest
{
    private readonly IFitRosDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TenantGuardBehavior(IFitRosDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        if (_currentUser.Role == UserRole.OwnerApp)
            return await next();

        if (!_currentUser.GymId.HasValue)
            throw new ForbiddenException("Tenant context is missing.");

        if (request.ResourceId == Guid.Empty)
            throw new DomainException("ResourceId cannot be empty.");

        var entityType = request.EntityType;

        if (!typeof(ITenantEntity).IsAssignableFrom(entityType))
            return await next();

        var dbContext = (DbContext)_db;

        // Use FindAsync — bypasses query filters entirely, works on both
        // InMemory and real databases, and finds by primary key directly
        var entity = await dbContext.FindAsync(entityType, request.ResourceId);

        if (entity is null)
            throw new NotFoundException("Resource not found.");

        if (entity is not ITenantEntity tenantEntity)
            return await next();

        if (!tenantEntity.GymId.HasValue)
            throw new NotFoundException("Resource not found.");

        if (tenantEntity.GymId.Value != _currentUser.GymId.Value)
            throw new ForbiddenException("Cross-tenant access denied.");

        return await next();
    }
}