using System;
using System.Threading;

namespace lohost.API.Request
{
    public class IntRequest
    {
        public static event EventHandler<IntResp> IntResponse = delegate { };

        private AutoResetEvent _messageReceived = new AutoResetEvent(false);
        private int? data = null;

        private int _defaultTimeout = 10 * 1000;

        public string TransactionId
        {
            get; set;
        }

        public IntRequest()
        {
            TransactionId = System.Guid.NewGuid().ToString();
        }

        public int? Execute()
        {
            IntResponse += Handler;

            try
            {
                bool responseReceived = this._messageReceived.WaitOne(_defaultTimeout);

                IntResponse -= Handler;

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
                IntResponse -= Handler;

                throw;
            }
        }

        private void Handler(object sender, IntResp args)
        {
            if (args.TransactionID == TransactionId)
            {
                data = args.Int;

                this._messageReceived.Set();
            }
        }

        public static void EventOccured(IntResp IntResp)
        {
            IntResponse?.Invoke(null, IntResp);
        }
    }

    public class IntResp : EventArgs
    {
        public string TransactionID { get; set; }

        public int? Int { get; set; }
    }
}
