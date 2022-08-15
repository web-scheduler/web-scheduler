namespace TaskScheduler.ServiceHost.Server.Interceptors;

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
        var tenantId = string.Empty;

        try
        {
            // Hook calls to any grain other than ICustomFilterGrain implementations.e
            // This avoids potential infinite recursion when calling OnReceivedCall() below.
            if (context.Grain is IScheduledTaskGrain &&
                context.InterfaceMethod.ReflectedType != typeof(ITenantScopedGrain<>) &&
                context.InterfaceMethod.ReflectedType != typeof(IRemindable))
            {
                tenantId = RequestContext.Get(RequestContextKeys.TenantId) as string ?? throw new ArgumentNullException($"{RequestContextKeys.TenantId} not found in RequestContext");

                var tenantIdAsGuid = Guid.ParseExact(tenantId, "D");

                var filterGrain = this.grainFactory.GetGrain<ITenantScopedGrain<IScheduledTaskGrain>>(context.Grain.GetPrimaryKeyString());

                var valid = await filterGrain.IsOwnedByAsync(tenantIdAsGuid);

                // Only claim a ScheduledTaskId for a Create
                if (valid is null && context.ImplementationMethod.Name == nameof(IScheduledTaskGrain.CreateAsync))
                {
                    await context.Invoke();
                    return;
                }

                if (valid == true)
                {
                    // Continue invoking the call on the target grain.
                    await context.Invoke();
                    return;
                }

                this.logger.TenantUnauthorized(tenantId, context.Grain.GetPrimaryKeyString());
                throw new UnauthorizedAccessException();
            }

            // Continue invoking the call on the target grain.
            try
            {
                await context.Invoke();
            }
            catch (Exception)
            {
                throw;
            }
        }
        catch (Exception exception)
        {
            this.logger.TenantException(exception, tenantId, context.Grain.GetPrimaryKeyString());
        }
    }
}
