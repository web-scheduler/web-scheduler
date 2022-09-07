using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebScheduler.DataMigrations.Migrations
{
    public partial class AddIX_OrleansStoragetoOrleansStoragetable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("/*!80013 SET SQL_REQUIRE_PRIMARY_KEY = 0 */;");

            migrationBuilder.Sql("ALTER TABLE `OrleansStorage` DROP PRIMARY KEY, ADD PRIMARY KEY(Id);");

            migrationBuilder.Sql("/*!80013 SET SQL_REQUIRE_PRIMARY_KEY = 1 */;");
            migrationBuilder.Sql("ALTER TABLE `OrleansStorage` ADD INDEX IX_OrleansStorage (GrainIdHash, GrainTypeHash);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("/*!80013 SET SQL_REQUIRE_PRIMARY_KEY = 0 */;");

            migrationBuilder.DropIndex(
                name: "IX_OrleansStorage",
                table: "OrleansStorage");

            migrationBuilder.DropPrimaryKey("PRIMARY", "OrleansStorage");
            migrationBuilder.Sql("ALTER TABLE `OrleansStorage` ADD PRIMARY KEY(Id, GrainIdHash, GrainTypeHash);");
            migrationBuilder.Sql("/*!80013 SET SQL_REQUIRE_PRIMARY_KEY = 1 */;");
        }
    }
}
