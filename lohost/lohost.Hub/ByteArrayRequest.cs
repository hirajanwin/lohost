using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace lohost.Hub
{
    public class ByteArrayRequest
    {
        public static event EventHandler<ByteArrayResp> ByteArrayResponse = delegate { };

        private AutoResetEvent _messageReceived = new AutoResetEvent(false);
        private Dictionary<int, byte[]> data = new Dictionary<int, byte[]>();
        private bool finalDataReceived = false;

        private int _defaultTimeout = 10 * 60 * 1000;

        public string TransactionId
        {
            get; set;
        }

        public ByteArrayRequest()
        {
            TransactionId = System.Guid.NewGuid().ToString();
        }

        public byte[] Execute()
        {
            ByteArrayResponse += Handler;

            try
            {
                bool responseReceived = this._messageReceived.WaitOne(_defaultTimeout);

                ByteArrayResponse -= Handler;

                if (responseReceived)
                {
                    List<byte> allData = new List<byte>();

                    foreach (int dataIndex in data.Keys.OrderBy(k => k)) allData.AddRange(data[dataIndex]);
                    
                    return allData.ToArray();
                }
                else
                {
                    throw new Exception("Error retrieving response");
                }
            }
            catch (Exception)
            {
                ByteArrayResponse -= Handler;

                throw;
            }
        }

        private void Handler(object sender, ByteArrayResp args)
        {
            if (args.TransactionID == TransactionId)
            {
                data[args.ChunkIndex] = args.ByteArray;

                if (args.FinalChunk) finalDataReceived = true;

                if (finalDataReceived)
                {
                    bool allDataRetrieved = true;

                    for (int i=0; i<=data.Keys.Max(); i++)
                    {
                        if (!data.ContainsKey(i))
                        {
                            allDataRetrieved = false;
                            break;
                        }
                    }

                    if (allDataRetrieved) this._messageReceived.Set();
                }
            }
        }

        public static void EventOccured(ByteArrayResp byteArrayResp)
        {
            ByteArrayResponse?.Invoke(null, byteArrayResp);
        }
    }

    public class ByteArrayResp : EventArgs
    {
        public string TransactionID { get; set; }

        public byte[] ByteArray { get; set; }

        public int ChunkIndex { get; set; }

        public bool FinalChunk { get; set; }
    }
}
