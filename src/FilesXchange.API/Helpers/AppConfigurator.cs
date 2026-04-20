using FilesXchange.API.Data;
using FilesXchange.API.Health;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Options;
using FilesXchange.API.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace FilesXchange.API.Helpers;

public static class AppConfigurator
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog();
        var appSettingsSection = builder.Configuration.GetSection(AppOptions.SectionName);
        builder.Services.Configure<AppOptions>(appSettingsSection);
        builder.Services.AddOptions<AppOptions>()
            .Bind(appSettingsSection)
            .ValidateDataAnnotations();
        var filesXchangeOptions = appSettingsSection.Get<AppOptions>() ?? new AppOptions();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = filesXchangeOptions.MaxFileSizeBytes;
            options.AllowSynchronousIO = true;
        });
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = filesXchangeOptions.MaxFileSizeBytes;
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddDbContext<FilesXchangeDbContext>(opts
            => opts.UseSqlite(builder.Configuration.GetConnectionString("Default")));
        builder.Services.AddScoped<ICacheService, CacheService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IFileExchangeService, FileExchangeService>();
        builder.Services.AddScoped<IExpiredFileCleanupService, ExpiredFileCleanupService>();
        builder.Services.AddHostedService<ExpiredFileCleanupHostedService>();
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("sqlite", failureStatus: HealthStatus.Unhealthy);
        builder.Services.AddControllers();
    }
}
