namespace WebScheduler.DataMigrations;
public class Migrator : BackgroundService
{
    private readonly ILogger<Migrator> logger;
    private readonly IHost host;

    public Migrator(ILogger<Migrator> logger, IHost host)
    {
        this.logger = logger;
        this.host = host;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        this.logger.LogInformation("Hello");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
        await this.host.StopAsync(stoppingToken).ConfigureAwait(true);
    }
}
