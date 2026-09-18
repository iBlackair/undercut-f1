namespace UndercutF1.Gui;

public sealed record SectorDto(string Value, string Style, string Segments);

public sealed record DriverRowDto(
    int Pos,
    string Number,
    string Code,
    string TeamColor,
    bool Retired,
    bool Stopped,
    bool ChequeredFlag,
    int PositionChange,
    string Gap,
    string Interval,
    string IntervalStyle,
    bool OffTrack,
    string Best,
    string BestStyle,
    string Last,
    string LastStyle,
    SectorDto S1,
    SectorDto S2,
    SectorDto S3,
    string PitStatus,
    string PitStyle,
    string? TyreCompound,
    string? TyreLetter,
    int? TyreAge,
    bool TyreNew,
    string Compare
);

public sealed record RaceControlMessageDto(string Utc, string Message, string Category);

public sealed record TimingTowerSnapshotDto(
    bool SessionRunning,
    string? SessionName,
    string? SessionType,
    string? Location,
    int? CurrentLap,
    int? TotalLaps,
    string? TrackStatus,
    string? TrackStatusMessage,
    string LocalTime,
    string SessionRemaining,
    bool ClockPaused,
    string HeaderColumnLabel,
    bool IsRace,
    List<DriverRowDto> Drivers,
    List<RaceControlMessageDto> RaceControl
);

public sealed record SessionListEntryDto(string Location, string Date, string Session, string Directory);

public sealed record StartReplayRequest(string Directory);

public sealed record ApiErrorDto(string Message);
