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
                        CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantId')) IS NOT NULL THEN 
                            JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantId'))
                        ELSE 
                            JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantIdString'))
                        END 
                    END
                    """, stored: false);

                entity.Property(e => e.IsScheduledTaskDeleted)
                    .HasColumnType("tinyint(1)")
                    // IsDeleted is only valid for the ScheduledTaskState, all queries against other grain states are rooted at the WebScheduler.Grains.Scheduler.ScheduledTaskGrain,WebScheduler.Grains.ScheduledTaskState
                    // The json serializer optimizes the output by not emitting default values for properties. boolean default is false, so is null in DB (no json for isDeleted).
                    .HasComputedColumnSql("""
                      CASE WHEN GrainTypeHash = 2108290596 THEN
                          CASE WHEN JSON_EXTRACT(PayloadJson, '$.isDeleted') IS NOT NULL THEN 
                              true
                          ELSE 
                              false
                          END 
                      END
                      """, stored: false);

                entity.Property(e => e.IsScheduledTaskEnabled)
                    .HasColumnType("tinyint(1)")
                    // IsDeleted is only valid for the ScheduledTaskState, all queries against other grain states are rooted at the WebScheduler.Grains.Scheduler.ScheduledTaskGrain,WebScheduler.Grains.ScheduledTaskState
                    // The json serializer optimizes the output by not emitting default values for properties. boolean default is false, so is null in DB (no json for isEnabled).
                    .HasComputedColumnSql("""
                      CASE WHEN GrainTypeHash = 2108290596 THEN
                          CASE WHEN JSON_EXTRACT(PayloadJson, '$.task.isEnabled') IS NOT NULL THEN 
                              true
                          ELSE 
                              false
                          END 
                      END
                      """, stored: false);

                entity.Property(e => e.ScheduledTaskCreatedAt)
                    .HasColumnType("DATETIME(6)")
                    // ScheduledTaskCreatedAt is only valid for the ScheduledTaskState, all queries against other grain states are rooted at the WebScheduler.Grains.Scheduler.ScheduledTaskGrain,WebScheduler.Grains.ScheduledTaskState
                    // We have max 7 microsecond precision in the json; mysql has a max of 6. the json is not padded so we must trim. We also remove the trailing Z, '2022-09-28T07:01:46.5644758'
                    .HasComputedColumnSql("""
                      CASE WHEN GrainTypeHash = 2108290596 AND IsScheduledTaskDeleted = false THEN
                            CASE LENGTH(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')))
                                WHEN 28 -- 7 microsecond precision  + Z, intentionally 26
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),26), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 27
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),26), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 26
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),25), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 25
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),24), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 21
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),20), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 22
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),21), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 23
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),22), '%Y-%m-%dT%H:%i:%s.%f+0000')
                                WHEN 24
                                    THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),23), '%Y-%m-%dT%H:%i:%s.%f+0000')
                            END
                        END
                      """, stored: false);

                entity.HasIndex(e => new { e.TenantId, e.IsScheduledTaskDeleted, e.IsScheduledTaskEnabled, e.ScheduledTaskCreatedAt })
                    .HasDatabaseName("IX_OrleansStorage_ScheduledTaskState_TenantId_IsDeletedEnabled");
            });

        base.OnModelCreating(modelBuilder);
    }
}
