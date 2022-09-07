namespace WebScheduler.Abstractions.Options;

using WebScheduler.Abstractions.Grains.Scheduler;

/// <summary>
/// Options to configure <see cref="IScheduledTaskGrain"/>
/// </summary>
public class ScheduledTaskGrainOptions
{
    /// <summary>
    /// The factory for creating <see cref="HttpClient"/>.
    /// </summary>
    public Func<HttpClient> ClientFactory { get; set; } = () => new HttpClient();
}
