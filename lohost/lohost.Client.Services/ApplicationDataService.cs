using lohost.Client.Models;
using Newtonsoft.Json;

namespace lohost.Client.Services
{
    public class ApplicationDataService
    {
        private readonly string _configLocation;

        public ApplicationDataService()
        {
            _configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");
        }

        public async Task<ApplicationData> GetApplicationData()
        {
            ApplicationData applicationData;

            if (File.Exists(_configLocation))
            {
                string jsonConfig = File.ReadAllText(_configLocation);

                applicationData = JsonConvert.DeserializeObject<ApplicationData>(jsonConfig);
            }
            else
            {
                applicationData = new ApplicationData();

                await Save(applicationData);
            }

            return applicationData;
        }

        private async Task Save(ApplicationData applicationData)
        {
            string jsonConfig = JsonConvert.SerializeObject(applicationData);

            await File.WriteAllTextAsync(_configLocation, jsonConfig);
        }
    }
}