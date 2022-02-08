namespace WebScheduler.Abstractions.Services;
/// <summary>
/// Retrieves the current date and/or time. Helps with unit testing by letting you mock the system clock.
/// </summary>
public class ClockService : IClockService
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
