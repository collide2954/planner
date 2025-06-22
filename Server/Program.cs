using Serilog;
using Server.Logging;
using Server.Data;
using Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
LoggingConfigurator.ConfigureLogger();
builder.Host.UseCustomLogging();

// Configure SQLite
builder.Services.AddDbContext<PlannerDbContext>(options =>
    options.UseSqlite("Data Source=planner.db"));
    
// Add AuthService
builder.Services.AddScoped<AuthService>();

try
{
    Log.Information("Starting Planner Server");
    
    var app = builder.Build();

    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
        context.Database.EnsureCreated();
        Log.Information("Database initialized");
    }

    app.MapGet("/health", () => {
        Log.Information("Health check received");
        return Results.Ok(new { status = "healthy" });
    });

    app.MapPost("/auth/register", async (HttpContext context, AuthService authService) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(body);
            var username = json.RootElement.GetProperty("username").GetString();
            var password = json.RootElement.GetProperty("password").GetString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return Results.BadRequest("Username and password are required");

            var result = await authService.RegisterUser(username, password);
            return result ? Results.Ok() : Results.BadRequest("Username already exists");
        }
        catch
        {
            return Results.BadRequest("Invalid request format");
        }
    });

    app.MapPost("/auth/login", async (HttpContext context, AuthService authService) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(body);
            var username = json.RootElement.GetProperty("username").GetString();
            var password = json.RootElement.GetProperty("password").GetString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return Results.BadRequest("Username and password are required");

            var result = await authService.ValidateUser(username, password);
            return result ? Results.Ok() : Results.Unauthorized();
        }
        catch
        {
            return Results.BadRequest("Invalid request format");
        }
    });

    app.Urls.Clear();
    app.Urls.Add("http://localhost:8063");
    Log.Information("Server configured to listen on port 8063");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
