using F1_Bot.Services;
using F1_Bot.Infrastructure.OpenF1;
using F1_Bot.Services.Bot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IOpenF1Client, OpenF1Client>(client =>
{
    client.BaseAddress = new Uri("https://api.openf1.org");
});

builder.Services.AddScoped<ICalendarService, OpenF1CalendarService>();
builder.Services.AddScoped<IStandingsService, OpenF1StandingsService>();
builder.Services.AddScoped<IRaceResultsService, OpenF1RaceResultsService>();

var botToken = builder.Configuration["TelegramBot:BotToken"];
if (string.IsNullOrWhiteSpace(botToken) || botToken == "YOUR_BOT_TOKEN_HERE")
{
    throw new InvalidOperationException(
        "Telegram bot token is not configured. Please set 'TelegramBot:BotToken' in appsettings.json");
}

builder.Services.AddHttpClient("TelegramBot", client =>
{
    client.BaseAddress = new Uri("https://api.telegram.org");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromSeconds(1),
    MaxConnectionsPerServer = 1,
    ConnectTimeout = TimeSpan.FromSeconds(10),
    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(1)
})
.SetHandlerLifetime(TimeSpan.FromSeconds(1));

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var botToken = builder.Configuration["TelegramBot:BotToken"];
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("TelegramBot");
    var botOptions = new TelegramBotClientOptions(botToken);
    return new TelegramBotClient(botOptions, httpClient);
});

builder.Services.AddScoped<TelegramBotMessageSender>();
builder.Services.AddScoped<TelegramBotCommandRouter>();
builder.Services.AddSingleton<TelegramBotPollingService>();
builder.Services.AddSingleton<ITelegramBotService>(sp => 
    sp.GetRequiredService<TelegramBotPollingService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelegramBotPollingService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "F1 Assistance Bot & API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapGet("/api/races", async (ICalendarService calendarService) =>
{
    var races = await calendarService.GetRacesAsync();
    return Results.Ok(races);
});

app.MapGet("/api/races/next", async (ICalendarService calendarService) =>
{
    var nextRace = await calendarService.GetNextRaceAsync();

    if (nextRace is null)
    {
        return Results.NotFound(new { message = "No upcoming race found" });
    }

    return Results.Ok(nextRace);
});

app.MapGet("/api/standings/drivers", async (IStandingsService standingsService) =>
{
    var standings = await standingsService.GetDriverStandingsAsync();
    return Results.Ok(standings);
});

app.MapGet("/api/standings/teams", async (IStandingsService standingsService) =>
{
    var standings = await standingsService.GetTeamStandingsAsync();
    return Results.Ok(standings);
});

app.MapGet("/api/races/last/results", async (IRaceResultsService raceResultsService) =>
{
    var results = await raceResultsService.GetLastRaceResultsAsync();

    if (results.Count == 0)
    {
        return Results.NotFound(new { message = "No race results found for the latest race" });
    }

    return Results.Ok(results);
});

app.Run();