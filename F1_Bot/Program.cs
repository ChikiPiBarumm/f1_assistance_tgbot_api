using F1_Bot.Application;
using F1_Bot.Application.Interfaces;
using F1_Bot.Infrastructure.OpenF1;
using F1_Bot.Presentation.Bot;
using F1_Bot.Presentation.Bot.Handlers;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IOpenF1Client, OpenF1Client>(client =>
{
    client.BaseAddress = new Uri("https://api.openf1.org");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<IUserStateService, UserStateService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IStandingsService, StandingsService>();
builder.Services.AddScoped<IRaceResultsService, RaceResultsService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IRaceDetailsService, RaceDetailsService>();

var botToken = builder.Configuration["TelegramBot:BotToken"];
if (string.IsNullOrWhiteSpace(botToken) || botToken == "YOUR_BOT_TOKEN_HERE")
{
    throw new InvalidOperationException(
        "Telegram bot token is not configured. Please set 'TelegramBot:BotToken' in appsettings.json");
}

builder.Services.AddHttpClient("TelegramBot", client =>
{
    client.BaseAddress = new Uri("https://api.telegram.org");
    client.Timeout = TimeSpan.FromSeconds(90);
})
.ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    MaxConnectionsPerServer = 4,
    ConnectTimeout = TimeSpan.FromSeconds(10),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
});

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("TelegramBot");
    var botOptions = new TelegramBotClientOptions(botToken);
    return new TelegramBotClient(botOptions, httpClient);
});

builder.Services.AddScoped<MessageSender>();
builder.Services.AddScoped<IArgumentParser, ArgumentParser>();
builder.Services.AddScoped<IStartHandler, StartHandler>();
builder.Services.AddScoped<IRaceDetailsHandler, RaceDetailsHandler>();
builder.Services.AddScoped<ICalendarHandler, CalendarHandler>();
builder.Services.AddScoped<IStandingsHandler, StandingsHandler>();
builder.Services.AddScoped<IResultsHandler, ResultsHandler>();
builder.Services.AddScoped<IModeHandler, ModeHandler>();
builder.Services.AddScoped<TelegramBotCommandRouter>();
builder.Services.AddSingleton<PollingService>();
builder.Services.AddSingleton<ITelegramBotService>(sp => 
    sp.GetRequiredService<PollingService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<PollingService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "F1 Assistance Bot & API");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();