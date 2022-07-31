namespace WebScheduler.DataMigrations;
using System;

public class OrleansMembershipTable
{
    public string DeploymentId { get; set; } = null!;
    public string Address { get; set; } = null!;
    public int Port { get; set; }
    public int Generation { get; set; }
    public string SiloName { get; set; } = null!;
    public string HostName { get; set; } = null!;
    public int Status { get; set; }
    public int? ProxyPort { get; set; }
    public string? SuspectTimes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime IamAliveTime { get; set; }

    public virtual OrleansMembershipVersionTable Deployment { get; set; } = null!;
}
