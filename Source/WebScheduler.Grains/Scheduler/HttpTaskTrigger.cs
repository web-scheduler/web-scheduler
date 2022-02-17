namespace WebScheduler.Grains.Scheduler;

using System;
using System.Collections.Generic;

/// <summary>
/// Factory for <see cref="HttpTaskProperties"/>.
/// </summary>
internal static class HttpTaskTrigger
{
    internal static HttpTaskProperties GetProperties(Dictionary<string, object> taskProperties) => new(taskProperties);
    internal class HttpTaskProperties
    {
        private const string EndpointUriKey = "EndpointUri";
        private const string HttpMethodKey = "HttpVerb";

        /// <summary>
        /// Retrieves the <see cref="HttpTaskProperties"/> from the <seealso cref="Abstractions.Grains.Scheduler.ScheduledTaskMetadata.TaskProperties"/>.
        /// </summary>
        /// <param name="taskProperties"></param>
        public HttpTaskProperties(Dictionary<string, object> taskProperties)
        {
            if (taskProperties.TryGetValue(EndpointUriKey, out var endpointUri))
            {
                ArgumentNullException.ThrowIfNull(endpointUri, nameof(endpointUri));
                this.EndpointUri = (Uri)endpointUri;
            }
            else
            {
                ArgumentNullException.ThrowIfNull(endpointUri, nameof(endpointUri));
                this.EndpointUri = default!;
            }

            if (taskProperties.TryGetValue(HttpMethodKey, out var httpVerb))
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

        public Uri EndpointUri { get; }
        public HttpMethod HttpMethod { get; }
    }
}
