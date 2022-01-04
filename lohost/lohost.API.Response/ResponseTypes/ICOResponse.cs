namespace lohost.API.Response
{
    public class ICOResponse : IResponse
    {
        private byte[] _data;

        public ICOResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "file";
        }

        public string GetContentType()
        {
            
            return "image/x-icon";
        }

        public object GetContent()
        {
            return _data;
        }
    }
}
