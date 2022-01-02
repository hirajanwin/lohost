using lohost.Client.Logging;
using lohost.Client.Models;
using lohost.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace lohost.Client.Services
{
    public class ApplicationAPI
    {
        private int CHUNK_SIZE = (4 * 1024 * 1024);

        private Log _logger;

        private ApplicationData _applicationData;

        private HubConnection _apiHubConnection;

        private bool _handshakeComplete = false;
        private bool _reconnect = false;

        public ApplicationAPI(Log log, ApplicationData applicationData)
        {
            _logger = log;

            _applicationData = applicationData;
        }

        public async Task ConnectSignalR()
        {

            if (_apiHubConnection != null)
            {
                try
                {
                    await _apiHubConnection.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error stopping connction to {_applicationData.GetRegisteredAddress()}", ex);
                }

                _apiHubConnection = null;
            }

            string apiKeyQueryParam = !string.IsNullOrEmpty(_applicationData.ApplicationKey) ? $"&appKey={_applicationData.ApplicationKey}" : string.Empty;
            string appPathsQueryParam = ((_applicationData.ApplicationPaths != null) && (_applicationData.ApplicationPaths.Length > 0)) ? $"&paths={string.Join("|", _applicationData.ApplicationPaths)}" : string.Empty;

            _apiHubConnection = new HubConnectionBuilder().WithUrl(_applicationData.ExternalAPI + $"/ApplicationHub?appId={_applicationData.ApplicationId}{apiKeyQueryParam}{appPathsQueryParam}").Build();
   
            _apiHubConnection.Closed += async (error) =>
            {
                if (_handshakeComplete)
                {
                    _logger.Error("SignalR connection aborted", error);

                    await Task.Delay(1000);
                }
                else
                {
                    _logger.Info($"Unable to register this client to {_applicationData.GetRegisteredAddress()}, waiting for it to become available");

                    await Task.Delay(5000);
                }

                if (_reconnect) await ConnectSignalR();
            };

            _apiHubConnection.On("GetDocument", async (string transactionId, string document) => await GetDocument(transactionId, document));

            _apiHubConnection.On("GetChunkSize", async (string transactionId) => await GetChunkSize(transactionId));

            _apiHubConnection.On("SendDocument", async (string transactionId, string document) => await SendDocument(transactionId, document));

            _apiHubConnection.On("SendDocumentChunk", async (string transactionId, string document, long startRange, long endRange) => await SendDocumentChunk(transactionId, document, startRange, endRange));

            await Start();
        }

        public async Task Start()
        {
            _reconnect = true;

            bool connected = false;

            try
            {
                _logger.Info($"Attempting to register this client as {_applicationData.GetRegisteredAddress()}");

                await _apiHubConnection.StartAsync();

                connected = true;
            }
            catch (Exception ex)
            {
                _logger.Info($"Error connecting {_applicationData.GetRegisteredAddress()}: {ex.Message}");
            }

            if (connected)
            {
                try
                {
                    string response = await _apiHubConnection.InvokeAsync<string>("Handshake", "1234");

                    _handshakeComplete = true;

                    _logger.Info("Connection made, handshake complete");

                    _logger.Info($"Your website should now be available at: {_applicationData.GetRegisteredAddress()}");
                }
                catch (Exception)
                {
                }
            }
            else
            {
                await Task.Delay(1000);

                await Start();
            }
        }

        public async Task Stop()
        {
            _reconnect = false;

            await _apiHubConnection.StopAsync();
        }

        public async Task GetDocument(string transactionId, string document)
        {
            _logger.Info($"Request received to get {document}");

            string applicationFolder = _applicationData.GetApplicationFolder();

            if (!string.IsNullOrEmpty(applicationFolder))
            {
                string filePath = Path.Join(applicationFolder, document.Replace(':', '\\'));
                if (File.Exists(filePath))
                {
                    _logger.Debug($"Getting file information for {filePath}");

                    FileInfo fi = new FileInfo(filePath);

                    await _apiHubConnection.InvokeAsync("DocumentRetrieved", transactionId, new ExternalDocument()
                    {
                        Path = document,
                        Size = fi.Length
                    });
                }
                else
                {
                    _logger.Error($"Unable to find file {filePath}");

                    await _apiHubConnection.InvokeAsync("DocumentRetrieved", transactionId, null);
                }
            }
            else
            {
                _logger.Error($"Unable to find application folder");

                await _apiHubConnection.InvokeAsync("DocumentRetrieved", transactionId, null);
            }
        }


        public async Task GetChunkSize(string transactionId)
        {
            await _apiHubConnection.InvokeAsync("ChunkSize", transactionId, CHUNK_SIZE);
        }

        public async Task SendDocument(string transactionId, string document)
        {
            _logger.Info($"Request received to send {document}");

            string applicationFolder = _applicationData.GetApplicationFolder();

            if (!string.IsNullOrEmpty(applicationFolder))
            {
                string filePath = Path.Join(applicationFolder, document.Replace(':', '\\'));

                _logger.Debug($"Reading file from {filePath}");

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                _logger.Debug($"File read from {filePath}: {fileBytes.Length}");

                int noOfChunks = (int)Math.Ceiling((double)fileBytes.Length / CHUNK_SIZE);

                _logger.Debug($"Number of chunks: {noOfChunks}");

                if (noOfChunks > 1)
                {
                    List<byte> allBytes = new List<byte>();

                    for (int i = 0; i < noOfChunks; i++)
                    {
                        _logger.Debug($"Reading chung no: {i}");

                        int amountToTake = fileBytes.Length - (i * CHUNK_SIZE);
                        if (amountToTake > CHUNK_SIZE) amountToTake = CHUNK_SIZE;

                        _logger.Debug($"Chunk Size: {amountToTake}");

                        allBytes.AddRange(fileBytes.Skip(i * CHUNK_SIZE).Take(amountToTake));
                    }

                    await _apiHubConnection.InvokeAsync("SentDocument", transactionId, Convert.ToBase64String(allBytes.ToArray()));
                }
                else
                {
                    await _apiHubConnection.InvokeAsync("SentDocument", transactionId, Convert.ToBase64String(fileBytes));
                }
            }
            else
            {
                _logger.Error($"Unable to find application folder");

                await _apiHubConnection.InvokeAsync("SentDocument", transactionId, null);
            }
        }

        public async Task SendDocumentChunk(string transactionId, string document, long startRange, long endRange)
        {
            _logger.Info($"Request received to send chunk for {document}");

            string applicationFolder = _applicationData.GetApplicationFolder();

            if (!string.IsNullOrEmpty(applicationFolder))
            {
                string filePath = Path.Join(applicationFolder, document.Replace(':', '\\'));

                int readCount = (int)(endRange - startRange);

                byte[] fileBytes = new byte[readCount];

                using (BinaryReader br = new BinaryReader(new FileStream(filePath, FileMode.Open)))
                {
                    br.BaseStream.Seek(startRange, SeekOrigin.Begin);
                    br.Read(fileBytes, 0, readCount);
                }

                _logger.Debug($"File read from {filePath}: {fileBytes.Length}");

                int noOfChunks = (int)Math.Ceiling((double)fileBytes.Length / CHUNK_SIZE);

                _logger.Debug($"Number of chunks: {noOfChunks}");

                if (noOfChunks > 1)
                {
                    for (int i = 0; i < noOfChunks; i++)
                    {
                        _logger.Debug($"Sending chung no: {i+1}");

                        int amountToTake = fileBytes.Length - (i * CHUNK_SIZE);
                        if (amountToTake > CHUNK_SIZE) amountToTake = CHUNK_SIZE;

                        _logger.Debug($"Chunk Size: {amountToTake}");

                        await _apiHubConnection.InvokeAsync("SentDocumentChunk", transactionId, i, Convert.ToBase64String(fileBytes.Skip(i * CHUNK_SIZE).Take(amountToTake).ToArray()), (i == noOfChunks - 1));
                    }
                }
                else
                {
                    _logger.Debug($"Sending chung no: {1}");

                    await _apiHubConnection.InvokeAsync("SentDocumentChunk", transactionId, 0, Convert.ToBase64String(fileBytes), true);
                }
            }
            else
            {
                _logger.Error($"Unable to find application folder");

                await _apiHubConnection.InvokeAsync("SentDocumentChunk", transactionId, 0, null, true);
            }
        }
    }
}
