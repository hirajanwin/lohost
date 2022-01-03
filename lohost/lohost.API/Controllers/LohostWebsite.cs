using lohost.API.Hubs;
using lohost.API.Logging;
using lohost.API.Models;
using lohost.API.Response;
using Newtonsoft.Json.Linq;
using System.Text;

namespace lohost.API.Controllers
{
    public class LohostWebsite
    {
        private readonly SystemLogging _systemLogging;

        private readonly string _hostingLocation;

        public LohostWebsite(SystemLogging systemLogging, string hostingLocation)
        {
            _systemLogging = systemLogging;

            _hostingLocation = hostingLocation;
        }

        public async Task<DocumentResponse> GetDocument(string document)
        {
            if (document.Trim('/').Equals("apps.json"))
            {
                JArray apps = new JArray();

                foreach (ListedApplication app in LocalApplicationHub.GetAllListedApplications())
                {
                    JArray tags = new JArray();
                    foreach (string tag in app.Tags) tags.Add(tag);

                    apps.Add(new JObject()
                    {
                        { "url", $"https://{app.Id}.{_hostingLocation}" },
                        { "name", app.Name },
                        { "tags", tags }
                    });
                }

                return new DocumentResponse()
                {
                    DocumentPath = document,
                    DocumentData = Encoding.ASCII.GetBytes(apps.ToString())
                };
            }
            else
            {
                string websitePath = Path.Join(Directory.GetCurrentDirectory(), "website", document.Trim('/').Replace('/', '\\'));

                _systemLogging.Debug($"Local website path: {websitePath}");

                if (File.Exists(websitePath))
                {
                    return new DocumentResponse()
                    {
                        DocumentPath = document,
                        DocumentData = File.ReadAllBytes(websitePath)
                    };
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
