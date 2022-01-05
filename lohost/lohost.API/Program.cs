using lohost.API.Controllers;
using lohost.API.Hubs;
using lohost.Logging;
using lohost.API.Response;

var builder = WebApplication.CreateBuilder(args);

var logger = new Log(Path.Join(Directory.GetCurrentDirectory(), "Logs"), 1);

var localIntegrationHub = new LocalApplicationHub(logger);

var internalURL = builder.Configuration["Hosting:InternalURL"];
var externalURL = builder.Configuration["Hosting:ExternalURL"];

Uri externalDomainURL = new Uri(externalURL);

string externalDomain = externalDomainURL.Authority;

string maxResponseSizeMBStr = builder.Configuration["Hosting:MaxResponseSizeMB"];

int? maxResponseSizeMB = null;

if (!string.IsNullOrEmpty(maxResponseSizeMBStr))
{
    try
    {
        maxResponseSizeMB = int.Parse(maxResponseSizeMBStr);
    }
    catch (Exception ex)
    {
        logger.Error("Error reading MaxResponseSizeMB", ex);

        maxResponseSizeMB = 1; // If there is an error, set it to 1MB in order to stop bandwidth from being smashed
    }
}


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

app.MapHub<LocalApplicationHub>("/ApplicationHub/v0_1");

app.MapGet("{*.}", async (HttpContext httpContext) =>
{
    string urlHost = httpContext.Request.Host.ToString();
    string queryPath = httpContext.Request.Path.ToString();

    if (!queryPath.Contains(".."))
    {
        string[] queryPathParts = queryPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if ((queryPathParts.Length == 0) || !queryPathParts.Last().Contains('.'))
        {
            if (queryPathParts.Length == 0) queryPath = "/index.html";
            else queryPath = $"/{string.Join('/', queryPathParts)}/index.html";
        }

        var applicationId = urlHost.Replace(externalDomain, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('.');

        DocumentResponse? documentResponse;

        try
        {
            // This is a request for a remote application.
            if (!string.IsNullOrEmpty(applicationId))
            {
                LocalApplication localApplication = new LocalApplication(logger, localIntegrationHub, maxResponseSizeMB);

                documentResponse = await localApplication.GetDocument(applicationId, queryPath);
            }
            else
            {
                LohostWebsite lohostWebsite = new LohostWebsite(logger, externalDomain);

                documentResponse = await lohostWebsite.GetDocument(queryPath);
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error retrieving {urlHost}{queryPath}", ex);

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
    }
    else
    {
        return Results.NotFound();
    }
});

app.Run(internalURL);