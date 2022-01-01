namespace lohost.API.Response
{
    public interface IResponse
    {
        public string GetContentType();

        public object GetContent();
    }
}
