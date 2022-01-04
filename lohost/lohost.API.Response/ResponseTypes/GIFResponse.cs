namespace lohost.API.Response
{
    public class GIFResponse : IResponse
    {
        private byte[] _data;

        public GIFResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "file";
        }

        public string GetContentType()
        {
            
            return "image/gif";
        }

        public object GetContent()
        {
            return _data;
        }
    }
}
