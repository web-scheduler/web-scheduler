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

}
