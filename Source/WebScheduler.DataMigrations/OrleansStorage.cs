namespace WebScheduler.DataMigrations;
using System;

public class OrleansStorage
{
    public long Id { get; set; }
    public int GrainIdHash { get; set; }
    public long GrainIdN0 { get; set; }
    public long GrainIdN1 { get; set; }
    public int GrainTypeHash { get; set; }
    public string GrainTypeString { get; set; } = null!;
    public string? GrainIdExtensionString { get; set; }
    public string ServiceId { get; set; } = null!;
    public byte[]? PayloadBinary { get; set; }
    public string? PayloadXml { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime ModifiedOn { get; set; }
    public int? Version { get; set; }
    public string? TenantId { get; set; }
    public bool? IsScheduledTaskDeleted { get; set; }
    public bool? IsScheduledTaskEnabled { get; set; }
}
