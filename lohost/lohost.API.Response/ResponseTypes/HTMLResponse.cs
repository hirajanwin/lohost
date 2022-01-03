using System.Text;

namespace lohost.API.Response
{
    public class HTMLResponse : IResponse
    {
        private byte[] _data;

        public HTMLResponse(byte[] data)
        {
            _data = data;
        }

        public string GetResultType()
        {
            return "text";
        }

        public string GetContentType()
        {
            return "text/html";
        }

        public object GetContent()
        {
            return Encoding.Default.GetString(_data);
        }
    }
}
