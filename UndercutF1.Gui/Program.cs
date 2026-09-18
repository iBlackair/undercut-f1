using System.Text.Json.Serialization;
using InMemLogger;
using UndercutF1.Data;
using UndercutF1.Gui;

var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration.AddJsonFile(GuiConfig.ConfigFilePath, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("UNDERCUTF1_");

builder.Logging.AddInMemory();

builder.Services.AddLiveTiming(builder.Configuration).AddSingleton<TimingTowerSnapshotBuilder>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
);

builder.WebHost.UseUrls("http://localhost:61938");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapSnapshotEndpoints();
app.MapControlEndpoints();
app.MapSessionEndpoints();
app.MapRaceControlPageEndpoints();
app.MapTyreStintPageEndpoints();
app.MapChampionshipPageEndpoints();
app.MapDriverTrackerPageEndpoints();
app.MapTimingHistoryPageEndpoints();
app.MapTeamRadioPageEndpoints();
app.MapLogsPageEndpoints();
app.MapInfoPageEndpoints();
app.MapAccountPageEndpoints();

await app.RunAsync();

/// <summary>
/// Top-level Program partial, exposed so it can be used as a generic logging category
/// (<c>ILogger&lt;Program&gt;</c>) from the endpoint classes in this assembly.
/// </summary>
public partial class Program { }
