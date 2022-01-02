namespace lohost.Client.Models
{
    public class ApplicationData
    {   
        public ApplicationData()
        {
            ApplicationId = System.Guid.NewGuid().ToString();

            ApplicationFolder = "App";

            LogsFolder = "Logs";

            ExternalAPI = "https://lohost.io";
        }

        public string ApplicationId { get; set; }

        public string ApplicationKey { get; set; }

        public string ExecutingLocation { get; }

        public string ApplicationFolder { get; set; }

        public string LogsFolder { get; set; }

        public string ExternalAPI { get; set; }

        public string[] ApplicationPaths { get; set; }

        public string GetRegisteredAddress()
        {
            if (ExternalAPI.StartsWith("https://")) return $"https://{ApplicationId}.{ExternalAPI.Substring("https://".Length)}";
            else return $"http://{ApplicationId}.{ExternalAPI.Substring("http://".Length)}";

        }

        public string GetExecutingLocation()
        {
            if (string.IsNullOrEmpty(ExecutingLocation))
            {
                return Directory.GetCurrentDirectory();
            }
            else
            {
                return ExecutingLocation;
            }
        }

        public string GetApplicationFolder()
        {
            string applicationFolder;

            try
            {
                Path.GetFullPath(ApplicationFolder);

                applicationFolder = ApplicationFolder;
            }
            catch (Exception)
            {
                applicationFolder = Path.Join(GetExecutingLocation(), ApplicationFolder);
            }

            if (!Directory.Exists(applicationFolder)) Directory.CreateDirectory(applicationFolder);

            return applicationFolder;
        }

        public string GetLogsFolder()
        {
            string logsFolder;

            try
            {
                Path.GetFullPath(LogsFolder);

                logsFolder = LogsFolder;
            }
            catch (Exception)
            {
                logsFolder = Path.Join(GetExecutingLocation(), LogsFolder);
            }

            if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);

            return logsFolder;
        }
    }
}