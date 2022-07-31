namespace WebSchedulerDataMigrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebScheduler.DataMigrations;

public static class Program
{
    public static void Main(string[] args)
        => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostBuilderContext, services) =>
        {
            Console.WriteLine(hostBuilderContext.Configuration["Storage:ConnectionString"]);
            services.AddDbContext<OrleansDbContext>(b => b.UseMySql(hostBuilderContext.Configuration["Storage:ConnectionString"],
                        ServerVersion.AutoDetect(hostBuilderContext.Configuration["Storage:ConnectionString"]),
                        dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName).EnableRetryOnFailure())
                    .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors());
        });
}

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
        var connectionString = configuration["Storage:ConnectionString"];

        var optionsBuilder = new DbContextOptionsBuilder<OrleansDbContext>()
           .UseMySql(configuration["Storage:ConnectionString"],
                        ServerVersion.AutoDetect(configuration["Storage:ConnectionString"]),
                        dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName).EnableRetryOnFailure())
                    .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        return new OrleansDbContext(optionsBuilder.Options);
    }
}
