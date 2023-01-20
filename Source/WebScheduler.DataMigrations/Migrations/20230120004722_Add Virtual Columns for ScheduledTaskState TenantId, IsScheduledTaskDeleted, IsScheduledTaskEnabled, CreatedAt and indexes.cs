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
                computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 AND IsScheduledTaskDeleted = false THEN\r\n      CASE LENGTH(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')))\r\n          WHEN 28 -- 7 microsecond precision  + Z, intentionally 26\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),26), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 27\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),26), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 26\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),25), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 25\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),24), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 21\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),20), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 22\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),21), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 23\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),22), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n          WHEN 24\r\n              THEN STR_TO_DATE(LEFT(JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.task.createdAt')),23), '%Y-%m-%dT%H:%i:%s.%f+0000')\r\n      END\r\n  END",
                stored: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "OrleansStorage",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                computedColumnSql: "CASE WHEN GrainTypeHash = 2108290596 THEN\r\n    CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantId')) IS NOT NULL THEN \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantId'))\r\n    ELSE \r\n        JSON_UNQUOTE(JSON_EXTRACT(PayloadJson, '$.tenantIdString'))\r\n    END \r\nEND",
                stored: false,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_OrleansStorage_ScheduledTaskState_TenantId_IsDeletedEnabled",
                table: "OrleansStorage",
                columns: new[] { "TenantId", "IsScheduledTaskDeleted", "IsScheduledTaskEnabled", "ScheduledTaskCreatedAt" });
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
