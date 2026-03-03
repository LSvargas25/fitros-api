using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;

namespace FitRos.Application.Common.Behaviors;

public sealed class AuthenticationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ICurrentUser _currentUser;

    public AuthenticationBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IAllowAnonymousRequest)
            return await next();

        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        return await next();
    }
}