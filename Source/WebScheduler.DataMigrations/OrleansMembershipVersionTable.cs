namespace WebScheduler.DataMigrations;
using System;
using System.Collections.Generic;

public class OrleansMembershipVersionTable
{
    public OrleansMembershipVersionTable() => this.OrleansMembershipTables = new HashSet<OrleansMembershipTable>();

    public string DeploymentId { get; set; } = null!;
    public DateTime? Timestamp { get; set; }
    public int Version { get; set; }

    public virtual ICollection<OrleansMembershipTable> OrleansMembershipTables { get; set; }
}
