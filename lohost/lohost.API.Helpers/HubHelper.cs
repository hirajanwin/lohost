using lohost.API.Models;

namespace lohost.API.Helpers
{
    public static class HubHelper
    {
        public static string GetConnectionId(Dictionary<string, ApplicationConnection> applicationConnection, string applicationId, string document)
        {
            List<ApplicationConnection> applicationConnections = applicationConnection.Where(ac => ac.Key == applicationId || ac.Key.StartsWith($"{applicationId}:")).Select(ac => ac.Value).ToList();

            if (applicationConnections.Count > 1)
            {
                ApplicationConnection? allApplicationConnection = applicationConnections.FirstOrDefault(ac => ac.Path == "*");
                List<ApplicationConnection> orderedConnections = applicationConnections.Where(ac => ac.Path != "*").OrderByDescending(ac => ac.Path.Length).ToList();

                for (int i = 0; i < orderedConnections.Count; i++)
                {
                    if (document.ToLower().TrimStart('/').StartsWith(orderedConnections[i].Path)) return orderedConnections[i].ConnectionId;
                }

                if (allApplicationConnection != null) return allApplicationConnection.ConnectionId;
                else return null;
            }
            else if (applicationConnection.Count == 1)
            {
                if (applicationConnections[0].Path.Equals("*") || document.ToLower().TrimStart('/').StartsWith(applicationConnections[0].Path)) return applicationConnections[0].ConnectionId;
                else return null;
            }
            else
            {
                return null;
            }
        }
        public static string GetAConnectionId(Dictionary<string, ApplicationConnection> applicationConnection, string applicationId)
        {
            List<ApplicationConnection> applicationConnections = applicationConnection.Where(ac => ac.Key == applicationId || ac.Key.StartsWith($"{applicationId}:")).Select(ac => ac.Value).ToList();

            if (applicationConnections.Count > 0)
            {
                ApplicationConnection? allApplicationConnection = applicationConnections.FirstOrDefault(ac => ac.Path == "*");

                if (allApplicationConnection != null)
                {
                    return allApplicationConnection.ConnectionId;
                }
                else
                {
                    return applicationConnections[0].ConnectionId;
                }
            }
            else
            {
                return null;
            }
        }
    }
}