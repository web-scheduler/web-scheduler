namespace WebScheduler.Abstractions.Grains;

using System;
using System.Threading.Tasks;
using Orleans;

/// <summary>
/// Used to validate tenent access.
/// </summary>
/// <typeparam name="TGrainInterface"></typeparam>
public interface ITenentScopedGrain<TGrainInterface> : IGrainWithStringKey
    where TGrainInterface : IGrain
{
    /// <summary>
    /// Checks if a resource is owned by a tenentId.
    /// </summary>
    /// <param name="tenantId">The tenet id</param>
    /// <returns>True if owned, false if not, null if unset.</returns>
    ValueTask<bool?> IsOwnedByAsync(Guid tenantId);

    /// <summary>
    /// Sets ownership of a resource.
    /// </summary>
    /// <param name="tenantId">The owner tenent id to set</param>
    /// <returns>A valueTask.</returns>
    ValueTask SetOwnedByAsync(Guid tenantId);
}
