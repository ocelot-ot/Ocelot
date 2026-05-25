using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Samples.WebSocket;
using Ocelot.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddOcelot(builder.Configuration);

if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();

var app = builder.Build();

// Serve static files (e.g., logo)
app.UseWebSockets();
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

var wsPipeline = new OcelotPipelineConfiguration
{
    WebSocketsProxyMiddleware = (context, next) =>
    {
        Task Next(HttpContext ctx) => next();
        var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
        var factory = context.RequestServices.GetRequiredService<IWebSocketsFactory>();
        var middleware = new CustomWebSocketsProxyMiddleware(loggerFactory, Next, factory);
        return middleware.Invoke(context);
    },
};
await app.UseOcelot(wsPipeline);
await app.RunAsync();

static async Task<string> GetWelcomeHtmlAsync()
{
    var htmlFilePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "welcome.html");
    return await File.ReadAllTextAsync(htmlFilePath);
}
