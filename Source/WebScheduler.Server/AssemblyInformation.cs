namespace WebScheduler.Server;

using System.Reflection;

public record AssemblyInformation(string Product, string Description, string Version, string InformationalVersion)
{
    public static readonly AssemblyInformation Current = new(typeof(AssemblyInformation).Assembly);

    public AssemblyInformation(Assembly assembly)
        : this(
            assembly.GetCustomAttribute<AssemblyProductAttribute>()!.Product,
            assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description,
            assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version,
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion)
    {
    }
}
