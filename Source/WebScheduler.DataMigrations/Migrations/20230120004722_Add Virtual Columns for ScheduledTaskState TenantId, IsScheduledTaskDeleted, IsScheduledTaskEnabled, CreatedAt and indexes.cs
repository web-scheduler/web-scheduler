using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebScheduler.DataMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddVirtualColumnsforScheduledTaskStateTenantIdIsScheduledTaskDeletedIsScheduledTaskEnabledCreatedAtandindexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScheduledTaskDeleted",
                table: "OrleansStorage",
                type: "tinyint(1)",
                nullable: true,
                computedColumnSql: "case when (`GrainTypeHash` = 2108290596) then (case when (json_extract(`PayloadJson`,'$.isDeleted') is not null) then TRUE else FALSE end) end",
                stored: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsScheduledTaskEnabled",
                table: "OrleansStorage",
                type: "tinyint(1)",
                nullable: true,
                computedColumnSql: "case when (`GrainTypeHash` = 2108290596) then (case when (json_extract(`PayloadJson`,'$.task.isEnabled') is not null) then TRUE else FALSE end) end",
                stored: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledTaskCreatedAt",
                table: "OrleansStorage",
                type: "DATETIME(6)",
                nullable: true,
                computedColumnSql: "case when ((`GrainTypeHash` = 2108290596) and (`IsScheduledTaskDeleted` = FALSE)) then (case length(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt'))) when 28 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),26),'%Y-%m-%dT%H:%i:%s.%f+0000') when 27 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),26),'%Y-%m-%dT%H:%i:%s.%f+0000') when 26 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),25),'%Y-%m-%dT%H:%i:%s.%f+0000') when 25 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),24),'%Y-%m-%dT%H:%i:%s.%f+0000') when 21 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),20),'%Y-%m-%dT%H:%i:%s.%f+0000') when 22 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),21),'%Y-%m-%dT%H:%i:%s.%f+0000') when 23 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),22),'%Y-%m-%dT%H:%i:%s.%f+0000') when 24 then str_to_date(left(json_unquote(json_extract(`PayloadJson`,'$.task.createdAt')),23),'%Y-%m-%dT%H:%i:%s.%f+0000') end) end",
                stored: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "OrleansStorage",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                computedColumnSql: "case when (`GrainTypeHash` = 2108290596) then (case when (json_unquote(json_extract(`PayloadJson`,'$.tenantId')) is not null) then json_unquote(json_extract(`PayloadJson`,'$.tenantId')) else json_unquote(json_extract(`PayloadJson`,'$.tenantIdString')) end) end",
                stored: false,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_TenantId_IsDeletedEnabled",
                table: "OrleansStorage",
                columns: new[] { "TenantId", "IsScheduledTaskDeleted", "IsScheduledTaskEnabled"});
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_TenantId_IsDeletedEnabled",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "IsScheduledTaskDeleted",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "IsScheduledTaskEnabled",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "ScheduledTaskCreatedAt",
                table: "OrleansStorage");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OrleansStorage");
        }
    }
}
