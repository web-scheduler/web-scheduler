namespace WebScheduler.Abstractions.Grains.Scheduler;

using System;
using System.Collections.Generic;

/// <summary>
/// Typed Http trigger properties
/// </summary>
public class HttpTriggerProperties
{
    /// <summary>
    /// Retrieves the <see cref="HttpTriggerProperties"/> from the <seealso cref="ScheduledTaskMetadata.TaskProperties"/>.
    /// </summary>
    /// <param name="taskProperties"></param>
    public HttpTriggerProperties(Dictionary<string, object> taskProperties)
    {
        if (taskProperties.TryGetValue(nameof(this.EndpointUri), out var endpointUri))
        {
            ArgumentNullException.ThrowIfNull(endpointUri, nameof(endpointUri));
            this.EndpointUri = (Uri)endpointUri;
        }
        else
        {
            ArgumentNullException.ThrowIfNull(endpointUri, nameof(endpointUri));
            this.EndpointUri = default!;
        }

        if (taskProperties.TryGetValue(nameof(this.HttpMethod), out var httpVerb))
        {
            ArgumentNullException.ThrowIfNull(nameof(httpVerb));
            this.HttpMethod = (HttpMethod)httpVerb;
        }
        else
        {
            ArgumentNullException.ThrowIfNull(nameof(endpointUri));
            this.HttpMethod = default!;
        }
    }

    /// <summary>
    /// The endpoint url.
    /// </summary>
    public Uri EndpointUri { get; }

    /// <summary>
    /// The <see cref="HttpMethod"/> to use for the request.
    /// </summary>
    public HttpMethod HttpMethod { get; }

    /// <summary>
    ///  Factory for creating <see cref="HttpTriggerProperties"/> from <paramref name="taskProperties"/>.
    /// </summary>
    /// <param name="taskProperties"></param>
    /// <returns>A <see cref="HttpTriggerProperties"/></returns>
    public static HttpTriggerProperties FromKeyValuePair(Dictionary<string, object> taskProperties) => new(taskProperties);
}

/// <summary>
/// Providers helpers for <see cref="HttpTriggerProperties"/>.
/// </summary>
public static class HttpTaskPropertiesExtensions
{
    /// <summary>
    /// Creates a Dictionary from a <see cref="HttpTriggerProperties"/>.
    /// </summary>
    /// <param name="httpTaskProperties">A <see cref="HttpTriggerProperties"/>.</param>
    /// <returns>A dictionary encoded from <paramref name="httpTaskProperties"/>.</returns>
    public static Dictionary<string, object> GetKeyValuePairs(this HttpTriggerProperties httpTaskProperties) => new()
    {
        { nameof(httpTaskProperties.EndpointUri), httpTaskProperties.EndpointUri },
        { nameof(httpTaskProperties.HttpMethod), httpTaskProperties.HttpMethod },
    };
}
