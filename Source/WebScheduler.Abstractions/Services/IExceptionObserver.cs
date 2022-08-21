namespace WebScheduler.Abstractions.Services;
using System;
using System.Threading.Tasks;

/// <summary>
/// Allows for consumers of the WebScheduler packages to observe internal exceptions and context of the exception.
/// </summary>
public interface IExceptionObserver
{
    /// <summary>
    /// Called when an exception occurs with a grain.
    /// </summary>
    /// <param name="exception">The exception.</param>
    ValueTask OnException(Exception exception);
}

/// <summary>
/// An example ExceptionObserver that does nothing.
/// </summary>
public class NoOpExceptionObserver : IExceptionObserver
{
    /// <summary>
    /// Called when an exception occurs with a grain.
    /// </summary>
    /// <param name="exception"></param>
    public ValueTask OnException(Exception exception) => ValueTask.CompletedTask;
}
