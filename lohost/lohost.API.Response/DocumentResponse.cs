namespace lohost.API.Response
{
    public class DocumentResponse
    {
        public string DocumentPath { get; set; }

        public byte[] DocumentData { get; set; }

        public IResponse GetResponse()
        {
            return new HTMLResponse(DocumentData);
        }
    }
}