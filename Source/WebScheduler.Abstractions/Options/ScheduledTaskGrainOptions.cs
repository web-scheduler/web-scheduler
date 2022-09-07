namespace WebScheduler.Abstractions.Options;
public class ScheduledTaskGrainOptions
{
    public Func<HttpClient> ClientFactory = () => new HttpClient();
}
