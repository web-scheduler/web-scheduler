using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebScheduler.DataMigrations.Migrations
{
    public partial class AddIX_OrleansStoragetoOrleansStoragetable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateIndex(
                name: "IX_OrleansStorage",
                table: "OrleansStorage",
                columns: new[] { "GrainIdHash", "GrainTypeHash" });

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropIndex(
                name: "IX_OrleansStorage",
                table: "OrleansStorage");
    }
}
