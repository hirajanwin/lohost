namespace lohost.Client.Models
{
    public class ApplicationData
    {   
        public ApplicationData()
        {
            ApplicationId = GenerateDefaultAddress();

            IsListed = false;

            ApplicationFolder = "App";

            LogsFolder = "Logs";

            ExternalAPI = "https://lohost.io";
        }

        public string ApplicationId { get; set; }

        public string ApplicationKey { get; set; }

        public bool IsListed { get; set; }

        public string Name { get; set; }

        public string[] Tags { get; set; }

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

        private string GenerateDefaultAddress()
        {
            char[] chars = new char[] { 
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 
                'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 
                'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            Random r = new Random();

            string address = string.Empty;

            for (int i = 0; i<5; i++) address += chars[r.Next(0, chars.Length)];

            return address;
        }
    }
}