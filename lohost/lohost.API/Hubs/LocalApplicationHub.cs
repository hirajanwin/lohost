using lohost.API.Request;
using lohost.Models;
using System.Runtime.Caching;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using lohost.API.Models;
using lohost.API.Logging;

namespace lohost.API.Hubs
{
    [HubName("LocalApplicationHub")]
    public class LocalApplicationHub : Hub
    {
        // Probably should be moved to Redis at some point
        private static Dictionary<string, ApplicationConnection> _ConnectedApplications = new Dictionary<string, ApplicationConnection>();

        private static object _registerApplicationLock = new object();
        private static object _listApplicationLock = new object();

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
            foreach (var applicationConnection in _ConnectedApplications.Where(kvp => kvp.Value.HasApplicationRouteConnection(Context.ConnectionId)).ToList())
            {
                if (!string.IsNullOrEmpty(applicationConnection.Value.Key))
                {
                    bool isEmpty = applicationConnection.Value.RemoveApplicationRouteConnection(Context.ConnectionId);

                    if (isEmpty)
                    {
                        _ConnectedApplications.Remove(applicationConnection.Key);

                        MemoryCache connectedApplicationLockCache = MemoryCache.Default;

                        if (connectedApplicationLockCache.Contains(applicationConnection.Key)) connectedApplicationLockCache.Remove(applicationConnection.Key);

                        connectedApplicationLockCache.Add(applicationConnection.Key, applicationConnection.Value, new CacheItemPolicy
                        {
                            AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10)
                        });
                    }
                }
            }

            await base.OnDisconnectedAsync(ex);
        }

        public static IList<ListedApplication> GetAllListedApplications()
        {
            List<ListedApplication> listedApplications = new List<ListedApplication>();

            foreach (ApplicationConnection application in _ConnectedApplications.Where(ca => ca.Value.IsListed).Select(ca => ca.Value))
            {
                listedApplications.Add(new ListedApplication()
                {
                    Id = application.Id,
                    Name = application.Name,
                    Tags = application.Tags
                });
            }

            return listedApplications;
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

                lock (_registerApplicationLock)
                { 
                    applicationId = applicationId.ToLower();

                    string[] appPaths;

                    if (!string.IsNullOrEmpty(applicationPaths)) appPaths = applicationPaths.Split(new char[] { '|' });
                    else appPaths = new string[] { "*" };

                    for (int i = 0; i < appPaths.Length; i++)
                    {
                        string appPath = appPaths[i].ToLower().TrimStart('/');

                        if (!_ConnectedApplications.ContainsKey(applicationId))
                        {
                            MemoryCache connectedApplicationLockCache = MemoryCache.Default;

                            if (connectedApplicationLockCache.Contains(applicationId))
                            {
                                ApplicationConnection applicationConnetion = (ApplicationConnection)connectedApplicationLockCache.Get(applicationId);

                                if ((applicationConnetion != null) &&
                                    (!string.IsNullOrEmpty(applicationKey) && (applicationConnetion.Key == applicationKey)))
                                {
                                    if (appPaths.Length > 0)
                                    {
                                        ApplicationConnection applicationConnection = new ApplicationConnection()
                                        {
                                            Id = applicationId,
                                            Key = applicationKey
                                        };

                                        applicationConnection.AddApplicationRoute(connectionId, appPath);

                                        _ConnectedApplications[applicationId] = applicationConnection;
                                    }

                                    connectedApplicationLockCache.Remove(applicationId);

                                    addedConnection = true;
                                }
                            }
                            else
                            {
                                ApplicationConnection applicationConnection = new ApplicationConnection()
                                {
                                    Id = applicationId
                                };

                                if (!string.IsNullOrEmpty(applicationKey)) applicationConnection.Key = applicationKey;

                                applicationConnection.AddApplicationRoute(connectionId, appPath);

                                _ConnectedApplications[applicationId] = applicationConnection;

                                addedConnection = true;
                            }
                        }
                        else
                        {
                            if (!_ConnectedApplications[applicationId].ApplicationRouteExists(appPath))
                            {
                                _ConnectedApplications[applicationId].AddApplicationRoute(connectionId, appPath);

                                addedConnection = true;
                            }
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

        public async Task ListApplication(string applicationId, string applicationPaths, string name, string[] tags)
        {
            applicationId = applicationId.ToLower();

            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                if (string.IsNullOrEmpty(applicationPaths) || applicationPaths.Equals("*"))
                {
                    lock (_listApplicationLock)
                    {
                        _ConnectedApplications[applicationId].IsListed = true;
                        _ConnectedApplications[applicationId].Name = name;
                        _ConnectedApplications[applicationId].Tags = tags;
                    }
                }
            }
        }

        public async Task<ExternalDocument> GetDocument(string applicationId, string document)
        {
            applicationId = applicationId.ToLower();

            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                string connectionId = _ConnectedApplications[applicationId].GetConnectionId(document);

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

        public async Task<int> GetChunkSize(string applicationId, string document)
        {
            applicationId = applicationId.ToLower();

            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                string connectionId = _ConnectedApplications[applicationId].GetConnectionId(document);

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
            applicationId = applicationId.ToLower();

            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                string connectionId = _ConnectedApplications[applicationId].GetConnectionId(document);

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

            if (_ConnectedApplications.ContainsKey(applicationId))
            {
                string connectionId = _ConnectedApplications[applicationId].GetConnectionId(document);

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
