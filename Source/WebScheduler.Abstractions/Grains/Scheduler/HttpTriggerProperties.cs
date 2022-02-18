namespace WebScheduler.Abstractions.Grains.Scheduler;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>
/// Typed Http trigger properties
/// </summary>
public class HttpTriggerProperties
{
    /// <summary>
    /// The endpoint url.
    /// </summary>
    [Display(Name = "URL", Description = "The URL to deliver the trigger to.")]
    [Url]
    [Required]
    public string EndPointUrl { get; set; } = default!;

    /// <summary>
    /// The <see cref="HttpMethod"/> to use for the request.
    /// </summary>
    [Required]
    [Display(Name = "HTTP Method", Description = "The HTTP method to use for the request.")]
    public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
}
