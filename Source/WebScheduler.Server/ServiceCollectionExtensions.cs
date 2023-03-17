namespace WebScheduler.Server;

using System.Reflection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebScheduler.Server.Constants;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods which extend ASP.NET Core services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    static string GetHttpFlavour(string protocol)
    {
        if (HttpProtocol.IsHttp10(protocol))
        {
            return OpenTelemetryHttpFlavour.Http10;
        }
        else if (HttpProtocol.IsHttp11(protocol))
        {
            return OpenTelemetryHttpFlavour.Http11;
        }
        else if (HttpProtocol.IsHttp2(protocol))
        {
            return OpenTelemetryHttpFlavour.Http20;
        }
        else if (HttpProtocol.IsHttp3(protocol))
        {
            return OpenTelemetryHttpFlavour.Http30;
        }

        throw new InvalidOperationException($"Protocol {protocol} not recognised.");
    }

    /// <summary>
    /// Adds Open Telemetry services and configures instrumentation and exporters.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="webHostEnvironment">The environment the application is running under.</param>
    /// <returns>The services with open telemetry added.</returns>
    public static IServiceCollection AddCustomOpenTelemetryTracing(this IServiceCollection services, IWebHostEnvironment webHostEnvironment) =>
        services.AddOpenTelemetry().WithTracing(
            builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder
                        .CreateEmpty()
                        .AddService(
                            webHostEnvironment.ApplicationName,
                            serviceVersion: typeof(Startup).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version)
                        .AddAttributes(
                            new KeyValuePair<string, object>[]
                            {
                                new(OpenTelemetryAttributeName.Deployment.Environment, webHostEnvironment.EnvironmentName),
                                new(OpenTelemetryAttributeName.Host.Name, Environment.MachineName),
                            })
                        .AddEnvironmentVariableDetector())
                    .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;

                            // Enrich spans with additional request and response meta data.
                            // See https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/http.md
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                var context = request.HttpContext;
                                activity.SetTag(OpenTelemetryAttributeName.Http.Flavor, GetHttpFlavour(request.Protocol))
                                        .SetTag(OpenTelemetryAttributeName.Http.Scheme, request.Scheme)
                                        .SetTag(OpenTelemetryAttributeName.Http.ClientIP, context.Connection.RemoteIpAddress)
                                        .SetTag(OpenTelemetryAttributeName.Http.RequestContentLength, request.ContentLength)
                                        .SetTag(OpenTelemetryAttributeName.Http.RequestContentType, request.ContentType);

                                var user = context.User;
                                if (user.Identity?.Name is not null)
                                {
                                    activity.SetTag(OpenTelemetryAttributeName.EndUser.Id, user.Identity.Name)
                                        .SetTag(OpenTelemetryAttributeName.EndUser.Scope, string.Join(',', user.Claims.Select(x => x.Value)));
                                }
                            };

                            options.EnrichWithHttpResponse = (activity, response) => activity.SetTag(OpenTelemetryAttributeName.Http.ResponseContentLength, response.ContentLength)
                                        .SetTag(OpenTelemetryAttributeName.Http.ResponseContentType, response.ContentType);
                        });

                if (webHostEnvironment.IsDevelopment())
                {
                    _ = builder.AddConsoleExporter(
                        options => options.Targets = ConsoleExporterOutputTargets.Console | ConsoleExporterOutputTargets.Debug);
                }

                // TODO: Add OpenTelemetry.Instrumentation.* NuGet packages and configure them to collect more span data.
                //       E.g. Add the OpenTelemetry.Instrumentation.Http package to instrument calls to HttpClient.
                // TODO: Add OpenTelemetry.Exporter.* NuGet packages and configure them here to export open telemetry span data.
                //       E.g. Add the OpenTelemetry.Exporter.OpenTelemetryProtocol package to export span data to Jaeger.
            }).Services;
}
