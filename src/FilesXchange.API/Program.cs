using System.Text.Json;
using FilesXchange.API.Contracts;
using FilesXchange.API.Data;
using FilesXchange.API.Helpers;
using FilesXchange.API.Helpers.Middleware;
using FilesXchange.API.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var bootstrapConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var bootstrapOptions = bootstrapConfiguration.GetSection(AppOptions.SectionName).Get<AppOptions>()
    ?? new AppOptions();
var bootstrapLogDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), bootstrapOptions.LogDirectory));
Directory.CreateDirectory(bootstrapLogDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(bootstrapLogDirectory, "filesxchange-.log"),
        rollingInterval: RollingInterval.Day,
        shared: true)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureServices();

    var app = builder.Build();
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<FilesXchangeDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    app.UseMiddleware<ExceptionLoggingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseStatusCodePages(async statusCodeContext =>
    {
        var response = statusCodeContext.HttpContext.Response;
        if (response.HasStarted)
        {
            return;
        }

        ErrorEnvelope? payload = response.StatusCode switch
        {
            StatusCodes.Status400BadRequest => new ErrorEnvelope(new ApiError("bad_request", "The request is invalid.")),
            StatusCodes.Status413PayloadTooLarge => new ErrorEnvelope(new ApiError("payload_too_large", "Total upload size exceeds the configured limit.")),
            _ => null
        };

        if (payload is null)
        {
            return;
        }

        response.ContentType = "application/json";
        await response.WriteAsync(JsonSerializer.Serialize(payload));
    });

    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.ToDictionary(
                    static entry => entry.Key,
                    static entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description
                    })
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    });

    _ = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
    _ = builder.Configuration.GetConnectionString("Default");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "[EXCEPTION] Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

