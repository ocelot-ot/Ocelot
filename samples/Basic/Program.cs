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
        await context.Response.WriteAsync(GetWelcomeHtml());
    }
    else
    {
        await next();
    }
});

await app.UseOcelot();
await app.RunAsync();

static string GetWelcomeHtml()
{
    return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Ocelot Gateway</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
        }
        .container {
            text-align: center;
            background: white;
            padding: 60px 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1);
            max-width: 600px;
            animation: slideIn 0.6s ease-out;
        }
        @keyframes slideIn {
            from {
                opacity: 0;
                transform: translateY(-20px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        .logo {
            max-width: 200px;
            height: auto;
            margin-bottom: 30px;
            animation: float 3s ease-in-out infinite;
        }
        @keyframes float {
            0%, 100% {
                transform: translateY(0px);
            }
            50% {
                transform: translateY(-10px);
            }
        }
        h1 {
            color: #333;
            font-size: 2.5em;
            margin-bottom: 15px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }
        .subtitle {
            color: #666;
            font-size: 1.1em;
            margin-bottom: 30px;
            line-height: 1.6;
        }
        .welcome-text {
            color: #555;
            font-size: 1em;
            margin: 20px 0;
            line-height: 1.8;
        }
        .features {
            display: flex;
            justify-content: space-around;
            margin-top: 40px;
            flex-wrap: wrap;
        }
        .feature {
            flex: 1;
            min-width: 150px;
            margin: 10px;
            padding: 15px;
            border-left: 4px solid #667eea;
        }
        .feature-title {
            color: #667eea;
            font-weight: bold;
            margin-bottom: 5px;
        }
        .feature-desc {
            color: #999;
            font-size: 0.9em;
        }
        .cta {
            margin-top: 40px;
        }
        .cta p {
            color: #777;
            font-size: 0.95em;
            margin-bottom: 15px;
        }
        .routes-list {
            background: #f5f5f5;
            border-radius: 5px;
            padding: 20px;
            text-align: left;
            margin: 20px 0;
            font-family: 'Courier New', monospace;
            font-size: 0.9em;
            color: #333;
        }
        .route-item {
            padding: 8px 0;
            border-bottom: 1px solid #e0e0e0;
        }
        .route-item:last-child {
            border-bottom: none;
        }
        .method {
            color: #667eea;
            font-weight: bold;
            display: inline-block;
            width: 45px;
        }
        .path {
            color: #764ba2;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <img src=""/ocelot_logo.png"" alt=""Ocelot Logo"" class=""logo"">
        <h1>Hello from Ocelot</h1>
        <p class=""subtitle"">Your API Gateway is running!</p>
        
        <div class=""welcome-text"">
            <p>Welcome to the <strong>Ocelot Basic Sample</strong> application.</p>
            <p>Ocelot is a .NET API Gateway built on top of .NET core and consists of a series of nuget packages.</p>
        </div>

        <div class=""features"">
            <div class=""feature"">
                <div class=""feature-title"">🚀 Fast</div>
                <div class=""feature-desc"">High-performance routing</div>
            </div>
            <div class=""feature"">
                <div class=""feature-title"">🔒 Secure</div>
                <div class=""feature-desc"">Built-in security features</div>
            </div>
            <div class=""feature"">
                <div class=""feature-title"">⚙️ Configurable</div>
                <div class=""feature-desc"">Easy JSON configuration</div>
            </div>
        </div>

        <div class=""cta"">
            <p><strong>Available Routes in Ocelot Configuration:</strong></p>
            <div class=""routes-list"">
                <div class=""route-item""><span class=""method"">GET</span><span class=""path"">/ocelot/posts/{id}</span></div>
                <div class=""route-item""><span class=""method"">GET</span><span class=""path"">/ocelot/docs/{everything}</span></div>
                <div class=""route-item""><span class=""method"">GET</span><span class=""path"">/_/{BFF}</span></div>
            </div>
            <p style=""margin-top: 20px; color: #999; font-size: 0.9em;"">Check the <code>ocelot.json</code> file to see the full configuration and add your own routes.</p>
        </div>

        <p style=""margin-top: 30px; color: #999; font-size: 0.85em;"">
            📖 Learn more at <a href=""https://github.com/ThreeMammals/Ocelot"" style=""color: #667eea; text-decoration: none;"">github.com/ThreeMammals/Ocelot</a>
        </p>
    </div>
</body>
</html>";
}
