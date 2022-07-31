#nullable disable

namespace WebScheduler.DataMigrations.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddPrimaryKeyToOrleansStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("SET SQL_REQUIRE_PRIMARY_KEY = 0;");
        migrationBuilder.AddColumn<long>(
            name: "Id",
            table: "OrleansStorage",
            type: "bigint",
            nullable: false,
            defaultValue: 0L)
            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

        migrationBuilder.AddPrimaryKey(
            name: "PRIMARY",
            table: "OrleansStorage",
            columns: new[] { "Id", "GrainIdHash", "GrainTypeHash" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PRIMARY",
            table: "OrleansStorage");

        migrationBuilder.DropColumn(
            name: "Id",
            table: "OrleansStorage");
    }
}
