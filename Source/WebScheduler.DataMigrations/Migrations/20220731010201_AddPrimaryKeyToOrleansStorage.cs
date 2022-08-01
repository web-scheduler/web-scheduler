#nullable disable

namespace WebScheduler.DataMigrations.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddPrimaryKeyToOrleansStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("SET SQL_REQUIRE_PRIMARY_KEY = 0;");

        migrationBuilder.DropIndex(
            name: "IX_OrleansStorage",
            table: "OrleansStorage");

        migrationBuilder.Sql("ALTER TABLE `OrleansStorage` ADD `Id` bigint NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `OrleansStorage` ADD PRIMARY KEY(Id, GrainIdHash, GrainTypeHash);");
        migrationBuilder.Sql("ALTER TABLE `OrleansStorage` MODIFY  `Id` bigint NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("SET SQL_REQUIRE_PRIMARY_KEY = 1;");
        migrationBuilder.Sql("DROP procedure POMELO_AFTER_ADD_PRIMARY_KEY;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PRIMARY",
            table: "OrleansStorage");

        migrationBuilder.DropColumn(
            name: "Id",
            table: "OrleansStorage");

        migrationBuilder.CreateIndex(
            name: "IX_OrleansStorage",
            table: "OrleansStorage",
            columns: new[] { "GrainIdHash", "GrainTypeHash" });
    }
}
