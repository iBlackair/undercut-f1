using Microsoft.AspNetCore.Mvc;
using UndercutF1.Data;

namespace UndercutF1.Gui;

public enum ControlOperation
{
    PauseClock,
    ResumeClock,
    ToggleClock,
}

public sealed record ControlRequest(ControlOperation Operation);

public sealed record ControlResponse(bool ClockPaused, bool SessionRunning, string? SessionName);

/// <summary>
/// Pause/resume/toggle the session clock. Mirrors <c>UndercutF1.Console.Api.ControlEndpoints</c>,
/// re-implemented here so this project only depends on <c>UndercutF1.Data</c> (not the console's
/// Spectre.Console/audio/graphics dependencies).
/// </summary>
public static class ControlEndpoints
{
    public static WebApplication MapControlEndpoints(this WebApplication app)
    {
        app.MapGet("/api/control", GetControlState);
        app.MapPost("/api/control", PostControl);
        return app;
    }

    private static IResult GetControlState(SessionInfoProcessor sessionInfo, IDateTimeProvider dateTimeProvider) =>
        Results.Ok(
            new ControlResponse(
                dateTimeProvider.IsPaused,
                sessionInfo.Latest.Name is not null,
                sessionInfo.Latest.Name
            )
        );

    private static IResult PostControl(
        [FromBody] ControlRequest request,
        SessionInfoProcessor sessionInfo,
        IDateTimeProvider dateTimeProvider
    )
    {
        if (sessionInfo.Latest.Name is null)
        {
            return Results.BadRequest(new ApiErrorDto("No session is currently running"));
        }

        switch (request.Operation)
        {
            case ControlOperation.PauseClock when !dateTimeProvider.IsPaused:
            case ControlOperation.ResumeClock when dateTimeProvider.IsPaused:
            case ControlOperation.ToggleClock:
                dateTimeProvider.TogglePause();
                break;
        }

        return GetControlState(sessionInfo, dateTimeProvider);
    }
}
