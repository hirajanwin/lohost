using System.Text;

namespace lohost.API.Response
{
    public class CSSResponse : IResponse
    {
        private byte[] _data;

        public CSSResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "text";
        }

        public string GetContentType()
        {
            return "text/css";
        }

        public object GetContent()
        {
            return Encoding.Default.GetString(_data);
        }
    }
}
