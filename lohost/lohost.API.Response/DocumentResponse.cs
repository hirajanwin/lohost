namespace lohost.API.Response
{
    public class DocumentResponse
    {
        public string DocumentPath { get; set; }

        public byte[] DocumentData { get; set; }

        public IResponse GetResponse()
        {
            string[] pathParts = DocumentPath.Split('/');

            string ext = Path.GetExtension(pathParts.Last());

            switch (ext.ToLower())
            {
                case ".html":
                    return new HTMLResponse(DocumentData);
                case ".css":
                    return new CSSResponse(DocumentData);
                case ".js":
                    return new JavaScriptResponse(DocumentData);
                case ".jpg":
                    return new JPGResponse(DocumentData);
                default:
                    return new HTMLResponse(DocumentData);
            }
        }
    }
}