using lohost.API.Request;
using lohost.Logging;
using lohost.Models;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace lohost.API.Hubs
{
    [HubName("LocalApplicationHub")]
    public class LocalApplicationHub : Hub
    {
        private static Dictionary<string, ApplicationConnection> _ConnectedApplications = new Dictionary<string, ApplicationConnection>();

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
                string applicationKey = httpContext.Request.Query["applicationKey"];

                if (string.IsNullOrEmpty(applicationKey))
                {
                    _ConnectedApplications[applicationId] = new ApplicationConnection()
                    {
                        ConnectionId = Context.ConnectionId,
                    };
                }
                else
                {
                    _ConnectedApplications[applicationId] = new ApplicationConnection()
                    {
                        ConnectionId = Context.ConnectionId,
                        Key = applicationKey
                    };
                }

                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            if (_ConnectedApplications.Any(ca => ca.Value.ConnectionId == Context.ConnectionId))
            {
                foreach (var applicationConnection in _ConnectedApplications.Where(kvp => kvp.Value.ConnectionId == Context.ConnectionId).ToList())
                {
                    if (string.IsNullOrEmpty(applicationConnection.Value.Key))
                    {
                        // There wasn't a key specified to the application, so remove
                        _ConnectedApplications.Remove(applicationConnection.Key);
                    }
                }
            }

            await base.OnDisconnectedAsync(ex);
        }
        public async Task<ExternalDocument> GetDocument(string applicationId, string document)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                ExternalDocumentRequest externalDocumentRequest = new ExternalDocumentRequest();

                await Clients.Client(_ConnectedApplications[applicationId].ConnectionId).SendAsync("GetDocument", externalDocumentRequest.TransactionId, document);

                ExternalDocument file = externalDocumentRequest.Execute();

                if (file != null)
                {
                    return file;
                }
                else
                {
                    throw new Exception("Error retrieving file");
                }
            }
            else
            {
                throw new Exception("Unable to connect to local application");
            }
        }

        public void DocumentRetrieved(string transactionId, ExternalDocument document)
        {
            ExternalDocumentRequest.EventOccured(new ExternalDocumentResp()
            {
                TransactionID = transactionId,
                ExternalDocument = document
            });
        }

        public async Task<int> GetChunkSize(string applicationId)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                if (_ConnectedApplications.ContainsKey(applicationId))
                {
                    IntRequest intRequest = new IntRequest();

                    await Clients.Client(_ConnectedApplications[applicationId].ConnectionId).SendAsync("GetChunkSize", intRequest.TransactionId);

                    int? chunkSize = intRequest.Execute();

                    if (chunkSize.HasValue)
                    {
                        return chunkSize.Value;
                    }
                    else
                    {
                        throw new Exception("Error retrieving chunk size");
                    }
                }
                else
                {
                    throw new Exception("Unable to fnd the local application");
                }
            }
            else
            {
                throw new Exception("Unable to connect to local application");
            }
        }

        public void ChunkSize(string transactionId, int? chunkSize)
        {
            IntRequest.EventOccured(new IntResp()
            {
                TransactionID = transactionId,
                Int = chunkSize
            });
        }

        public async Task<byte[]> SendDocument(string applicationId, string document)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                ByteArrayRequest byteArrayRequest = new ByteArrayRequest();

                await Clients.Client(_ConnectedApplications[applicationId].ConnectionId).SendAsync("SendDocument", byteArrayRequest.TransactionId, document);

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

        public async Task<byte[]> SendDocumentChunk(string applicationId, string document, long startRange, long endRange)
        {
            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                ByteArrayRequest byteArrayRequest = new ByteArrayRequest();

                await Clients.Client(_ConnectedApplications[applicationId].ConnectionId).SendAsync("DownloadDocumentChunk", byteArrayRequest.TransactionId, document, startRange, endRange);

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
