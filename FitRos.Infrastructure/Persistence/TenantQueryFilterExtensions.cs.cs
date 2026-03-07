using FitRos.Domain.Common;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public static class TenantQueryFilterExtensions
{
    public static void ApplyTenantQueryFilters(
        this ModelBuilder modelBuilder,
        FitRosDbContext context)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(TenantQueryFilterExtensions)
                .GetMethod(nameof(SetTenantFilter),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, new object[] { modelBuilder, context });
        }
    }

    static void SetTenantFilter<TEntity>(
        ModelBuilder modelBuilder,
        FitRosDbContext context)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => e.GymId == context.CurrentGymId);
    }
}