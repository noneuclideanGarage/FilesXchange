using FilesXchange.API.Data;
using FilesXchange.API.Options;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Helpers;

public static class AppConfigurator
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {   
        var appSettingsSection = builder.Configuration.GetSection(AppOptions.SectionName);
        builder.Services.Configure<AppOptions>(appSettingsSection);

        builder.Services.AddDbContext<FilesXchangeDbContext>(opts 
            => opts.UseSqlite(builder.Configuration.GetConnectionString("Default")));
        builder.Services.AddControllers();
    }
}
