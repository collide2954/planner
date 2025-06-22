using Serilog;
using server.Logging;

var builder = WebApplication.CreateBuilder(args);

LoggingConfigurator.ConfigureLogger();
builder.Host.UseCustomLogging();

try
{
    Log.Information("Starting Planner server");
    
    var app = builder.Build();

    app.MapGet("/health", () => {
        Log.Information("Health check received");
        return Results.Ok(new { status = "healthy" });
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
