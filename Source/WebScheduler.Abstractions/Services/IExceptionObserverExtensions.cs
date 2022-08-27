namespace WebScheduler.Abstractions.Services;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="IExceptionObserver"/>
/// </summary>
public static class IExceptionObserverExtensions
{
    /// <summary>
    /// Publishes an exception to the observer and returns a task that completes when the exception is observed accounting for <see cref="ValueTask"/>.
    /// </summary>
    /// <param name="exceptionObserver"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask ObserveException(this IExceptionObserver? exceptionObserver, Exception exception)
    {
        if (exceptionObserver is null)
        {
            return ValueTask.CompletedTask;
        }

        var valueTask = exceptionObserver.OnException(exception);
        if (valueTask.IsCompleted)
        {
            return ValueTask.CompletedTask;
        }

        return valueTask;
    }
}
