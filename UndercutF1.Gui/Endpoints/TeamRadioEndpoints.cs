using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record TeamRadioCaptureDto(
    string Key,
    string Utc,
    string Number,
    string Code,
    string TeamColor,
    bool Downloaded,
    string? Transcription
);

public static class TeamRadioEndpoints
{
    public static WebApplication MapTeamRadioPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/team-radio",
            (TeamRadioProcessor teamRadio, DriverListProcessor driverList) =>
            {
                var captures = teamRadio
                    .Ordered.Select(kv =>
                    {
                        var driver = driverList.Latest?.GetValueOrDefault(kv.Value.RacingNumber ?? "") ?? new();
                        return new TeamRadioCaptureDto(
                            kv.Key,
                            kv.Value.Utc?.ToString("HH:mm:ss") ?? "",
                            kv.Value.RacingNumber ?? "",
                            driver.Tla ?? "UNK",
                            driver.TeamColour ?? "808080",
                            !string.IsNullOrEmpty(kv.Value.DownloadedFilePath),
                            kv.Value.Transcription
                        );
                    })
                    .ToList();

                return Results.Ok(captures);
            }
        );

        app.MapPost(
            "/api/team-radio/{key}/transcribe",
            async (string key, TeamRadioProcessor teamRadio, ILogger<Program> logger) =>
            {
                if (!teamRadio.Latest.Captures.ContainsKey(key))
                    return Results.NotFound();

                try
                {
                    await teamRadio.TranscribeAsync(key);
                    return Results.Ok(new { transcription = teamRadio.Latest.Captures[key].Transcription });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to transcribe team radio {Key}", key);
                    return Results.Problem(
                        title: "Failed to transcribe team radio",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway
                    );
                }
            }
        );

        app.MapGet(
            "/api/team-radio/{key}/audio",
            async (string key, TeamRadioProcessor teamRadio, ILogger<Program> logger) =>
            {
                if (!teamRadio.Latest.Captures.ContainsKey(key))
                    return Results.NotFound();

                try
                {
                    var filePath = await teamRadio.DownloadTeamRadioToFileAsync(key);
                    var stream = File.OpenRead(filePath);
                    return Results.File(stream, "audio/mpeg", enableRangeProcessing: true);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to download team radio {Key}", key);
                    return Results.Problem(
                        title: "Failed to download team radio",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway
                    );
                }
            }
        );

        return app;
    }
}
