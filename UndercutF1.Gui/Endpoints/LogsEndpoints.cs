using InMemLogger;
using Microsoft.Extensions.Logging;

namespace UndercutF1.Gui;

public sealed record LogEntryDto(string Level, string Message, string? Exception);

public static class LogsEndpoints
{
    public static WebApplication MapLogsPageEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/logs",
            (InMemoryLogger inMemoryLogger, string? minLevel) =>
            {
                var minimumLevel = Enum.TryParse<LogLevel>(minLevel, out var parsed)
                    ? parsed
                    : LogLevel.Information;

                // RecordedLogs is backed by a plain List<T> that the logger keeps appending to from
                // other request threads, so a snapshot enumeration can race with a concurrent Add()
                // (an ArgumentException from List<T>.CopyTo). Retry a couple of times rather than 500.
                List<LogEntryDto>? logs = null;
                for (var attempt = 0; attempt < 3 && logs is null; attempt++)
                {
                    try
                    {
                        logs = inMemoryLogger
                            .RecordedLogs.ToArray()
                            .Where(x => x.Level >= minimumLevel)
                            .Reverse()
                            .Take(200)
                            .Select(x => new LogEntryDto(
                                x.Level.ToString(),
                                x.Message,
                                x.Exception?.ToString()
                            ))
                            .ToList();
                    }
                    catch (ArgumentException)
                    {
                        // Transient race with a concurrent writer; try again.
                    }
                }

                return Results.Ok(logs ?? []);
            }
        );

        return app;
    }
}
