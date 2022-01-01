using System;
using System.Threading;

namespace lohost.API.Request
{
    public class StringRequest
    {
        public static event EventHandler<StringResp> StringResponse = delegate { };

        private AutoResetEvent _messageReceived = new AutoResetEvent(false);
        private string data = null;

        private int _defaultTimeout = 10 * 1000;

        public string TransactionId
        {
            get; set;
        }

        public StringRequest()
        {
            TransactionId = System.Guid.NewGuid().ToString();
        }

        public string Execute()
        {
            StringResponse += Handler;

            try
            {
                bool responseReceived = this._messageReceived.WaitOne(_defaultTimeout);

                StringResponse -= Handler;

                if (responseReceived)
                {
                    return data;
                }
                else
                {
                    throw new Exception("Error retrieving response");
                }
            }
            catch (Exception)
            {
                StringResponse -= Handler;

                throw;
            }
        }

        private void Handler(object sender, StringResp args)
        {
            if (args.TransactionID == TransactionId)
            {
                data = args.String;

                this._messageReceived.Set();
            }
        }

        public static void EventOccured(StringResp stringResp)
        {
            StringResponse?.Invoke(null, stringResp);
        }
    }

    public class StringResp : EventArgs
    {
        public string TransactionID { get; set; }

        public string String { get; set; }
    }
}
