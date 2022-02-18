namespace WebScheduler.Server.Interceptors;

using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains;
using WebScheduler.Abstractions.Grains.Scheduler;

public class TenentValidationInterceptor : IIncomingGrainCallFilter
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<TenentValidationInterceptor> logger;

    public TenentValidationInterceptor(IGrainFactory grainFactory, ILogger<TenentValidationInterceptor> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        // Hook calls to any grain other than ICustomFilterGrain implementations.e
        // This avoids potential infinite recursion when calling OnReceivedCall() below.
        if (context.Grain is IScheduledTaskGrain && context.InterfaceMethod.ReflectedType != typeof(ITenentScopedGrain<>))
        {
            var filterGrain = this.grainFactory.GetGrain<ITenentScopedGrain<IScheduledTaskGrain>>(context.Grain.GetPrimaryKeyString());

            var tenantId = RequestContext.Get(RequestContextKeys.TenentId) as string ?? throw new ArgumentNullException($"{RequestContextKeys.TenentId} not found in RequestContext");

            var tenantIdAsGuid = Guid.ParseExact(tenantId, "D");
            var valid = await filterGrain.IsOwnedByAsync(tenantIdAsGuid).ConfigureAwait(true);

            if (valid is null)
            {
                await filterGrain.SetOwnedByAsync(tenantIdAsGuid).ConfigureAwait(true);
                await context.Invoke().ConfigureAwait(true);
                return;
            }
            else if (valid.Value)
            {
                // Continue invoking the call on the target grain.
                await context.Invoke().ConfigureAwait(true);
                return;
            }

            this.logger.LogWarning("Tenent {TenentId} is not authorized to access {ScheduledTaskId}", tenantId, context.Grain.GetPrimaryKeyString());
            throw new UnauthorizedAccessException();
        }

        // Continue invoking the call on the target grain.
        await context.Invoke().ConfigureAwait(true);
    }
}
