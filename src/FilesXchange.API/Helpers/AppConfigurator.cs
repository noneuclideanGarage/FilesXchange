using FilesXchange.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Helpers;

public static class AppConfigurator
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<FilesXchangeDbContext>(opts =>
            {
                string dbConfigurationKey = "DatabaseName";
                var dbName = builder.Configuration[dbConfigurationKey];
                var pathForDatabase = Environment
                    .GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var DbPath = Path.Join(pathForDatabase, dbName);
                opts.UseSqlite($"Data Source={DbPath}");
            });
    }
}
