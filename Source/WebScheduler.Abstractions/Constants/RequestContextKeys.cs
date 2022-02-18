namespace WebScheduler.Abstractions.Constants;

using Orleans.Runtime;

/// <summary>
/// Keys for <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextKeys
{
    /// <summary>
    /// The constant for TenentId
    /// </summary>
    public const string TenentId = nameof(TenentId);
}
