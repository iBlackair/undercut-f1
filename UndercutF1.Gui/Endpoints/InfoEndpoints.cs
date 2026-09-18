using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record InfoPageDto(
    string DataDirectory,
    string LogDirectory,
    bool Notify,
    bool Verbose,
    string F1TvStatus,
    string? F1TvExpiry,
    string ConfigFilePath,
    bool ConfigFileExists,
    string Version,
    string Os,
    int Port
);

public static class InfoEndpoints
{
    public static WebApplication MapInfoPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/info",
            (IOptions<LiveTimingOptions> options, Formula1Account account) =>
                Results.Ok(
                    new InfoPageDto(
                        options.Value.DataDirectory,
                        options.Value.LogDirectory,
                        options.Value.Notify,
                        options.Value.Verbose,
                        account.IsAuthenticated.ToString(),
                        account.Payload?.Expiry.ToString("yyyy-MM-dd"),
                        GuiConfig.ConfigFilePath,
                        File.Exists(GuiConfig.ConfigFilePath),
                        ThisAssembly.AssemblyInformationalVersion,
                        RuntimeInformation.OSDescription,
                        61938
                    )
                )
        );

        return app;
    }
}
