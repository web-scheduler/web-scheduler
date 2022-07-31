#nullable disable

namespace WebScheduler.DataMigrations.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;

public partial class Remove_IX_OrleansStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.DropIndex(
            name: "IX_OrleansStorage",
            table: "OrleansStorage");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.CreateIndex(
            name: "IX_OrleansStorage",
            table: "OrleansStorage",
            columns: new[] { "GrainIdHash", "GrainTypeHash" });
}
