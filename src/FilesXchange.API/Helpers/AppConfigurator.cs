using FilesXchange.API.Data;
using FilesXchange.API.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Helpers;

public static class AppConfigurator
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        var appSettingsSection = builder.Configuration.GetSection(AppOptions.SectionName);
        builder.Services.Configure<AppOptions>(appSettingsSection);
        builder.Services.AddOptions<AppOptions>()
            .Bind(appSettingsSection)
            .ValidateDataAnnotations();
        var filesXchangeOptions = 
            appSettingsSection.Get<AppOptions>() ?? new AppOptions();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = filesXchangeOptions.MaxFileSizeBytes;
            options.AllowSynchronousIO = true;
        });
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = filesXchangeOptions.MaxFileSizeBytes;
        });

        builder.Services.AddDbContext<FilesXchangeDbContext>(opts
            => opts.UseSqlite(builder.Configuration.GetConnectionString("Default")));
        builder.Services.AddControllers();
    }
}
