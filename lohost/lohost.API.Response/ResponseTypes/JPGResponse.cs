namespace lohost.API.Response
{
    public class JPGResponse : IResponse
    {
        private byte[] _data;

        public JPGResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "file";
        }

        public string GetContentType()
        {
            
            return "image/jpeg";
        }

        public object GetContent()
        {
            return _data;
        }
    }
}
