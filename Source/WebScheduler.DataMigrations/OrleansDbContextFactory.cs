namespace WebScheduler.DataMigrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using WebScheduler.DataMigrations.CompiledModels;

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
          .AddUserSecrets(typeof(OrleansDbContext).Assembly, optional: true)
          .AddEnvironmentVariables();

        var configuration = configurationBuilder.Build();
        var connectionString = configuration.GetConnectionString("Default");

        var optionsBuilder = new DbContextOptionsBuilder<OrleansDbContext>()
           .UseMySql(connectionString,
                        ServerVersion.AutoDetect(connectionString),
                        dbOpts => dbOpts.MigrationsAssembly(typeof(OrleansDbContextFactory).Assembly.FullName).EnableRetryOnFailure());

        // Use a precompiled model for performance gains.
        optionsBuilder.UseModel(OrleansDbContextModel.Instance);

        return new OrleansDbContext(optionsBuilder.Options);
    }
}
