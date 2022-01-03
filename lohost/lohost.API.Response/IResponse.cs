namespace lohost.API.Response
{
    public interface IResponse
    {
        public string GetResultType();

        public string GetContentType();

        public object GetContent();
    }
}
