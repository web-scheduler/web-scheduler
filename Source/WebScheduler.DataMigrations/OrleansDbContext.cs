namespace WebScheduler.DataMigrations;
using Microsoft.EntityFrameworkCore;

public class OrleansDbContext : DbContext
{
    public OrleansDbContext(DbContextOptions<OrleansDbContext> options)
      : base(options)
    {
    }

    public virtual DbSet<OrleansMembershipTable> OrleansMembershipTables { get; set; } = null!;
    public virtual DbSet<OrleansMembershipVersionTable> OrleansMembershipVersionTables { get; set; } = null!;
    public virtual DbSet<OrleansQuery> OrleansQueries { get; set; } = null!;
    public virtual DbSet<OrleansRemindersTable> OrleansRemindersTables { get; set; } = null!;
    public virtual DbSet<OrleansStorage> OrleansStorages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<OrleansMembershipTable>(entity =>
        {
            entity.HasKey(e => new { e.DeploymentId, e.Address, e.Port, e.Generation })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0, 0 });

            entity.ToTable("OrleansMembershipTable");

            entity.Property(e => e.DeploymentId)
                .HasMaxLength(150)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            entity.Property(e => e.Address).HasMaxLength(45);

            entity.Property(e => e.HostName)
                .HasMaxLength(150)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            entity.Property(e => e.IamAliveTime)
                .HasColumnType("datetime")
                .HasColumnName("IAmAliveTime");

            entity.Property(e => e.SiloName)
                .HasMaxLength(150)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.Property(e => e.SuspectTimes).HasMaxLength(8000);

            entity.HasOne(d => d.Deployment)
                .WithMany(p => p.OrleansMembershipTables)
                .HasForeignKey(d => d.DeploymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MembershipTable_MembershipVersionTable_DeploymentId");
        })
            .Entity<OrleansMembershipVersionTable>(entity =>
        {
            entity.HasKey(e => e.DeploymentId)
                .HasName("PRIMARY");

            entity.ToTable("OrleansMembershipVersionTable");

            entity.Property(e => e.DeploymentId)
                .HasMaxLength(150)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamp")
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        })
            .Entity<OrleansQuery>(entity =>
        {
            entity.HasKey(e => e.QueryKey)
                .HasName("PRIMARY");

            entity.ToTable("OrleansQuery");

            entity.Property(e => e.QueryKey).HasMaxLength(64);

            entity.Property(e => e.QueryText).HasMaxLength(8000);
        })
            .Entity<OrleansRemindersTable>(entity =>
        {
            entity.HasKey(e => new { e.ServiceId, e.GrainId, e.ReminderName })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("OrleansRemindersTable");

            entity.Property(e => e.ServiceId)
                .HasMaxLength(150)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            entity.Property(e => e.GrainId).HasMaxLength(150);

            entity.Property(e => e.ReminderName)
                .HasMaxLength(150)
                .UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            entity.Property(e => e.StartTime).HasColumnType("datetime");
        })
            .Entity<OrleansStorage>(entity =>
            {
                entity.Property(e => e.Id)
                .UseMySqlIdentityColumn();

                entity.HasKey(e => new { e.Id, e.GrainIdHash, e.GrainTypeHash })
                .HasName("PRIMARY");

                entity.HasIndex(e => new { e.GrainIdHash, e.GrainTypeHash })
                .HasDatabaseName("IX_OrleansStorage");

                entity.ToTable("OrleansStorage");

                entity.Property(e => e.GrainIdExtensionString)
                    .HasMaxLength(512)
                    .UseCollation("utf8_general_ci")
                    .HasCharSet("utf8");

                entity.Property(e => e.GrainTypeString)
                    .HasMaxLength(512)
                    .UseCollation("utf8_general_ci")
                    .HasCharSet("utf8");

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.PayloadBinary).HasColumnType("blob");

                entity.Property(e => e.PayloadJson).HasColumnType("json");

                entity.Property(e => e.ServiceId)
                    .HasMaxLength(150)
                    .UseCollation("utf8_general_ci")
                    .HasCharSet("utf8");

                entity.Property(e => e.TenantId)
                    .HasMaxLength(255)
                    .UseCollation("utf8_general_ci")
                    .HasCharSet("utf8")
                    // TenantId is only valid for the ScheduledTaskState, all queries against other grain states are rooted at the WebScheduler.Grains.Scheduler.ScheduledTaskGrain,WebScheduler.Grains.ScheduledTaskState.
                    .HasComputedColumnSql("""
                    CASE WHEN GrainTypeHash = 2108290596 THEN
                        CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, "$.tenantId")) IS NOT NULL THEN 
                            JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, "$.tenantId"))
                        ELSE 
                            JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, "$.tenantIdString"))
                        END 
                    END
                    """, stored: true);

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_OrleansStorage_ScheduledTaskState_TenantId");

                entity.Property(e => e.IsScheduledTaskDeleted)
                    .HasColumnType("bit")
                    // IsDeleted is only valid for the ScheduledTaskState, all queries against other grain states are rooted at the WebScheduler.Grains.Scheduler.ScheduledTaskGrain,WebScheduler.Grains.ScheduledTaskState
                    // The json serializer optimizes the output by not emitting default values for properties. boolean default is false, so is null in DB (no json for isDeleted).
                    .HasComputedColumnSql("""
                      CASE WHEN GrainTypeHash = 2108290596 THEN
                          CASE WHEN JSON_EXTRACT(PayloadJson, "$.isDeleted") IS NOT NULL THEN 
                              true
                          ELSE 
                              false
                          END 
                      END
                      """, stored: true);

                entity.HasIndex(e => e.IsScheduledTaskDeleted)
                    .HasDatabaseName("IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskDeleted");

                entity.Property(e => e.IsScheduledTaskEnabled)
                    .HasColumnType("bit")
                    // IsDeleted is only valid for the ScheduledTaskState, all queries against other grain states are rooted at the WebScheduler.Grains.Scheduler.ScheduledTaskGrain,WebScheduler.Grains.ScheduledTaskState
                    // The json serializer optimizes the output by not emitting default values for properties. boolean default is false, so is null in DB (no json for isEnabled).
                    .HasComputedColumnSql("""
                      CASE WHEN GrainTypeHash = 2108290596 THEN
                          CASE WHEN JSON_EXTRACT(PayloadJson, "$.task.isEnabled") IS NOT NULL THEN 
                              true
                          ELSE 
                              false
                          END 
                      END
                      """, stored: true);

                entity.HasIndex(e => e.IsScheduledTaskEnabled)
                    .HasDatabaseName("IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskEnabled");
            });

        base.OnModelCreating(modelBuilder);
    }
}
