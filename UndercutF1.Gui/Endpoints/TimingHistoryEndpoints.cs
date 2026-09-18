using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record TimingHistoryRowDto(
    int Line,
    string Number,
    string Code,
    string TeamColor,
    string GapToLeader,
    string GapDelta,
    string Interval,
    string IntervalDelta,
    string LastLap,
    string LastLapStyle,
    SectorDto S1,
    SectorDto S2,
    SectorDto S3
);

public sealed record TimingHistoryPageDto(
    bool SessionRunning,
    int Lap,
    int? TotalLaps,
    List<TimingHistoryRowDto> Rows
);

public sealed record ChartSeriesDto(string Code, string TeamColor, List<double?> Values);

public sealed record TimingHistoryChartDto(
    bool IsRace,
    List<int> Laps,
    List<ChartSeriesDto> GapSeries,
    List<ChartSeriesDto> LapTimeSeries,
    List<ChartSeriesDto> DeltaToAverageSeries
);

public static class TimingHistoryEndpoints
{
    private const int LapsInChart = 15;

    public static WebApplication MapTimingHistoryPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/timing-history/{lap:int}",
            (int lap, TimingDataProcessor timingData, LapCountProcessor lapCount, DriverListProcessor driverList, SessionInfoProcessor sessionInfo) =>
            {
                if (sessionInfo.Latest.Name is null)
                {
                    return Results.Ok(new TimingHistoryPageDto(false, lap, null, []));
                }

                var selectedLapDrivers = timingData.DriversByLap.GetValueOrDefault(lap);
                var previousLapDrivers = timingData.DriversByLap.GetValueOrDefault(lap - 1);

                if (selectedLapDrivers is null)
                {
                    return Results.Ok(
                        new TimingHistoryPageDto(true, lap, lapCount.Latest?.TotalLaps, [])
                    );
                }

                var rows = selectedLapDrivers
                    .OrderBy(x => x.Value.Line)
                    .Select(kv =>
                    {
                        var (driverNumber, line) = (kv.Key, kv.Value);
                        var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
                        var previousLap = previousLapDrivers?.GetValueOrDefault(driverNumber) ?? new();

                        var gapDelta = line.GapToLeaderSeconds() - previousLap.GapToLeaderSeconds();
                        var intervalDelta =
                            line.IntervalToPositionAhead?.IntervalSeconds()
                            - previousLap.IntervalToPositionAhead?.IntervalSeconds();

                        return new TimingHistoryRowDto(
                            line.Line ?? 0,
                            driver.RacingNumber ?? driverNumber,
                            driver.Tla ?? "UNK",
                            driver.TeamColour ?? "808080",
                            line.GapToLeader ?? "",
                            FormatDelta(gapDelta),
                            line.IntervalToPositionAhead?.Value ?? "",
                            FormatDelta(intervalDelta),
                            line.LastLapTime?.Value ?? "",
                            LastLapStyle(line.LastLapTime),
                            Sector(line.Sectors.GetValueOrDefault("0")),
                            Sector(line.Sectors.GetValueOrDefault("1")),
                            Sector(line.Sectors.GetValueOrDefault("2"))
                        );
                    })
                    .ToList();

                return Results.Ok(
                    new TimingHistoryPageDto(true, lap, lapCount.Latest?.TotalLaps, rows)
                );
            }
        );

        app.MapGet(
            "/api/timing-history/{lap:int}/chart",
            (int lap, TimingDataProcessor timingData, DriverListProcessor driverList, SessionInfoProcessor sessionInfo) =>
            {
                var minLap = lap - LapsInChart + 1;
                var maxLap = lap;
                var laps = Enumerable.Range(Math.Max(1, minLap), maxLap - Math.Max(1, minLap) + 1).ToList();

                var driverNumbers = driverList.Latest?.Keys.Where(k => k != "_kf").ToList() ?? [];

                var gapSeriesData = driverNumbers.ToDictionary(d => d, _ => new List<double?>());
                var lapTimeSeriesData = driverNumbers.ToDictionary(d => d, _ => new List<double?>());
                var deltaSeriesData = driverNumbers.ToDictionary(d => d, _ => new List<double?>());

                foreach (var lapNumber in laps)
                {
                    var lines = timingData.DriversByLap.GetValueOrDefault(lapNumber);
                    if (lines is null)
                    {
                        foreach (var d in driverNumbers)
                        {
                            gapSeriesData[d].Add(null);
                            lapTimeSeriesData[d].Add(null);
                            deltaSeriesData[d].Add(null);
                        }
                        continue;
                    }

                    var fastestLap =
                        lines.Values.Min(x => x.LastLapTime?.ToTimeSpan()) ?? TimeSpan.FromMinutes(2);
                    var threshold = fastestLap + TimeSpan.FromSeconds(30);

                    var averageLapMs = lines
                        .Where(x => driverList.IsSelected(x.Key))
                        .Select(x => x.Value.LastLapTime.ToTimeSpan())
                        .Where(x => x.HasValue)
                        .Select(x => x!.Value.TotalMilliseconds)
                        .DefaultIfEmpty(0)
                        .Average();

                    foreach (var d in driverNumbers)
                    {
                        var driverLine = lines.GetValueOrDefault(d);
                        if (driverLine is null)
                        {
                            gapSeriesData[d].Add(null);
                            lapTimeSeriesData[d].Add(null);
                            deltaSeriesData[d].Add(null);
                            continue;
                        }

                        gapSeriesData[d].Add((double?)lines.SmartGapToLeaderSeconds(d));

                        var lapTime = driverLine.LastLapTime?.ToTimeSpan();
                        if (lapTime > threshold || driverLine.IsPitLap || !lapTime.HasValue)
                        {
                            lapTimeSeriesData[d].Add(null);
                            deltaSeriesData[d].Add(null);
                        }
                        else
                        {
                            lapTimeSeriesData[d].Add(lapTime.Value.TotalMilliseconds);
                            deltaSeriesData[d].Add(lapTime.Value.TotalMilliseconds - averageLapMs);
                        }
                    }
                }

                ChartSeriesDto ToSeries(Dictionary<string, List<double?>> data, string driverNumber)
                {
                    var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
                    return new ChartSeriesDto(
                        driver.Tla ?? driverNumber,
                        driver.TeamColour ?? "808080",
                        data[driverNumber]
                    );
                }

                var selectedDrivers = driverNumbers.Where(d => driverList.IsSelected(d)).ToList();

                return Results.Ok(
                    new TimingHistoryChartDto(
                        sessionInfo.Latest.IsRace(),
                        laps,
                        selectedDrivers.Select(d => ToSeries(gapSeriesData, d)).ToList(),
                        selectedDrivers.Select(d => ToSeries(lapTimeSeriesData, d)).ToList(),
                        selectedDrivers.Select(d => ToSeries(deltaSeriesData, d)).ToList()
                    )
                );
            }
        );

        return app;
    }

    private static string FormatDelta(decimal? delta) =>
        delta switch
        {
            null => "",
            < 0 => $"{delta:0.000}",
            _ => $"+{delta:0.000}",
        };

    private static string LastLapStyle(TimingDataPoint.Driver.LapSectorTime? time) =>
        time?.OverallFastest ?? false ? "best" : time?.PersonalFastest ?? false ? "pb" : "normal";

    private static SectorDto Sector(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return new SectorDto("", "normal", "");
        var style = time.OverallFastest ?? false ? "best" : time.PersonalFastest ?? false ? "pb" : "normal";
        return new SectorDto(time.Value ?? "", style, "");
    }
}
