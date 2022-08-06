namespace WebScheduler.Server.IntegrationTest.Fixtures;

using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

public class TestClientBuilderConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) =>
        clientBuilder.AddSimpleMessageStreamProvider(Constants.StreamProviderName.Default);
}
