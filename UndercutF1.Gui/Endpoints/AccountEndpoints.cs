using System.Text.Json.Nodes;
using UndercutF1.Data;

namespace UndercutF1.Gui;

public sealed record AccountPageDto(
    string Status,
    string? SubscriptionStatus,
    string? SubscribedProduct,
    string? Expiry,
    string ConfigFilePath
);

public sealed record SetTokenRequest(string Token);

public static class AccountEndpoints
{
    public static WebApplication MapAccountPageEndpoints(this WebApplication app)
    {
        app.MapGet("/api/account", (Formula1Account account) => Results.Ok(ToDto(account)));

        app.MapPost(
            "/api/account/token",
            async (SetTokenRequest request, Formula1Account account, ILogger<Program> logger) =>
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return Results.BadRequest(new ApiErrorDto("Token cannot be empty"));
                }

                try
                {
                    await PersistTokenAsync(request.Token);
                    // Give immediate feedback rather than waiting for the config file watcher to notice the change.
                    account.Refresh(request.Token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist Formula 1 access token");
                    return Results.Problem(
                        title: "Failed to save token",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }

                return Results.Ok(ToDto(account));
            }
        );

        app.MapPost(
            "/api/account/logout",
            async (Formula1Account account) =>
            {
                await PersistTokenAsync(null);
                account.Refresh(null);
                return Results.Ok(ToDto(account));
            }
        );

        return app;
    }

    private static AccountPageDto ToDto(Formula1Account account) =>
        new(
            account.IsAuthenticated.ToString(),
            account.Payload?.SubscriptionStatus,
            account.Payload?.SubscribedProduct,
            account.Payload?.Expiry.ToString("yyyy-MM-dd"),
            GuiConfig.ConfigFilePath
        );

    private static async Task PersistTokenAsync(string? token)
    {
        var path = GuiConfig.ConfigFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var root =
            File.Exists(path) ? JsonNode.Parse(await File.ReadAllTextAsync(path)) as JsonObject
            : new JsonObject();
        root ??= new JsonObject();

        if (token is null)
        {
            root.Remove("formula1AccessToken");
        }
        else
        {
            root["formula1AccessToken"] = token;
        }

        await File.WriteAllTextAsync(
            path,
            root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
        );
    }
}
