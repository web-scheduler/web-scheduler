namespace WebScheduler.Abstractions.Grains.Scheduler;
using System.Runtime.Serialization;

/// <summary>
/// ErrorDeletingScheduledTask is thrown when we're unable to create a scheduled task for technical reasons.
/// </summary>
[Serializable]
public class ErrorDeletingScheduledTaskException : Exception
{
    /// <summary>
    /// Initializes a new instance of the System.Exception class with serialized data.
    /// </summary>
    /// <param name="serializationInfo"></param>
    /// <param name="streamingContext"></param>
    protected ErrorDeletingScheduledTaskException(SerializationInfo serializationInfo, StreamingContext streamingContext)
    { }

    /// <summary>Initializes a new instance of the <see cref="Exception" /> class.</summary>
    public ErrorDeletingScheduledTaskException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="Exception" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public ErrorDeletingScheduledTaskException(string? message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="Exception" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public ErrorDeletingScheduledTaskException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
