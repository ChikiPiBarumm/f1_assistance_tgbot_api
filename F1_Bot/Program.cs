using F1_Bot.Services;
using F1_Bot.Infrastructure.OpenF1;
using F1_Bot.Presentation.Bot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IOpenF1Client, OpenF1Client>(client =>
{
    client.BaseAddress = new Uri("https://api.openf1.org");
});

builder.Services.AddSingleton<IUserStateService, OpenF1UserStateService>();
builder.Services.AddScoped<ICalendarService, OpenF1CalendarService>();
builder.Services.AddScoped<IStandingsService, OpenF1StandingsService>();
builder.Services.AddScoped<IRaceResultsService, OpenF1RaceResultsService>();
builder.Services.AddScoped<ISessionService, OpenF1SessionService>();
builder.Services.AddScoped<IRaceDetailsService, OpenF1RaceDetailsService>();

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
    PooledConnectionLifetime = TimeSpan.FromSeconds(1),
    MaxConnectionsPerServer = 1,
    ConnectTimeout = TimeSpan.FromSeconds(10),
    PooledConnectionIdleTimeout = TimeSpan.FromSeconds(1)
})
.SetHandlerLifetime(TimeSpan.FromSeconds(1));

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var botToken = builder.Configuration["TelegramBot:BotToken"];
    if (string.IsNullOrWhiteSpace(botToken))
    {
        throw new InvalidOperationException("Telegram bot token is not configured.");
    }
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

app.MapControllers();

app.Run();