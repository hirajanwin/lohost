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

        public string ExecutingLocation { get; }

        public string ApplicationFolder { get; set; }

        public string LogsFolder { get; set; }

        public string ExternalAPI { get; set; }

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
            try
            {
                Path.GetFullPath(ApplicationFolder);

                return ApplicationFolder;
            }
            catch (Exception)
            {
                return Path.Join(GetExecutingLocation(), ApplicationFolder);
            }
        }

        public string GetLogsFolder()
        {
            try
            {
                Path.GetFullPath(LogsFolder);

                return LogsFolder;
            }
            catch (Exception)
            {
                return Path.Join(GetExecutingLocation(), LogsFolder);
            }
        }
    }
}