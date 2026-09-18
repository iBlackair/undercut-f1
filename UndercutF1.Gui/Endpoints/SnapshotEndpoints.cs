namespace UndercutF1.Gui;

public static class SnapshotEndpoints
{
    public static WebApplication MapSnapshotEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/snapshot",
            (TimingTowerSnapshotBuilder builder) => Results.Ok(builder.Build())
        );

        return app;
    }
}
