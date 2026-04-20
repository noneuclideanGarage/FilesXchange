using FilesXchange.API.Helpers;
using FilesXchange.API.Options;
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


var builder = WebApplication.CreateBuilder(args);
builder.ConfigureServices();

var app = builder.Build();

//app.ConfigureMiddleware();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapControllers();

app.Run();
