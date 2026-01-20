using F1_Bot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// This sets up OpenAPI (the machine-readable API description).
builder.Services.AddOpenApi();

// Register our F1 services
builder.Services.AddScoped<ICalendarService, FakeCalendarService>();
builder.Services.AddScoped<IStandingsService, FakeStandingsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Expose the OpenAPI document as JSON at /openapi/v1.json (by default)
    app.MapOpenApi();

    // Enable Swagger UI, which reads the OpenAPI document and shows a UI
    app.UseSwaggerUI(options =>
    {
        // Tell Swagger UI where the OpenAPI JSON is and give it a name
        options.SwaggerEndpoint("/openapi/v1.json", "F1 Assistance Bot & API v1");

        // URL path where Swagger UI will be available: /swagger
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/api/health", () =>
{
    return new { status = "ok" };
});

// Get all races
app.MapGet("/api/races", async (ICalendarService calendarService) =>
{
    var races = await calendarService.GetRacesAsync();
    return Results.Ok(races);
});

// Get next race
app.MapGet("/api/races/next", async (ICalendarService calendarService) =>
{
    var nextRace = await calendarService.GetNextRaceAsync();

    if (nextRace is null)
    {
        return Results.NotFound(new { message = "No upcoming race found" });
    }

    return Results.Ok(nextRace);
});

// Get driver standings
app.MapGet("/api/standings/drivers", async (IStandingsService standingsService) =>
{
    var standings = await standingsService.GetDriverStandingsAsync();
    return Results.Ok(standings);
});

// Get team standings
app.MapGet("/api/standings/teams", async (IStandingsService standingsService) =>
{
    var standings = await standingsService.GetTeamStandingsAsync();
    return Results.Ok(standings);
});

app.Run();