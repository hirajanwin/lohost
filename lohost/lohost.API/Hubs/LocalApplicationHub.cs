using lohost.API.Request;
using lohost.Models;
using System.Runtime.Caching;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using lohost.API.Models;
using lohost.API.Helpers;
using lohost.API.Logging;

namespace lohost.API.Hubs
{
    [HubName("LocalApplicationHub")]
    public class LocalApplicationHub : Hub
    {
        // Probably should be moved to Redis at some point
        private static Dictionary<string, ApplicationConnection> _ConnectedApplications = new Dictionary<string, ApplicationConnection>();

        private SystemLogging _systemLogging;

        public LocalApplicationHub(SystemLogging systemLogging)
        {
            _systemLogging = systemLogging;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            if (_ConnectedApplications.Any(ca => ca.Value.ConnectionId == Context.ConnectionId))
            {
                foreach (var applicationConnection in _ConnectedApplications.Where(kvp => kvp.Value.ConnectionId == Context.ConnectionId).ToList())
                {
                    if (!string.IsNullOrEmpty(applicationConnection.Value.Key))
                    {
                        MemoryCache connectedApplicationLockCache = MemoryCache.Default;

                        if (connectedApplicationLockCache.Contains(applicationConnection.Key)) connectedApplicationLockCache.Remove(applicationConnection.Key);

                        connectedApplicationLockCache.Add(applicationConnection.Key, applicationConnection.Value, new CacheItemPolicy
                        {
                            AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10)
                        });
                    }

                    _ConnectedApplications.Remove(applicationConnection.Key);
                }
            }

            await base.OnDisconnectedAsync(ex);
        }

        public async Task Register(string connectionId, string applicationId, string applicationKey, string applicationPaths)
        {
            _systemLogging.Debug($"connectionId: {connectionId}");
            _systemLogging.Debug($"applicationId: {applicationId}");
            _systemLogging.Debug($"applicationKey: {applicationKey}");
            _systemLogging.Debug($"applicationPaths: {applicationPaths}");

            if (!string.IsNullOrEmpty(applicationId))
            {
                bool addedConnection = false;

                applicationId = applicationId.ToLower();

                string[] appPaths;

                if (!string.IsNullOrEmpty(applicationPaths)) appPaths = applicationPaths.Split(new char[] { '|' });
                else appPaths = new string[] { "*" };

                for (int i = 0; i < appPaths.Length; i++)
                {
                    string appId;

                    if (appPaths[i] == "*") appId = applicationId;
                    else appId = $"{applicationId}:{appPaths[i].ToLower().TrimStart('/')}";

                    string appPath = appPaths[i].ToLower().TrimStart('/');

                    if (!_ConnectedApplications.ContainsKey(appId))
                    {
                        MemoryCache connectedApplicationLockCache = MemoryCache.Default;

                        if (connectedApplicationLockCache.Contains(appId))
                        {
                            ApplicationConnection applicationConnetion = (ApplicationConnection)connectedApplicationLockCache.Get(applicationId);

                            if ((applicationConnetion != null) &&
                                (!string.IsNullOrEmpty(applicationKey) && (applicationConnetion.Key == applicationKey)))
                            {
                                if (appPaths.Length > 0)
                                {
                                    _ConnectedApplications[appId] = new ApplicationConnection()
                                    {
                                        ConnectionId = connectionId,
                                        Key = applicationKey,
                                        Path = appPath
                                    };
                                }

                                connectedApplicationLockCache.Remove(applicationId);

                                addedConnection = true;
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(applicationKey))
                            {
                                _ConnectedApplications[appId] = new ApplicationConnection()
                                {
                                    ConnectionId = connectionId,
                                    Path = appPath
                                };
                            }
                            else
                            {
                                _ConnectedApplications[appId] = new ApplicationConnection()
                                {
                                    ConnectionId = connectionId,
                                    Key = applicationKey,
                                    Path = appPath
                                };
                            }

                            addedConnection = true;
                        }
                    }
                }

                _systemLogging.Debug($"addedConnection: {addedConnection}");

                if (!addedConnection) Context.Abort();
            }
            else
            {
                Context.Abort();
            }
        }

        public async Task ListApplication(string applicationId, string name, string[] tags)
        {

        }

        public async Task<ExternalDocument> GetDocument(string applicationId, string document)
        {
            applicationId = applicationId.ToLower();

            string connectionId = HubHelper.GetConnectionId(_ConnectedApplications, applicationId, document);

            if (!string.IsNullOrEmpty(connectionId))
            {
                ExternalDocumentRequest externalDocumentRequest = new ExternalDocumentRequest();

                await Clients.Client(connectionId).SendAsync("GetDocument", externalDocumentRequest.TransactionId, document);

                return externalDocumentRequest.Execute();
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
            applicationId = applicationId.ToLower();

            string connectionId = HubHelper.GetAConnectionId(_ConnectedApplications, applicationId);

            if (!string.IsNullOrEmpty(connectionId))
            {
                IntRequest intRequest = new IntRequest();

                await Clients.Client(connectionId).SendAsync("GetChunkSize", intRequest.TransactionId);

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
            applicationId = applicationId.ToLower();

            string connectionId = HubHelper.GetConnectionId(_ConnectedApplications, applicationId, document);

            if (!string.IsNullOrEmpty(connectionId))
            {
                ByteArrayRequest byteArrayRequest = new ByteArrayRequest();

                await Clients.Client(connectionId).SendAsync("SendDocument", byteArrayRequest.TransactionId, document);

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
            applicationId = applicationId.ToLower();

            string connectionId = HubHelper.GetConnectionId(_ConnectedApplications, applicationId, document);

            if (!string.IsNullOrEmpty(connectionId))
            {
                ByteArrayRequest byteArrayRequest = new ByteArrayRequest();

                await Clients.Client(connectionId).SendAsync("DownloadDocumentChunk", byteArrayRequest.TransactionId, document, startRange, endRange);

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
