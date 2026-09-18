using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record RaceControlEntryDto(
    string Utc,
    string Message,
    string Category,
    string? Flag,
    string? Scope,
    int? Lap
);

public sealed record RaceControlPageDto(
    bool SessionRunning,
    string? TrackStatus,
    string? TrackStatusMessage,
    int? CurrentLap,
    int? TotalLaps,
    List<RaceControlEntryDto> Messages
);

public static class RaceControlEndpoints
{
    public static WebApplication MapRaceControlPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/race-control",
            (
                RaceControlMessageProcessor raceControl,
                TrackStatusProcessor trackStatus,
                LapCountProcessor lapCount,
                SessionInfoProcessor sessionInfo
            ) =>
            {
                var messages = raceControl
                    .Latest.Messages.OrderByDescending(x => x.Value.Utc)
                    .Take(100)
                    .Select(x => new RaceControlEntryDto(
                        x.Value.Utc.ToString("HH:mm:ss"),
                        x.Value.Message ?? "",
                        x.Value.Category ?? "",
                        x.Value.Flag,
                        x.Value.Scope,
                        x.Value.Lap
                    ))
                    .ToList();

                return Results.Ok(
                    new RaceControlPageDto(
                        sessionInfo.Latest.Name is not null,
                        trackStatus.Latest?.Status,
                        trackStatus.Latest?.Message,
                        lapCount.Latest?.CurrentLap,
                        lapCount.Latest?.TotalLaps,
                        messages
                    )
                );
            }
        );

        return app;
    }
}
