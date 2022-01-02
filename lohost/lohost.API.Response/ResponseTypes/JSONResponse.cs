using System.Text;

namespace lohost.API.Response
{
    public class JSONResponse : IResponse
    {
        private byte[] _data;

        public JSONResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "text";
        }

        public string GetContentType()
        {
            return "application/json";
        }

        public object GetContent()
        {
            return Encoding.Default.GetString(_data);
        }
    }
}
