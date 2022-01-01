using System.Text;

namespace lohost.API.Response
{
    public class JavaScriptResponse : IResponse
    {
        private byte[] _data;

        public JavaScriptResponse(byte[] data)
        {
            _data = data;
        }

        public string GetContentType()
        {
            return "text/javascript";
        }

        public object GetContent()
        {
            return Encoding.Default.GetString(_data);
        }
    }
}
