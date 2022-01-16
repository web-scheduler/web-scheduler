namespace WebScheduler.Server.IntegrationTest;

using System;
using System.Threading.Tasks;
using WebScheduler.Abstractions.Grains;
using WebScheduler.Server.IntegrationTest.Fixtures;
using Xunit;
using Xunit.Abstractions;

public class CounterGrainTest : ClusterFixture
{
    public CounterGrainTest(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task AddCount_PassValue_ReturnsTotalCountAsync()
    {
        var grain = this.Cluster.GrainFactory.GetGrain<ICounterGrain>(Guid.Empty);

        var count = await grain.AddCountAsync(10L).ConfigureAwait(false);

        Assert.Equal(10L, count);
    }

    [Fact]
    public async Task GetCount_Default_ReturnsTotalCountAsync()
    {
        var grain = this.Cluster.GrainFactory.GetGrain<ICounterGrain>(Guid.Empty);

        var count = await grain.GetCountAsync().ConfigureAwait(false);

        Assert.Equal(0L, count);
    }
}
