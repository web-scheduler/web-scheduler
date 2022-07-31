namespace WebScheduler.DataMigrations;
using System;

public class OrleansRemindersTable
{
    public string ServiceId { get; set; } = null!;
    public string GrainId { get; set; } = null!;
    public string ReminderName { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public long Period { get; set; }
    public int GrainHash { get; set; }
    public int Version { get; set; }
}
