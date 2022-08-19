using Microsoft.Extensions.Logging;
/// <summary>
/// <see cref="ILogger"/> extension methods. Helps log messages using strongly typing and source generators.
/// </summary>
internal static partial class TenantValidationInterceptorLoggerExtensions
{
    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Warning,
        Message = "Tenant {tenantId} is not authorized to access {scheduledTaskId}")]
    public static partial void TenantUnauthorized(this ILogger logger, string tenantId, string scheduledTaskId);

    [LoggerMessage(
      EventId = 7001,
      Level = LogLevel.Error,
      Message = "Error with Tenant {tenantId} attempted access to {scheduledTaskId}")]
    public static partial void TenantScopedGrainFilter(this ILogger logger, Exception ex, string tenantId, string scheduledTaskId);
}
