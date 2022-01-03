using System;
using System.Collections.Generic;
using System.Linq;

namespace lohost.API.Models
{
    public class ApplicationConnection
    {
        public string Id { get; set; }

        public string Key { get; set; }

        public bool IsListed { get; set; } = false;

        public string Name { get; set; }

        public string[] Tags { get; set; }

        public List<ApplicationRoute> ApplicationRoutes { get; set; } = new List<ApplicationRoute>();

        public string GetAConnectionId()
        {
            if (ApplicationRoutes.Count > 0)
            {
                ApplicationRoute? allApplicationRoute = ApplicationRoutes.FirstOrDefault(ac => ac.Path == "*");

                if (allApplicationRoute != null)
                {
                    return allApplicationRoute.ConnectionId;
                }
                else
                {
                    return ApplicationRoutes[0].ConnectionId;
                }
            }
            else
            {
                return null;
            }
        }

        public string GetConnectionId(string document)
        {
            if (ApplicationRoutes.Count > 1)
            {
                ApplicationRoute? allApplicationConnection = ApplicationRoutes.FirstOrDefault(ac => ac.Path == "*");
                List<ApplicationRoute> orderedConnections = ApplicationRoutes.Where(ac => ac.Path != "*").ToList();

                for (int i = 0; i < orderedConnections.Count; i++)
                {
                    if (document.ToLower().TrimStart('/').StartsWith(orderedConnections[i].Path)) return orderedConnections[i].ConnectionId;
                }

                if (allApplicationConnection != null)
                {
                    return allApplicationConnection.ConnectionId;
                }
                else
                {
                    return null;
                }
            }
            else if (ApplicationRoutes.Count == 1)
            {
                if (ApplicationRoutes[0].Path.Equals("*") || document.ToLower().TrimStart('/').StartsWith(ApplicationRoutes[0].Path))
                {
                    return ApplicationRoutes[0].ConnectionId;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void AddApplicationRoute(string connectionId, string path)
        {
            ApplicationRoutes.Add(new ApplicationRoute()
            {
                ConnectionId = connectionId,
                Path = path
            });

            ApplicationRoutes = ApplicationRoutes.OrderByDescending(ac => ac.Path.Length).ToList();
        }

        public bool ApplicationRouteExists(string path)
        {
            return ApplicationRoutes.Any(ar => ar.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasApplicationRouteConnection(string connectionId)
        {
            return ApplicationRoutes.Any(ar => ar.ConnectionId.Equals(connectionId, StringComparison.Ordinal));
        }

        public bool RemoveApplicationRouteConnection(string connectionId)
        {
            ApplicationRoutes.RemoveAll(ar => ar.ConnectionId.Equals(connectionId, StringComparison.Ordinal));

            return (ApplicationRoutes.Count() == 0);
        }
    }
}
