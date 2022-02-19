namespace WebScheduler.Abstractions.Grains.Scheduler;

using System.ComponentModel.DataAnnotations;
using WebScheduler.Abstractions.Validators;

/// <summary>
/// Typed Http trigger properties
/// </summary>
public class HttpTriggerProperties
{
    /// <summary>
    /// The default constructor.
    /// </summary>
    public HttpTriggerProperties()
    { }
    /// <summary>
    /// The endpoint url.
    /// </summary>
    [Display(Name = "URL", Description = "The URL to deliver the trigger to.")]
    [UrlHttpOrHttps]
    public string EndPointUrl { get; set; } = default!;

    /// <summary>
    /// The <see cref="HttpMethod"/> to use for the request.
    /// </summary>
    [Required]
    [Display(Name = "HTTP Method", Description = "The HTTP method to use for the request.")]
    public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
}
