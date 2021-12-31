using lohost.API.Hubs.RequestTypes;
using lohost.Logging;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace lohost.API.Hubs
{
    [HubName("LocalApplicationHub")]
    public class LocalApplicationHub : Hub
    {
        private static Dictionary<string, string> _ConnectedApplications = new Dictionary<string, string>();

        private SystemLogging _systemLogging;

        public LocalApplicationHub(SystemLogging systemLogging)
        {
            _systemLogging = systemLogging;
        }

        public bool IsConnected(string applicationId)
        {
            return _ConnectedApplications.ContainsKey(applicationId);
        }    

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            string applicationId = httpContext.Request.Query["applicationId"];

            if (!string.IsNullOrEmpty(applicationId))
            {
                _ConnectedApplications[applicationId] = Context.ConnectionId;

                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            if (_ConnectedApplications.ContainsValue(Context.ConnectionId))
            {
                foreach (var applicationConnection in _ConnectedApplications.Where(kvp => kvp.Value == Context.ConnectionId).ToList())
                {
                    _ConnectedApplications.Remove(applicationConnection.Key);
                }
            }

            await base.OnDisconnectedAsync(ex);
        }

        public async Task<int> GetSendingChunks(string applicationId, string fileId)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                IntRequest intRequest = new IntRequest();

                await Clients.Client(_ConnectedApplications[applicationId]).SendAsync("GetSendingChunks", intRequest.TransactionId, fileId);

                int? numberOfChunks = intRequest.Execute();

                if (numberOfChunks.HasValue)
                {
                    return numberOfChunks.Value;
                }
                else
                {
                    throw new Exception("Error retrieving chunk size");
                }
            }
            else
            {
                throw new Exception("Unable to connect to local application");
            }
        }

        public void NumberOfSendingChunks(string transactionId, int? noOfChunks)
        {
            IntRequest.EventOccured(new IntResp()
            {
                TransactionID = transactionId,
                Int = noOfChunks
            });
        }

        public async Task<byte[]> SendDocument(string applicationId, string fileId)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                ByteArrayRequest byteArrayRequest = new ByteArrayRequest();

                await Clients.Client(_ConnectedApplications[applicationId]).SendAsync("DownloadDocument", byteArrayRequest.TransactionId, fileId);

                byte[] fileData = byteArrayRequest.Execute();

                if (fileData != null)
                {
                    return fileData;
                }
                else
                {
                    throw new Exception("Error download document");
                }
            }
            else
            {
                throw new Exception("Unable to connect to local application");
            }
        }

        public void SentDocument(string transactionId, string documentData)
        {
            ByteArrayRequest.EventOccured(new ByteArrayResp()
            {
                TransactionID = transactionId,
                ByteArray = Convert.FromBase64String(documentData),
                ChunkIndex = 0,
                FinalChunk = true
            });
        }

        public async Task<byte[]> SendDocumentChunk(string applicationId, string fileId, long startRange, long endRange)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                ByteArrayRequest byteArrayRequest = new ByteArrayRequest();

                await Clients.Client(_ConnectedApplications[applicationId]).SendAsync("DownloadDocumentChunk", byteArrayRequest.TransactionId, fileId, startRange, endRange);

                byte[] fileData = byteArrayRequest.Execute();

                if (fileData != null)
                {
                    return fileData;
                }
                else
                {
                    throw new Exception("Error download document");
                }
            }
            else
            {
                throw new Exception("Unable to connect to local application");
            }
        }

        public void SentDocumentChunk(string transactionId, int chunkIndex, string documentData, bool finalChunk)
        {
            ByteArrayRequest.EventOccured(new ByteArrayResp()
            {
                TransactionID = transactionId,
                ByteArray = Convert.FromBase64String(documentData),
                ChunkIndex = chunkIndex,
                FinalChunk = finalChunk
            });
        }

        public string Handshake(string input)
        {
            return "5678";
        }
    }
}
