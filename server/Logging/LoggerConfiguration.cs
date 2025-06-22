using Serilog;
using Microsoft.Extensions.Hosting;

namespace server.Logging;

public static class LoggingConfigurator
{
    public static void ConfigureLogger()
    {
        Log.Logger = new Serilog.LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/planner-.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .MinimumLevel.Information()
            .CreateLogger();
    }

    public static IHostBuilder UseCustomLogging(this IHostBuilder builder)
    {
        return builder.UseSerilog();
    }
}
