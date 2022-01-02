using lohost.API.Controllers;
using lohost.API.Hubs;
using lohost.API.Logging;
using lohost.API.Response;

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
builder.Services.AddSingleton<LocalApplicationHub>(t => localIntegrationHub);

// Build the app and configure the required services
var app = builder.Build();

app.UseHttpsRedirection();

app.MapHub<LocalApplicationHub>("/ApplicationHub");

app.MapGet("{*.}", async (HttpContext httpContext) =>
{
    string urlHost = httpContext.Request.Host.ToString();
    string queryPath = httpContext.Request.Path.ToString();

    if (string.IsNullOrEmpty(queryPath) || !queryPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last().Contains('.'))
    {
        if (string.IsNullOrEmpty(queryPath)) queryPath = "index.html";
        else queryPath = $"{queryPath.TrimEnd('/')}/index.html";
    }

    var applicationId = urlHost.Replace(hostingLocation, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('.');

    DocumentResponse documentResponse;

    // This is a request for a remote application.
    if (!string.IsNullOrEmpty(applicationId))
    {
        LocalApplication localApplication = new LocalApplication(systemLogging, localIntegrationHub);

        documentResponse = await localApplication.GetDocument(applicationId, queryPath);
    }
    else
    {
        documentResponse = null;
    }

    if ((documentResponse != null) && (documentResponse.DocumentFound()))
    {
        IResponse response = documentResponse.GetResponse();

        switch (response.GetResultType())
        {
            case "text":
                return Results.Text((string)response.GetContent(), response.GetContentType());
            case "file":
                return Results.File((byte[])response.GetContent(), response.GetContentType());
            default:
                return Results.NoContent();
        }
    }
    else
    {
        return Results.NotFound();
    }
});

app.Run();