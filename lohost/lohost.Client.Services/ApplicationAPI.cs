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
                    _logger.Error("Error stopping hub connection", ex);
                }

                _apiHubConnection = null;
            }

            _apiHubConnection = new HubConnectionBuilder().WithUrl(_applicationData.ExternalAPI + $"/ApplicationHub?applicationId={_applicationData.ApplicationId}").Build();

            _apiHubConnection.Closed += async (error) =>
            {
                _logger.Error("SignalR connection aborted", error);

                await Task.Delay(1000);

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
                _logger.Info("Starting connection");

                await _apiHubConnection.StartAsync();

                connected = true;
            }
            catch (Exception ex)
            {
                _logger.Info($"Error connecting to hub: {ex.Message}");
            }

            if (connected)
            {
                _logger.Info("Connection Started, checking handshake");

                string response = await _apiHubConnection.InvokeAsync<string>("Handshake", "1234");

                _logger.Info("Handshake response: " + response);
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
            _logger.Info($"Request received to get document");

            string applicationFolder = _applicationData.GetApplicationFolder();

            if (!string.IsNullOrEmpty(applicationFolder))
            {
                string filePath = Path.Join(applicationFolder, document.Replace(':', '\\'));
                if (File.Exists(filePath))
                {
                    _logger.Info($"Getting file information for {filePath}");

                    FileInfo fi = new FileInfo(filePath);

                    await _apiHubConnection.InvokeAsync("DocumentRetrieved", transactionId, new ExternalDocument()
                    {
                        Path = document,
                        Size = fi.Length
                    });
                }
                else
                {
                    _logger.Info($"Unable to find file {filePath}");

                    await _apiHubConnection.InvokeAsync("DocumentRetrieved", transactionId, null);
                }
            }
            else
            {
                _logger.Info($"Unable to find application folder");

                await _apiHubConnection.InvokeAsync("DocumentRetrieved", transactionId, null);
            }
        }


        public async Task GetChunkSize(string transactionId)
        {
            await _apiHubConnection.InvokeAsync("ChunkSize", transactionId, CHUNK_SIZE);
        }

        public async Task SendDocument(string transactionId, string document)
        {
            _logger.Info($"Request found to send document");

            string applicationFolder = _applicationData.GetApplicationFolder();

            if (!string.IsNullOrEmpty(applicationFolder))
            {
                string filePath = Path.Join(applicationFolder, document.Replace(':', '\\'));

                _logger.Info($"Reading file from {filePath}");

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                _logger.Info($"File read from {filePath}: {fileBytes.Length}");

                int noOfChunks = (int)Math.Ceiling((double)fileBytes.Length / CHUNK_SIZE);

                _logger.Info($"Number of chunks: {noOfChunks}");

                if (noOfChunks > 1)
                {
                    List<byte> allBytes = new List<byte>();

                    for (int i = 0; i < noOfChunks; i++)
                    {
                        _logger.Info($"Reading chung no: {i}");

                        int amountToTake = fileBytes.Length - (i * CHUNK_SIZE);
                        if (amountToTake > CHUNK_SIZE) amountToTake = CHUNK_SIZE;

                        _logger.Info($"Chunk Size: {amountToTake}");

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
                _logger.Info($"Unable to find application folder");

                await _apiHubConnection.InvokeAsync("SentDocument", transactionId, null);
            }
        }

        public async Task SendDocumentChunk(string transactionId, string document, long startRange, long endRange)
        {
            _logger.Info($"Request found to send document chunk");

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

                _logger.Info($"File read from {filePath}: {fileBytes.Length}");

                int noOfChunks = (int)Math.Ceiling((double)fileBytes.Length / CHUNK_SIZE);

                _logger.Info($"Number of chunks: {noOfChunks}");

                if (noOfChunks > 1)
                {
                    for (int i = 0; i < noOfChunks; i++)
                    {
                        _logger.Info($"Sending chung no: {i}");

                        int amountToTake = fileBytes.Length - (i * CHUNK_SIZE);
                        if (amountToTake > CHUNK_SIZE) amountToTake = CHUNK_SIZE;

                        _logger.Info($"Chunk Size: {amountToTake}");

                        await _apiHubConnection.InvokeAsync("SentDocumentChunk", transactionId, i, Convert.ToBase64String(fileBytes.Skip(i * CHUNK_SIZE).Take(amountToTake).ToArray()), (i == noOfChunks - 1));
                    }
                }
                else
                {
                    await _apiHubConnection.InvokeAsync("SentDocumentChunk", transactionId, 0, Convert.ToBase64String(fileBytes), true);
                }
            }
            else
            {
                await _apiHubConnection.InvokeAsync("SentDocumentChunk", transactionId, 0, null, true);
            }
        }
    }
}
