using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FitRos.Application.Common.Behaviors;

public sealed class TenantGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITenantGuardedRequest
{
    private readonly DbContext _db;
    private readonly ICurrentUser _currentUser;

    public TenantGuardBehavior(DbContext db, ICurrentUser currentUser)
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

        var method = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType);

        var set = (IQueryable)method.Invoke(_db, null)!;

        var gymId = await set
            .Cast<object>()
            .Where(e => EF.Property<Guid>(e, "Id") == request.ResourceId)
            .Select(e => EF.Property<Guid?>(e, nameof(ITenantEntity.GymId)))
            .FirstOrDefaultAsync(cancellationToken);

        if (!gymId.HasValue)
            throw new KeyNotFoundException("Resource not found.");

        if (gymId.Value != _currentUser.GymId.Value)
            throw new ForbiddenException("Cross-tenant access denied.");

        return await next();
    }
}