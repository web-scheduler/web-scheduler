namespace WebScheduler.Abstractions.Tests;
using System;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using WebScheduler.Abstractions.Services;
using Xunit;

public class ExceptionObserverTests
{
    [Theory]
    [MemberData(nameof(AggregateExceptionTestData))]
    public async Task  IExceptionObserverExtensions_Works<T>(T exception)
        where T : Exception
    {
        var fakeExceptionObserver = A.Fake<IExceptionObserver>();

        Type typePassed = null!;
        A.CallTo(() => fakeExceptionObserver.OnException(A<T>.That.IsInstanceOf(typeof(T)))).ReturnsLazily(c =>
        {
            typePassed = c.Arguments[0].GetType();
            return ValueTask.CompletedTask;
        });

        await fakeExceptionObserver.ObserveException(exception);
        typePassed.Should().Be<T>();
    }

    public static IEnumerable<object[]> AggregateExceptionTestData()
    {
        var types = new Exception[]
        {
                new AggregateException(),
                new ArgumentNullException(),
                new InvalidOperationException()
        };

        foreach (var type in types)
        {
            yield return new[] { type };
        }
    }
}
