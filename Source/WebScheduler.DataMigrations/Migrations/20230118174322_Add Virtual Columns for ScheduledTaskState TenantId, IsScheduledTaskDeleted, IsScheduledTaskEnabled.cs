using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebScheduler.DataMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddVirtualColumnsforScheduledTaskStateTenantIdIsScheduledTaskDeletedIsScheduledTaskEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "IsScheduledTaskDeleted",
                table: "OrleansStorage",
                type: "bit",
                nullable: true,
                computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_EXTRACT(PayloadJson, \"$.isDeleted\") IS NOT NULL THEN \r\n        true\r\n    ELSE \r\n        false\r\n    END \r\nEND",
                stored: true);

            migrationBuilder.AddColumn<ulong>(
                name: "IsScheduledTaskEnabled",
                table: "OrleansStorage",
                type: "bit",
                nullable: true,
                computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_EXTRACT(PayloadJson, \"$.task.isEnabled\") IS NOT NULL THEN \r\n        true\r\n    ELSE \r\n        false\r\n    END \r\nEND",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "OrleansStorage",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, \"$.tenantId\")) IS NOT NULL THEN \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, \"$.tenantId\"))\r\n    ELSE \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, \"$.tenantIdString\"))\r\n    END \r\nEND",
                stored: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskDeleted",
                table: "OrleansStorage",
                column: "IsScheduledTaskDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskEnabled",
                table: "OrleansStorage",
                column: "IsScheduledTaskEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_TenantId",
                table: "OrleansStorage",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskDeleted",
                table: "OrleansStorage");

            migrationBuilder.DropIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_IsScheduledTaskEnabled",
                table: "OrleansStorage");

            migrationBuilder.DropIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_TenantId",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "IsScheduledTaskDeleted",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "IsScheduledTaskEnabled",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OrleansStorage");
        }
    }
}
