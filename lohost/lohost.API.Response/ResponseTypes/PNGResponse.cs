namespace lohost.API.Response
{
    public class PNGResponse : IResponse
    {
        private byte[] _data;

        public PNGResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "file";
        }

        public string GetContentType()
        {
            
            return "image/png";
        }

        public object GetContent()
        {
            return _data;
        }
    }
}
