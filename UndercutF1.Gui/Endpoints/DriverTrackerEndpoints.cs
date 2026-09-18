using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record TrackCornerDto(int Number, double X, double Y);

public sealed record TrackDriverDto(
    string Number,
    string Code,
    string TeamColor,
    double X,
    double Y,
    bool OffTrack,
    bool Selected,
    string Gap
);

public sealed record DriverTrackerPageDto(
    bool SessionRunning,
    bool HasCircuit,
    bool HasPositions,
    string Reason,
    double ViewWidth,
    double ViewHeight,
    List<double[]> TrackPoints,
    List<TrackCornerDto> Corners,
    List<TrackDriverDto> Drivers
);

public static class DriverTrackerEndpoints
{
    public static WebApplication MapDriverTrackerPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/driver-tracker",
            (
                SessionInfoProcessor sessionInfo,
                PositionDataProcessor positionData,
                TimingDataProcessor timingData,
                DriverListProcessor driverList,
                Formula1Account account
            ) =>
            {
                var sessionRunning = sessionInfo.Latest.Name is not null;
                var circuitPoints = sessionInfo.Latest.CircuitPoints;
                var latestPositions = positionData.Latest.Position.LastOrDefault()?.Entries ?? [];

                if (!sessionRunning)
                {
                    return Results.Ok(
                        new DriverTrackerPageDto(false, false, false, "No session running.", 0, 0, [], [], [])
                    );
                }

                if (circuitPoints.Count == 0)
                {
                    return Results.Ok(
                        new DriverTrackerPageDto(
                            true,
                            false,
                            false,
                            "Unable to load circuit map data for this session.",
                            0,
                            0,
                            [],
                            [],
                            []
                        )
                    );
                }

                if (latestPositions.Count == 0)
                {
                    return Results.Ok(
                        new DriverTrackerPageDto(
                            true,
                            true,
                            false,
                            $"No car position data available yet. This feed requires an F1 TV subscription. Current account status: {account.IsAuthenticated}.",
                            0,
                            0,
                            [],
                            [],
                            []
                        )
                    );
                }

                var minX = circuitPoints.Min(p => p.x);
                var minY = circuitPoints.Min(p => p.y);
                var maxX = circuitPoints.Max(p => p.x);
                var maxY = circuitPoints.Max(p => p.y);

                // Flip Y (track coordinate space has Y growing the opposite way to SVG/image space)
                // and shift everything in to a 0-based viewBox, matching UndercutF1.Console's TransformPoint.
                (double x, double y) Transform(double x, double y) => (x - minX, maxY - y);

                var trackPoints = circuitPoints.Select(p => Transform(p.x, p.y)).Select(p => new[] { p.x, p.y }).ToList();

                var corners = sessionInfo
                    .Latest.CircuitCorners.Select(c =>
                    {
                        var (x, y) = Transform(c.x, c.y);
                        return new TrackCornerDto(c.number, x, y);
                    })
                    .ToList();

                var drivers = driverList
                    .Latest.Select(kv =>
                    {
                        var (driverNumber, driver) = (kv.Key, kv.Value);
                        var position = latestPositions.GetValueOrDefault(driverNumber);
                        if (position is null || !position.X.HasValue || !position.Y.HasValue)
                            return null;

                        var (x, y) = Transform(position.X.Value, position.Y.Value);
                        var line = timingData.Latest.Lines.GetValueOrDefault(driverNumber);
                        var offTrack =
                            position.Status
                            == PositionDataPoint.PositionData.Entry.DriverStatus.OffTrack;

                        return new TrackDriverDto(
                            driver.RacingNumber ?? driverNumber,
                            driver.Tla ?? "UNK",
                            driver.TeamColour ?? "808080",
                            x,
                            y,
                            offTrack,
                            driver.IsSelected,
                            line?.GapToLeader ?? line?.IntervalToPositionAhead?.Value ?? ""
                        );
                    })
                    .Where(x => x is not null)
                    .Select(x => x!)
                    .ToList();

                return Results.Ok(
                    new DriverTrackerPageDto(
                        true,
                        true,
                        true,
                        "",
                        maxX - minX,
                        maxY - minY,
                        trackPoints,
                        corners,
                        drivers
                    )
                );
            }
        );

        return app;
    }
}
