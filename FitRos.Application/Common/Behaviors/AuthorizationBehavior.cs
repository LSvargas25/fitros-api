using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Common.Behaviors
{
    public sealed class AuthorizationBehavior<TRequest, TResponse>
     : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ICurrentUser _currentUser;

        public AuthorizationBehavior(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not IAuthorizeRequest authorizeRequest)
                return await next();

            if (!_currentUser.IsAuthenticated)
                throw new UnauthorizedException("User not authenticated.");

            if (!authorizeRequest.AllowedRoles.Contains(_currentUser.Role))
                throw new ForbiddenException("You are not authorized.");

            return await next();
        }
    }
}
