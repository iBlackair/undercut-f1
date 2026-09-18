using UndercutF1.Data;

namespace UndercutF1.Gui;

/// <summary>
/// Session lifecycle: starting a live session (which <c>ILiveTimingClient</c> records to
/// <c>subscribe.json</c>/<c>live.jsonl</c> automatically, unconditionally, per
/// <see cref="LiveTimingClient"/>), listing previously recorded sessions, and replaying one.
/// </summary>
public static class SessionEndpoints
{
    public static WebApplication MapSessionEndpoints(this WebApplication app)
    {
        app.MapGet("/api/sessions", GetSessions);
        app.MapPost("/api/sessions/live/start", StartLive);
        app.MapPost("/api/sessions/replay/start", StartReplay);

        return app;
    }

    private static async Task<IResult> GetSessions(IJsonTimingClient jsonTimingClient)
    {
        var directories = await jsonTimingClient.GetDirectoryNamesAsync();

        var entries = directories
            .SelectMany(group =>
                group.Value.Select(session => new SessionListEntryDto(
                    group.Key.Location,
                    group.Key.Date.ToString("yyyy-MM-dd"),
                    session.Session,
                    session.Directory
                ))
            )
            .ToList();

        return Results.Ok(entries);
    }

    private static async Task<IResult> StartLive(
        ILiveTimingClient liveTimingClient,
        SessionInfoProcessor sessionInfo,
        IDateTimeProvider dateTimeProvider,
        ILogger<Program> logger
    )
    {
        if (sessionInfo.Latest.Name is not null)
        {
            return Results.BadRequest(
                new ApiErrorDto(
                    $"A session ('{sessionInfo.Latest.Name}') is already running. Restart the app to switch sessions."
                )
            );
        }

        try
        {
            await liveTimingClient.StartAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start live timing session");
            return Results.Problem(
                title: "Failed to start live session",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway
            );
        }

        return Results.Ok(
            new ControlResponse(dateTimeProvider.IsPaused, true, sessionInfo.Latest.Name)
        );
    }

    private static IResult StartReplay(
        StartReplayRequest request,
        IJsonTimingClient jsonTimingClient,
        SessionInfoProcessor sessionInfo,
        IDateTimeProvider dateTimeProvider,
        ILogger<Program> logger
    )
    {
        if (sessionInfo.Latest.Name is not null)
        {
            return Results.BadRequest(
                new ApiErrorDto(
                    $"A session ('{sessionInfo.Latest.Name}') is already running. Restart the app to switch sessions."
                )
            );
        }

        if (string.IsNullOrWhiteSpace(request.Directory) || !Directory.Exists(request.Directory))
        {
            return Results.BadRequest(new ApiErrorDto("Unknown recording directory"));
        }

        // Fire-and-forget: this streams the whole recording in with the session's own delay,
        // the same way UndercutF1.Console's StartSimulatedSessionInputHandler does.
        _ = Task.Run(async () =>
        {
            try
            {
                await jsonTimingClient.LoadSimulationDataAsync(request.Directory, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to replay session from {Directory}", request.Directory);
            }
        });

        return Results.Accepted();
    }
}
