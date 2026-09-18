using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record ChampionshipDriverDto(
    int Position,
    int PositionChange,
    string Number,
    string Code,
    string TeamColor,
    decimal CurrentPoints,
    decimal PredictedPoints,
    decimal Change,
    decimal GapToNext
);

public sealed record ChampionshipTeamDto(
    int Position,
    int PositionChange,
    string TeamName,
    string TeamColor,
    decimal CurrentPoints,
    decimal PredictedPoints,
    decimal Change,
    decimal GapToNext
);

public sealed record SpeedTrapEntryDto(string Code, string TeamColor, string Speed);

public sealed record SpeedTrapDto(string Trap, List<SpeedTrapEntryDto> Entries);

public sealed record ChampionshipPageDto(
    bool Available,
    string Reason,
    List<ChampionshipDriverDto> Drivers,
    List<ChampionshipTeamDto> Teams,
    List<SpeedTrapDto> SpeedTraps
);

public static class ChampionshipEndpoints
{
    private static readonly string[] SpeedTrapKeys = ["FL", "I1", "I2", "ST"];

    public static WebApplication MapChampionshipPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/championship",
            (
                SessionInfoProcessor sessionInfo,
                ChampionshipPredictionProcessor championshipPrediction,
                TimingStatsProcessor timingStats,
                DriverListProcessor driverList,
                Formula1Account account
            ) =>
            {
                if (!sessionInfo.Latest.IsRace())
                {
                    return Results.Ok(
                        new ChampionshipPageDto(
                            false,
                            "Championship tables are only available during race sessions.",
                            [],
                            [],
                            []
                        )
                    );
                }

                if (championshipPrediction.Latest.Teams.Count == 0)
                {
                    return Results.Ok(
                        new ChampionshipPageDto(
                            false,
                            $"Unable to find championship data for the current session. This requires an F1 TV subscription. Current account status: {account.IsAuthenticated}.",
                            [],
                            [],
                            []
                        )
                    );
                }

                var driverRows = championshipPrediction
                    .Latest.Drivers.OrderBy(x => x.Value.PredictedPosition)
                    .ToList();
                var prevDriverPoints = driverRows.FirstOrDefault().Value?.PredictedPoints ?? 0;

                var drivers = driverRows
                    .Select(x =>
                    {
                        var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                        var gap = prevDriverPoints - (x.Value.PredictedPoints ?? 0);
                        prevDriverPoints = x.Value.PredictedPoints ?? 0;
                        return new ChampionshipDriverDto(
                            x.Value.PredictedPosition ?? 0,
                            (x.Value.PredictedPosition ?? 0) - (x.Value.CurrentPosition ?? 0),
                            driver.RacingNumber ?? x.Key,
                            driver.Tla ?? "UNK",
                            driver.TeamColour ?? "808080",
                            x.Value.CurrentPoints ?? 0,
                            x.Value.PredictedPoints ?? 0,
                            (x.Value.PredictedPoints ?? 0) - (x.Value.CurrentPoints ?? 0),
                            -gap
                        );
                    })
                    .ToList();

                var teamRows = championshipPrediction
                    .Latest.Teams.OrderBy(x => x.Value.PredictedPosition)
                    .ToList();
                var prevTeamPoints = teamRows.FirstOrDefault().Value?.PredictedPoints ?? 0;

                var teams = teamRows
                    .Select(x =>
                    {
                        var driver = driverList.Latest.FirstOrDefault(d =>
                            d.Value.TeamName == x.Value.TeamName
                        ).Value;
                        var gap = prevTeamPoints - (x.Value.PredictedPoints ?? 0);
                        prevTeamPoints = x.Value.PredictedPoints ?? 0;
                        return new ChampionshipTeamDto(
                            x.Value.PredictedPosition ?? 0,
                            (x.Value.PredictedPosition ?? 0) - (x.Value.CurrentPosition ?? 0),
                            x.Key,
                            driver?.TeamColour ?? "808080",
                            x.Value.CurrentPoints ?? 0,
                            x.Value.PredictedPoints ?? 0,
                            (x.Value.PredictedPoints ?? 0) - (x.Value.CurrentPoints ?? 0),
                            -gap
                        );
                    })
                    .ToList();

                var speedTraps = SpeedTrapKeys.Select(trapKey =>
                {
                    var entries = timingStats
                        .Latest.Lines.OrderBy(x => x.Value.BestSpeeds.GetValueOrDefault(trapKey)?.Position)
                        .Select(x =>
                        {
                            var driver = driverList.Latest.GetValueOrDefault(x.Key) ?? new();
                            var speed = x.Value.BestSpeeds.GetValueOrDefault(trapKey)?.Value ?? "";
                            return (driver, speed);
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x.speed))
                        .Select(x => new SpeedTrapEntryDto(
                            x.driver.Tla ?? "UNK",
                            x.driver.TeamColour ?? "808080",
                            x.speed
                        ))
                        .ToList();
                    return new SpeedTrapDto(trapKey, entries);
                })
                    .ToList();

                return Results.Ok(new ChampionshipPageDto(true, "", drivers, teams, speedTraps));
            }
        );

        return app;
    }
}
