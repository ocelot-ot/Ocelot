using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot Basic setup
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot(); // single ocelot.json file in read-only mode
builder.Services
    .AddOcelot(builder.Configuration);

// Add your features
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

// Add middlewares aka app.Use*()
var app = builder.Build();

// Serve static files (e.g., logo)
app.UseStaticFiles();

// Add middleware to handle "/" path with custom HTML response
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && context.Request.Method == "GET")
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(await GetWelcomeHtmlAsync());
    }
    else
    {
        await next();
    }
});

await app.UseOcelot();
await app.RunAsync();

static async Task<string> GetWelcomeHtmlAsync()
{
    var htmlFilePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "Hello.html");
    return await File.ReadAllTextAsync(htmlFilePath);
}
