namespace WebScheduler.Abstractions.Services;

/// <summary>
/// Retrieves the current date and/or time. Helps with unit testing by letting you mock the system clock.
/// </summary>
public interface IClockService
{
    /// <summary>
    /// The current UtcNow.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
