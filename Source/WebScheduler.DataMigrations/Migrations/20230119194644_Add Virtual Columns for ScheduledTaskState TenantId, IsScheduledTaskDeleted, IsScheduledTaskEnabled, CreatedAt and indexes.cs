#nullable disable

namespace WebScheduler.DataMigrations.Migrations;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
            computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_EXTRACT(PayloadJson, '$.isDeleted') IS NOT NULL THEN \r\n        true\r\n    ELSE \r\n        false\r\n    END \r\nEND",
            stored: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsScheduledTaskEnabled",
            table: "OrleansStorage",
            type: "tinyint(1)",
            nullable: true,
            computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_EXTRACT(PayloadJson, '$.task.isEnabled') IS NOT NULL THEN \r\n        true\r\n    ELSE \r\n        false\r\n    END \r\nEND",
            stored: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "ScheduledTaskCreatedAt",
            table: "OrleansStorage",
            type: "DATETIME(6)",
            nullable: true,
            computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 AND IsScheduledTaskDeleted = false THEN\r\n        STR_TO_DATE(REPLACE(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')), 'Z','+0000'), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\nEND",
            stored: false);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            table: "OrleansStorage",
            type: "varchar(255)",
            maxLength: 255,
            nullable: true,
            computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantId')) IS NOT NULL THEN \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantId'))\r\n    ELSE \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantIdString'))\r\n    END \r\nEND",
            collation: "utf8_general_ci",
            stored: false)
            .Annotation("MySql:CharSet", "utf8");

        migrationBuilder.CreateIndex(
            name: "IX_OrleansStorage_ScheduledTaskState_TenantId_IsDeletedEnabled",
            table: "OrleansStorage",
            columns: new[] { "TenantId", "IsScheduledTaskDeleted", "IsScheduledTaskEnabled" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OrleansStorage_ScheduledTaskState_TenantId_IsDeletedEnabled",
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

        // This goes last because of dependency from ScheduledTaskCreatedAt
        migrationBuilder.DropColumn(
          name: "IsScheduledTaskDeleted",
          table: "OrleansStorage");
    }
}
