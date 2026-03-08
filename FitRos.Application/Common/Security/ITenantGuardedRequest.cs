namespace FitRos.Application.Common.Security;

public interface ITenantGuardedRequest
{
    Guid ResourceId { get; }
    Type EntityType { get; }
}