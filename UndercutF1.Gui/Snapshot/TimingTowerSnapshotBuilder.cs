using UndercutF1.Data;
using Status = UndercutF1.Data.TimingDataPoint.Driver.StatusFlags;

namespace UndercutF1.Gui;

/// <summary>
/// Builds a plain-DTO snapshot of the current timing state for the web GUI, mirroring the
/// logic in <c>UndercutF1.Console.Display.TimingTowerDisplay</c> and <c>CommonDisplayComponents</c>
/// but shaped for JSON rather than Spectre.Console rendering.
/// </summary>
public sealed class TimingTowerSnapshotBuilder(
    TimingDataProcessor timingData,
    TimingAppDataProcessor timingAppData,
    DriverListProcessor driverList,
    LapCountProcessor lapCountProcessor,
    SessionInfoProcessor sessionInfo,
    PositionDataProcessor positionData,
    TrackStatusProcessor trackStatus,
    RaceControlMessageProcessor raceControlMessages,
    ExtrapolatedClockProcessor extrapolatedClock,
    IDateTimeProvider dateTimeProvider
)
{
    public TimingTowerSnapshotDto Build()
    {
        var session = sessionInfo.Latest;
        var sessionRunning = session.Name is not null;
        var isRace = session.IsRace();

        var drivers = sessionRunning
            ? (isRace ? BuildRaceRows() : BuildNonRaceRows())
            : [];

        var raceControl = raceControlMessages
            .Latest.Messages.OrderByDescending(x => x.Value.Utc)
            .Take(8)
            .Select(x => new RaceControlMessageDto(
                x.Value.Utc.ToString("HH:mm:ss"),
                x.Value.Message ?? "",
                x.Value.Category ?? ""
            ))
            .ToList();

        var localOffset = string.IsNullOrWhiteSpace(session.GmtOffset)
            ? TimeSpan.Zero
            : TimeSpan.Parse(session.GmtOffset);
        var localDate = dateTimeProvider.Utc.ToOffset(localOffset);

        return new TimingTowerSnapshotDto(
            sessionRunning,
            session.Name,
            session.Type,
            session.Meeting?.Name,
            lapCountProcessor.Latest?.CurrentLap,
            lapCountProcessor.Latest?.TotalLaps,
            trackStatus.Latest?.Status,
            trackStatus.Latest?.Message,
            localDate.ToString(@"HH\:mm\:ss"),
            FormatRemaining(extrapolatedClock.ExtrapolatedRemaining()),
            dateTimeProvider.IsPaused,
            isRace ? "Leader" : (session.Name ?? "Driver"),
            isRace,
            drivers,
            raceControl
        );
    }

    private static string FormatRemaining(TimeSpan remaining) =>
        remaining < TimeSpan.Zero ? "00:00:00" : remaining.ToString(@"hh\:mm\:ss");

    private List<DriverRowDto> BuildRaceRows()
    {
        if (timingData.Latest is null || timingData.Latest.Lines.Count == 0)
            return [];

        var lines = timingData.Latest.GetOrderedLines();

        var fastestLastLap = timingData
            .Latest.Lines.Values.MinBy(x => x.LastLapTime?.ToTimeSpan())
            ?.LastLapTime;
        var fastestBestLap = timingData
            .Latest.Lines.Values.MinBy(x => x.BestLapTime?.ToTimeSpan())
            ?.BestLapTime;

        var rows = new List<DriverRowDto>();

        foreach (var (driverNumber, line) in lines)
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var position =
                positionData.Latest.Position.LastOrDefault()?.Entries.GetValueOrDefault(driverNumber)
                ?? new();
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;
            var positionChange = (line.Line ?? 0) - (driver.Line ?? line.Line ?? 0);
            var offTrack =
                position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack;

            var pitStatus = line.InPit.GetValueOrDefault() ? "IN"
                : line.PitOut.GetValueOrDefault() ? "OUT"
                : $"{line.NumberOfPitStops ?? 0}";
            var pitStyle = line.InPit.GetValueOrDefault() ? "in"
                : line.PitOut.GetValueOrDefault() ? "out"
                : "normal";

            rows.Add(
                new DriverRowDto(
                    line.Line ?? 0,
                    driver.RacingNumber ?? driverNumber,
                    driver.Tla ?? "UNK",
                    driver.TeamColour ?? "808080",
                    line.Retired.GetValueOrDefault(),
                    line.Stopped.GetValueOrDefault(),
                    line.Status?.HasFlag(Status.ChequeredFlag) ?? false,
                    Math.Sign(positionChange),
                    line.GapToLeader ?? "",
                    offTrack ? "OFF TRK" : line.IntervalToPositionAhead?.Value ?? "",
                    IntervalStyle(line.IntervalToPositionAhead),
                    offTrack,
                    line.BestLapTime?.Value ?? "",
                    BestLapStyle(line.BestLapTime, fastestBestLap),
                    line.LastLapTime?.Value ?? "",
                    LastLapStyle(line.LastLapTime, fastestLastLap),
                    Sector(line.Sectors.GetValueOrDefault("0")),
                    Sector(line.Sectors.GetValueOrDefault("1")),
                    Sector(line.Sectors.GetValueOrDefault("2")),
                    pitStatus,
                    pitStyle,
                    stint?.Compound,
                    stint?.Compound is { Length: > 0 } c ? c[0].ToString() : null,
                    stint?.TotalLaps,
                    stint?.New.GetValueOrDefault() ?? false,
                    line.GapToLeader ?? ""
                )
            );
        }

        return rows;
    }

    private List<DriverRowDto> BuildNonRaceRows()
    {
        if (timingData.Latest is null || timingData.Latest.Lines.Count == 0)
            return [];

        var lines = timingData.Latest.GetOrderedLines();
        var bestDriver = lines.FirstOrDefault();

        var rows = new List<DriverRowDto>();

        foreach (var (driverNumber, line) in lines)
        {
            var driver = driverList.Latest?.GetValueOrDefault(driverNumber) ?? new();
            var position =
                positionData.Latest.Position.LastOrDefault()?.Entries.GetValueOrDefault(driverNumber)
                ?? new();
            var appData = timingAppData.Latest?.Lines.GetValueOrDefault(driverNumber) ?? new();
            var stint = appData.Stints.LastOrDefault().Value;
            var offTrack =
                position.Status == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack;

            var gapToLeaderSeconds =
                bestDriver.Value is not null
                    ? (line.BestLapTime.ToTimeSpan() - bestDriver.Value.BestLapTime.ToTimeSpan())
                        ?.TotalSeconds
                    : null;
            var gap =
                gapToLeaderSeconds is null ? ""
                : gapToLeaderSeconds == 0 ? "LEADER"
                : $"{(gapToLeaderSeconds > 0 ? "+" : "")}{gapToLeaderSeconds:f3}";

            var pitStatus = line.InPit.GetValueOrDefault() ? "IN"
                : line.PitOut.GetValueOrDefault() ? "OUT"
                : $"{line.NumberOfPitStops ?? 0}";
            var pitStyle = line.InPit.GetValueOrDefault() ? "in"
                : line.PitOut.GetValueOrDefault() ? "out"
                : "normal";

            rows.Add(
                new DriverRowDto(
                    line.Line ?? 0,
                    driver.RacingNumber ?? driverNumber,
                    driver.Tla ?? "UNK",
                    driver.TeamColour ?? "808080",
                    line.KnockedOut.GetValueOrDefault(),
                    false,
                    line.Status?.HasFlag(Status.ChequeredFlag) ?? false,
                    0,
                    gap,
                    offTrack ? "OFF TRK" : "",
                    "normal",
                    offTrack,
                    line.BestLapTime?.Value ?? "",
                    "normal",
                    line.LastLapTime?.Value ?? "",
                    LastLapStyle(line.LastLapTime, null),
                    Sector(line.Sectors.GetValueOrDefault("0")),
                    Sector(line.Sectors.GetValueOrDefault("1")),
                    Sector(line.Sectors.GetValueOrDefault("2")),
                    pitStatus,
                    pitStyle,
                    stint?.Compound,
                    stint?.Compound is { Length: > 0 } c ? c[0].ToString() : null,
                    stint?.TotalLaps,
                    stint?.New.GetValueOrDefault() ?? false,
                    gap
                )
            );
        }

        return rows;
    }

    private static string IntervalStyle(TimingDataPoint.Driver.Interval? interval)
    {
        var seconds = interval?.IntervalSeconds();
        return seconds is > 0 and < 1 ? "fast" : "normal";
    }

    private static string BestLapStyle(
        TimingDataPoint.Driver.BestLap? time,
        TimingDataPoint.Driver.BestLap? fastest
    ) =>
        !string.IsNullOrWhiteSpace(time?.Value) && fastest?.ToTimeSpan() == time?.ToTimeSpan()
            ? "best"
            : "normal";

    private static string LastLapStyle(
        TimingDataPoint.Driver.LapSectorTime? time,
        TimingDataPoint.Driver.LapSectorTime? fastest
    ) =>
        string.IsNullOrWhiteSpace(time?.Value) ? "normal"
        : fastest is not null && fastest.ToTimeSpan() == time.ToTimeSpan() ? "best"
        : time.OverallFastest ?? false ? "best"
        : time.PersonalFastest ?? false ? "pb"
        : "normal";

    private static SectorDto Sector(TimingDataPoint.Driver.LapSectorTime? time)
    {
        if (time is null)
            return new SectorDto("", "normal", "");

        if (string.IsNullOrWhiteSpace(time.Value) && time.Segments.Count > 0)
        {
            var segments = time.Segments.Values.Select(segment =>
                segment.Status.GetValueOrDefault() switch
                {
                    var s when s.HasFlag(Status.PitLane) => 'd',
                    var s when s.HasFlag(Status.OverallBest) => 'o',
                    var s when s.HasFlag(Status.PersonalBest) => 'p',
                    var s when s.HasFlag(Status.SegmentComplete) => 'y',
                    _ => '_',
                }
            );
            return new SectorDto("", "normal", new string([.. segments]));
        }

        var style = time.OverallFastest ?? false ? "best" : time.PersonalFastest ?? false ? "pb" : "normal";
        return new SectorDto(time.Value ?? "", style, "");
    }
}
