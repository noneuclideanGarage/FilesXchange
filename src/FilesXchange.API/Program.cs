using FilesXchange.API.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.ConfigureMiddleware();

var apiEndpoints = app.MapGroup("/api");

apiEndpoints.MapPost("/upload",
    async (IList<IFormFile> files, CancellationToken ctk) =>
{
    //TODO: Upload method
});

apiEndpoints.MapGet("/download/{token}",
    async (string token, CancellationToken ctk) =>
{
    //TODO: Download method
});



app.Run();
