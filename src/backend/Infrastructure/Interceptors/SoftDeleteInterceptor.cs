using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Interceptors;

/// <summary>
/// Interceptor that implements soft delete functionality by converting hard deletes
/// into updates that set IsDeleted flags and timestamps.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Intercepts the save changes operation to handle soft delete logic.
    /// </summary>
    /// <param name="eventData">Contextual information about the DbContext in use.</param>
    /// <param name="result">The current result indicating whether interception is suppressed.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation. The task result contains
    /// an <see cref="InterceptionResult{Int32}"/> indicating whether interception is suppressed.
    /// </returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        IEnumerable<EntityEntry<ISoftDeletable>> entries = eventData
            .Context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (EntityEntry<ISoftDeletable> softDeletable in entries)
        {
            softDeletable.State = EntityState.Modified;
            softDeletable.Entity.SoftDelete();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
