using System;

namespace FilesXchange.API.Helpers;

public static class AppMiddleware
{
    public static void ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        app.MapControllers();
        // app.UseHttpsRedirection();
    }
}
