using FilesXchange.API.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.ConfigureMiddleware();


app.Run();
