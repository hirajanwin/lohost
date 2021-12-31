using System;
using System.Threading;

namespace lohost.API.Hubs.RequestTypes
{
    public class LongRequest
    {
        public static event EventHandler<LongResp> LongResponse = delegate { };

        private AutoResetEvent _messageReceived = new AutoResetEvent(false);
        private long? data = null;
        
        private int _defaultTimeout = 10 * 1000;

        public string TransactionId
        {
            get; set;
        }

        public LongRequest()
        {
            TransactionId = System.Guid.NewGuid().ToString();
        }

        public long? Execute()
        {
            LongResponse += Handler;

            try
            {
                bool responseReceived = this._messageReceived.WaitOne(_defaultTimeout);

                LongResponse -= Handler;

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
                LongResponse -= Handler;

                throw;
            }
        }

        private void Handler(object sender, LongResp args)
        {
            if (args.TransactionID == TransactionId)
            {
                data = args.Long;

                this._messageReceived.Set();
            }
        }

        public static void EventOccured(LongResp LongResp)
        {
            LongResponse?.Invoke(null, LongResp);
        }
    }

    public class LongResp : EventArgs
    {
        public string TransactionID { get; set; }

        public long? Long { get; set; }
    }
}
