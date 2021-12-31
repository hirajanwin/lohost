using lohost.API.Controllers;
using lohost.API.Hubs;
using lohost.Logging;

var builder = WebApplication.CreateBuilder(args);

var hostingLocation = builder.Configuration["Hosting:Location"];

var systemLogging = new SystemLogging();
var localIntegrationHub = new LocalApplicationHub(systemLogging);

// Might as well open it up
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Add SignalR
builder.Services.AddSignalR(configure => { configure.MaximumReceiveMessageSize = null; });

// Build the app and configure the required services
var app = builder.Build();

app.UseHttpsRedirection();

app.MapHub<LocalApplicationHub>("/ApplicationHub");

app.MapGet("{*.}", async (HttpContext httpContext) =>
{
    var urlHost = httpContext.Request.Host.ToString();
    var queryPath = httpContext.Request.Path.ToString();

    var applicationId = urlHost.Replace(hostingLocation, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('.');

    // This is a request for a remote application.
    if (!string.IsNullOrEmpty(applicationId))
    {
        LocalApplication localApplication = new LocalApplication(systemLogging, localIntegrationHub);

        await localApplication.GetDocument(applicationId, queryPath);
    }
    else
    {
        Console.WriteLine("local");
    }
});

app.Run();