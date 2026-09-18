using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record StintDto(
    string Compound,
    string Letter,
    bool IsNew,
    int StartLaps,
    int TotalLaps,
    int Length,
    int PitLap,
    string? BestLapTime,
    string? PitLaneDuration,
    string? PitStopTime
);

public sealed record TyreStintDriverDto(
    int Line,
    string Number,
    string Code,
    string TeamColor,
    List<StintDto> Stints
);

public sealed record TyreStintPageDto(
    bool SessionRunning,
    int? CurrentLap,
    int? TotalLaps,
    List<TyreStintDriverDto> Drivers
);

public static class TyreStintEndpoints
{
    public static WebApplication MapTyreStintPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/tyre-stints",
            (
                TimingAppDataProcessor timingAppData,
                PitLaneTimeCollectionProcessor pitLaneTimeCollection,
                PitStopSeriesProcessor pitStopSeries,
                DriverListProcessor driverList,
                LapCountProcessor lapCount,
                SessionInfoProcessor sessionInfo
            ) =>
            {
                var drivers = new List<TyreStintDriverDto>();

                foreach (var (driverNumber, line) in timingAppData.Latest.GetOrderedLines())
                {
                    var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
                    var stints = new List<StintDto>();

                    foreach (var (stintNumber, stint) in line.Stints.OrderBy(x => x.Key))
                    {
                        var pitLaneTime = pitLaneTimeCollection
                            .Latest.PitTimesList.GetValueOrDefault(driverNumber)
                            ?.ElementAtOrDefault(int.Parse(stintNumber) - 1);
                        var pitStop = pitStopSeries
                            .Latest.PitTimes.GetValueOrDefault(driverNumber)
                            ?.ElementAtOrDefault(int.Parse(stintNumber) - 1)
                            .Value?.PitStop;

                        stints.Add(
                            new StintDto(
                                stint.Compound ?? "UNKNOWN",
                                stint.Compound is { Length: > 0 } c ? c[0].ToString() : "?",
                                stint.New.GetValueOrDefault(),
                                stint.StartLaps ?? 0,
                                stint.TotalLaps ?? 0,
                                Math.Max(1, stint.GetStintDuration()),
                                line.Stints.GetPitLapForStint(stintNumber),
                                stint.LapTime,
                                pitLaneTime?.Duration,
                                pitStop?.PitStopTime
                            )
                        );
                    }

                    drivers.Add(
                        new TyreStintDriverDto(
                            line.Line ?? 0,
                            driver.RacingNumber ?? driverNumber,
                            driver.Tla ?? "UNK",
                            driver.TeamColour ?? "808080",
                            stints
                        )
                    );
                }

                return Results.Ok(
                    new TyreStintPageDto(
                        sessionInfo.Latest.Name is not null,
                        lapCount.Latest?.CurrentLap,
                        lapCount.Latest?.TotalLaps,
                        drivers
                    )
                );
            }
        );

        return app;
    }
}
