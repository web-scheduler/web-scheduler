namespace WebScheduler.Server.Interceptors;

using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Abstractions.Grains;
using WebScheduler.Abstractions.Grains.Scheduler;

public class TenantValidationInterceptor : IIncomingGrainCallFilter
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<TenantValidationInterceptor> logger;

    public TenantValidationInterceptor(IGrainFactory grainFactory, ILogger<TenantValidationInterceptor> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        // Hook calls to any grain other than ICustomFilterGrain implementations.e
        // This avoids potential infinite recursion when calling OnReceivedCall() below.
        if (context.Grain is IScheduledTaskGrain &&
            context.InterfaceMethod.ReflectedType != typeof(ITenantScopedGrain<>) &&
            context.InterfaceMethod.ReflectedType != typeof(IRemindable)
            )
        {
            var filterGrain = this.grainFactory.GetGrain<ITenantScopedGrain<IScheduledTaskGrain>>(context.Grain.GetPrimaryKeyString());

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            var tenantId = RequestContext.Get(RequestContextKeys.TenantId) as string ?? throw new ArgumentNullException($"{RequestContextKeys.TenantId} not found in RequestContext");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

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

            this.logger.TenantUnauthorized(tenantId, context.Grain.GetPrimaryKeyString());
            throw new UnauthorizedAccessException();
        }

        // Continue invoking the call on the target grain.
        await context.Invoke().ConfigureAwait(true);
    }
}
