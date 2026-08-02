namespace CrashScript.Cli;

public static class AppInfo
{
    public const string Name = "CrashScript";

    public static string Version { get; } =
        ResolveVersion();

    private static string ResolveVersion()
    {
        Version? assemblyVersion =
            typeof(AppInfo)
                .Assembly
                .GetName()
                .Version;

        if (assemblyVersion is null)
        {
            return "0.1.0";
        }

        return assemblyVersion.ToString(3);
    }
}