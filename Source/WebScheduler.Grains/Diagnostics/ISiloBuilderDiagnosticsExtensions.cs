namespace Orleans.Hosting;

using WebScheduler.Grains.Diagnostics;

public static class ISiloBuilderDiagnosticsExtensions
{
    public static ISiloBuilder AddActivityPropagation(this ISiloBuilder @this) => @this.AddIncomingGrainCallFilter<ActivityPropagationIncomingGrainCallFilter>().AddOutgoingGrainCallFilter<ActivityPropagationOutgoingGrainCallFilter>();

    public static IClientBuilder AddDiagnostics(this IClientBuilder @this) => @this.AddOutgoingGrainCallFilter<ActivityPropagationOutgoingGrainCallFilter>();
}
