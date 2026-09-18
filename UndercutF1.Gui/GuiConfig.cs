namespace UndercutF1.Gui;

/// <summary>
/// Resolves the same <c>config.json</c> location that <c>UndercutF1.Console</c> uses,
/// so the GUI and the TUI share configuration (data directory, F1TV token, etc.).
/// </summary>
public static class GuiConfig
{
    public static string ConfigFilePath => GetConfigFilePath();

    private static string GetConfigFilePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Join(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData,
                    Environment.SpecialFolderOption.Create
                ),
                "undercut-f1",
                "config.json"
            );
        }

        var xdgConfigDirectory = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(xdgConfigDirectory))
        {
            xdgConfigDirectory = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config"
            );
        }

        return Path.Join(xdgConfigDirectory, "undercut-f1", "config.json");
    }
}
