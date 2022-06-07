namespace WebScheduler.Abstractions.Constants;

using Orleans.Runtime;

/// <summary>
/// Keys for <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextKeys
{
    /// <summary>
    /// The constant for TenantId
    /// </summary>
    public const string TenantId = nameof(TenantId);
}
