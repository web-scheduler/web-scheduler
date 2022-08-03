namespace WebScheduler.Server;

using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Statistics;
using WebScheduler.Abstractions.Constants;
using WebScheduler.Server.Options;
using Serilog;
using Serilog.Extensions.Hosting;
using WebScheduler.Grains.HealthChecks;
using Boxed.AspNetCore;
using WebScheduler.Server.Interceptors;
using WebScheduler.Grains.Constants;
using Serilog.Formatting.Compact;
using Orleans.Versions.Compatibility;
using Orleans.Versions.Selector;

#pragma warning disable RCS1102 // Make class static.
public class Program
#pragma warning restore RCS1102 // Make class static.
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = CreateBootstrapLogger();
        IHost? host = null;

        try
        {
            host = CreateHostBuilder(args).Build();

            host.LogApplicationStarted();
            await host.RunAsync().ConfigureAwait(true);
            host!.LogApplicationStopped();

            return 0;
        }
        catch (OrleansLifecycleCanceledException)
        {
            return 0;
        }
        catch (Exception exception)
        {
            host!.LogApplicationTerminatedUnexpectedly(exception);

            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureHostConfiguration(
                configurationBuilder => configurationBuilder.AddCustomBootstrapConfiguration(args))
            .ConfigureAppConfiguration(
                (hostingContext, configurationBuilder) =>
                {
                    hostingContext.HostingEnvironment.ApplicationName = AssemblyInformation.Current.Product;
                    _ = configurationBuilder.AddCustomConfiguration(hostingContext.HostingEnvironment, args);
                })
            .UseSerilog(ConfigureReloadableLogger)
            .UseDefaultServiceProvider(
                (context, options) =>
                {
                    var isDevelopment = context.HostingEnvironment.IsDevelopment();
                    options.ValidateScopes = isDevelopment;
                    options.ValidateOnBuild = isDevelopment;
                })
            .UseOrleans(ConfigureSiloBuilder)
            .ConfigureWebHost(ConfigureWebHostBuilder)
            .UseConsoleLifetime();

    private static void ConfigureSiloBuilder(
        Microsoft.Extensions.Hosting.HostBuilderContext context,
        ISiloBuilder siloBuilder) =>
        siloBuilder
            .AddIncomingGrainCallFilter<TenantValidationInterceptor>()
            .ConfigureServices(
                (context, services) =>
                {
                    _ = services.ConfigureAndValidateSingleton<ApplicationOptions>(context.Configuration);
                    _ = services.ConfigureAndValidateSingleton<ClusterMembershipOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.ClusterMembership)));
                    _ = services.ConfigureAndValidateSingleton<ClusterOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Cluster)));
                    _ = services.ConfigureAndValidateSingleton<StorageOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Storage)));
                })
            .Configure<GrainVersioningOptions>(options =>
            {
                options.DefaultCompatibilityStrategy = nameof(BackwardCompatible);
                options.DefaultVersionSelectorStrategy = nameof(AllCompatibleVersions);
            })
            .UseSiloUnobservedExceptionsHandler()
            .UseAdoNetClustering(options =>
                {
                    options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                    options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                })
            .ConfigureEndpoints(
                EndpointOptions.DEFAULT_SILO_PORT,
                EndpointOptions.DEFAULT_GATEWAY_PORT,
                listenOnAnyHostAddress: !context.HostingEnvironment.IsDevelopment())
            .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(StorageHealthCheckGrain).Assembly).WithReferences())
            .AddAdoNetGrainStorageAsDefault(options =>
                {
                    options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                    options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                    options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                    options.UseJsonFormat = true;
                })
            .AddAdoNetGrainStorage(GrainStorageProviderName.ScheduledTaskState, options =>
                {
                    options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                    options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                    options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                    options.UseJsonFormat = true;
                })
            .AddAdoNetGrainStorage(GrainStorageProviderName.ScheduledTaskMetadataHistory, options =>
                {
                    options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                    options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                    options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                    options.UseJsonFormat = true;
                })
            .AddAdoNetGrainStorage(GrainStorageProviderName.ScheduledTaskTriggerHistory, options =>
            {
                options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                options.UseJsonFormat = true;
            })
            .UseAdoNetReminderService(options =>
                {
                    options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                    options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                })
            .AddSimpleMessageStreamProvider(StreamProviderName.ScheduledTasks)
            .AddAdoNetGrainStorage(GrainStorageProviderName.PubSubStore, options =>
                {
                    options.Invariant = GetStorageOptions(context.Configuration).Invariant;
                    options.ConnectionString = GetStorageOptions(context.Configuration).ConnectionString;
                    options.ConfigureJsonSerializerSettings = ConfigureJsonSerializerSettings;
                    options.UseJsonFormat = true;
                })
            .UseIf(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), x => x.UseLinuxEnvironmentStatistics())
            .UseIf(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), x => x.UsePerfCounterEnvironmentStatistics())
            .UseDashboard(options => options.BasePath = GetOrleansDashboardOptions(context.Configuration).BasePath);

    private static void ConfigureWebHostBuilder(IWebHostBuilder webHostBuilder) =>
        webHostBuilder
            .UseKestrel(
                (builderContext, options) =>
                {
                    options.AddServerHeader = false;
                    _ = options.Configure(
                        builderContext.Configuration.GetSection(nameof(ApplicationOptions.Kestrel)),
                        reloadOnChange: false);
                })
            .UseStartup<Startup>();

    /// <summary>
    /// Creates a logger used during application initialisation.
    /// <see href="https://nblumhardt.com/2020/10/bootstrap-logger/"/>.
    /// </summary>
    /// <returns>A logger that can load a new configuration.</returns>
    private static ReloadableLogger CreateBootstrapLogger() =>
        new LoggerConfiguration()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateBootstrapLogger();

    /// <summary>
    /// Configures a logger used during the applications lifetime.
    /// <see href="https://nblumhardt.com/2020/10/bootstrap-logger/"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    private static void ConfigureReloadableLogger(
        Microsoft.Extensions.Hosting.HostBuilderContext context,
        IServiceProvider services,
        LoggerConfiguration configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

    private static void ConfigureJsonSerializerSettings(JsonSerializerSettings jsonSerializerSettings)
    {
        jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        jsonSerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
    }

    private static StorageOptions GetStorageOptions(IConfiguration configuration) =>
        configuration.GetSection(nameof(ApplicationOptions.Storage)).Get<StorageOptions>();

    private static OrleansDashboard.DashboardOptions GetOrleansDashboardOptions(IConfiguration configuration) =>
        configuration.GetSection(nameof(ApplicationOptions.OrleansDashboard)).Get<OrleansDashboard.DashboardOptions>();
}
