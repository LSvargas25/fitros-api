using System.Linq.Expressions;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Infrastructure.Persistence;

public static class TenantQueryFilterExtensions
{
    public static void ApplyTenantQueryFilters(
        this ModelBuilder modelBuilder,
        Guid? gymId)
    {
        if (!gymId.HasValue)
            return;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "entity");

            var property = Expression.Property(
                parameter,
                nameof(ITenantEntity.GymId));

            var constant = Expression.Constant(gymId, typeof(Guid?));
            var body = Expression.Equal(property, constant);

            var lambda = Expression.Lambda(body, parameter);

            modelBuilder
                .Entity(entityType.ClrType)
                .HasQueryFilter(lambda);
        }
    }
}