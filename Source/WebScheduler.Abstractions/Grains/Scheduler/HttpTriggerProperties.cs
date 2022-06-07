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
    public string HttpMethod { get; set; } = System.Net.Http.HttpMethod.Get.Method;

    /// <summary>
    /// The Request Headers to send with the Http request.
    /// </summary>
    [Display(Name = "Headers", Description = "The Request Headers to use for the request.")]
    public List<Header> Headers { get; set; } = new();
}

/// <summary>
/// Represents a Header Key Value Pair.
/// </summary>
public class Header
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public Header()
    { }

    /// <summary>
    /// The Key name.
    /// </summary>
    [Required]
    [Display(Name = "Key", Description = "The header key.")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The Value.
    /// </summary>
    [Display(Name = "Key", Description = "The header value.")]
    public string Value { get; set; } = string.Empty;
}
