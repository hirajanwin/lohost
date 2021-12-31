using System;
using System.Threading;

namespace lohost.API.Hubs.RequestTypes
{
    public class BoolRequest
    {
        public static event EventHandler<BoolResp> BoolResponse = delegate { };

        private AutoResetEvent _messageReceived = new AutoResetEvent(false);
        private bool? data = null;

        private int _defaultTimeout = 10 * 1000;

        public string TransactionId
        {
            get; set;
        }

        public BoolRequest()
        {
            TransactionId = System.Guid.NewGuid().ToString();
        }

        public bool? Execute()
        {
            BoolResponse += Handler;

            try
            {
                bool responseReceived = this._messageReceived.WaitOne(_defaultTimeout);

                BoolResponse -= Handler;

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
                BoolResponse -= Handler;

                throw;
            }
        }

        private void Handler(object sender, BoolResp args)
        {
            if (args.TransactionID == TransactionId)
            {
                data = args.Bool;

                this._messageReceived.Set();
            }
        }

        public static void EventOccured(BoolResp BoolResp)
        {
            BoolResponse?.Invoke(null, BoolResp);
        }
    }

    public class BoolResp : EventArgs
    {
        public string TransactionID { get; set; }

        public bool? Bool { get; set; }
    }
}
