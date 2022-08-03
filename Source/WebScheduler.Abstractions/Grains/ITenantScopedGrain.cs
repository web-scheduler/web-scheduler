namespace WebScheduler.Abstractions.Grains;

using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;

/// <summary>
/// Used to validate tenant access.
/// </summary>
/// <typeparam name="TGrainInterface"></typeparam>
[Version(1)]
public interface ITenantScopedGrain<TGrainInterface> : IGrainWithStringKey
    where TGrainInterface : IGrain
{
    /// <summary>
    /// Checks if a resource is owned by a tenantId.
    /// </summary>
    /// <param name="tenantId">The tenat id</param>
    /// <returns>True if owned, false if not, null if unset.</returns>
    ValueTask<bool?> IsOwnedByAsync(Guid tenantId);
}
