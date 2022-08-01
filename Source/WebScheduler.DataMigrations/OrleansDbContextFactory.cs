namespace WebScheduler.DataMigrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Used by EF tools at design time to create migrations.
/// </summary>
public class OrleansDbContextFactory : IDesignTimeDbContextFactory<OrleansDbContext>
{
    public OrleansDbContext CreateDbContext(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
          .AddUserSecrets("dotnet-WebScheduler.DataMigrations-C67C4BF2-BBB4-4CE3-84C9-40E277E34893")
          .AddEnvironmentVariables();

        var configuration = configurationBuilder.Build();
        var connectionString = configuration["ConnectionStrings:Default"];

        var optionsBuilder = new DbContextOptionsBuilder<OrleansDbContext>()
           .UseMySql(connectionString,
                        MySqlServerVersion.LatestSupportedServerVersion,
                        dbOpts => dbOpts.MigrationsAssembly(typeof(OrleansDbContextFactory).Assembly.FullName).EnableRetryOnFailure());
        return new OrleansDbContext(optionsBuilder.Options);
    }
}
