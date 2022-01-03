using lohost.API.Controllers;
using lohost.API.Hubs;
using lohost.API.Logging;
using lohost.API.Models;
using lohost.API.Response;
using Newtonsoft.Json.Linq;
using System.Text;

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

    string[] queryPathParts = queryPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

    if ((queryPathParts.Length == 0) || !queryPathParts.Last().Contains('.'))
    {
        if (queryPathParts.Length == 0) queryPath = "/index.html";
        else queryPath = $"/{string.Join('/', queryPathParts)}/index.html";
    }

    var applicationId = urlHost.Replace(hostingLocation, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('.');

    DocumentResponse? documentResponse;

    try
    {
        // This is a request for a remote application.
        if (!string.IsNullOrEmpty(applicationId))
        {
            LocalApplication localApplication = new LocalApplication(systemLogging, localIntegrationHub);

            documentResponse = await localApplication.GetDocument(applicationId, queryPath);
        }
        else
        {
            if (queryPath.Trim('/').Equals("apps.json"))
            {
                JArray apps = new JArray();

                foreach (ListingApplication app in LocalApplicationHub.GetAllListedApplications())
                {
                    JArray tags = new JArray();
                    foreach (string tag in app.Tags) tags.Add(tag);

                    apps.Add(new JObject()
                    {
                        { "name", app.Name },
                        { "tags", tags }
                    });
                }

                documentResponse = new DocumentResponse()
                {
                    DocumentPath = queryPath,
                    DocumentData = Encoding.ASCII.GetBytes(apps.ToString())
                };
            }
            else
            {
                string websitePath = Path.Join(Directory.GetCurrentDirectory(), "website", queryPath.Trim('/').Replace('/', '\\'));

                systemLogging.Debug($"Local website path: {websitePath}");

                if (File.Exists(websitePath))
                {
                    documentResponse = new DocumentResponse()
                    {
                        DocumentPath = queryPath,
                        DocumentData = File.ReadAllBytes(websitePath)
                    };
                }
                else
                {
                    documentResponse = null;
                }
            }
        }
    }
    catch (Exception ex)
    {
        systemLogging.Error($"Error retrieving {urlHost}{queryPath}", ex);

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