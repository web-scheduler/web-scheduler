namespace WebScheduler.Server.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.TestingHost;
using Serilog.Extensions.Logging;
using WebScheduler.Server.Constants;

public class TestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder) =>
        siloBuilder
            .ConfigureServices(services => services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory()))
            .AddMemoryGrainStorageAsDefault()
            .AddMemoryGrainStorage(GrainStorageProviderName.PubSubStore)
            .AddSimpleMessageStreamProvider(StreamProviderName.Default);
}
