namespace WebScheduler.Abstractions.Grains;

/// <summary>
/// Model for Tenant state.
/// </summary>
public class TenantState
{
    /// <summary>
    ///  The tenantId.
    /// </summary>
    public Guid TenantId { get; set; }
}
