#nullable disable

namespace WebScheduler.DataMigrations.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(GetFromResources("WebScheduler.DataMigrations.Migrations.BaseMySQL.sql"));
    private static string GetFromResources(string filename)
    {
        var thisAssembly = typeof(Initial).Assembly;
        using var stream = thisAssembly.GetManifestResourceStream(filename);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    { }
}
