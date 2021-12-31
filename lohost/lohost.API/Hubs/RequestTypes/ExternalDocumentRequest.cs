using lohost.Models;

namespace lohost.API.Hubs.RequestTypes
{
    public class ExternalDocumentRequest
    {
        public static EventHandler<ExternalDocumentResp> ExternalDocumentResponse;

        private AutoResetEvent _messageReceived = new AutoResetEvent(false);
        private ExternalDocument document = null;

        private int _defaultTimeout = 10 * 1000;

        public string TransactionId
        {
            get; set;
        }

        public ExternalDocumentRequest()
        {
            TransactionId = System.Guid.NewGuid().ToString();
        }

        public ExternalDocument Execute()
        {
            ExternalDocumentResponse += Handler;

            try
            {
                bool responseReceived = this._messageReceived.WaitOne(_defaultTimeout);

                ExternalDocumentResponse -= Handler;

                if (responseReceived)
                {
                    return document;
                }
                else
                {
                    throw new Exception("Error retrieving response");
                }
            }
            catch (Exception)
            {
                ExternalDocumentResponse -= Handler;

                throw;
            }
        }

        private void Handler(object sender, ExternalDocumentResp args)
        {
            if (args.TransactionID == TransactionId)
            {
                document = args.ExternalDocument;

                this._messageReceived.Set();
            }
        }

        public static void EventOccured(ExternalDocumentResp externalDocumentResp)
        {
            ExternalDocumentResponse?.Invoke(null, externalDocumentResp);
        }
    }

    public class ExternalDocumentResp : EventArgs
    {
        public string TransactionID { get; set; }

        public ExternalDocument ExternalDocument { get; set; }
    }
}
