namespace lohost.API.Models
{
    public class ApplicationConnection
    {
        public string ConnectionId { get; set; }

        public string Key { get; set; }

        public bool IsListed { get; set; } = false;

        public string Name { get; set; }

        public string[] Tags { get; set; }

        public string Path { get; set; }
    }
}
