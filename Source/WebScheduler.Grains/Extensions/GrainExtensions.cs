namespace Orleans;

internal static class GrainExtensions
{
    public static Task SelfInvokeAfter<T>(this Grain grain, Func<T, Task> action)
        where T : IGrain
    {
        var g = grain.AsReference<T>();
        return action(g);
    }
}
