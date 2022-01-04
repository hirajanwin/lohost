using lohost.API.Hubs;
using lohost.Logging;
using lohost.API.Response;
using lohost.Models;

namespace lohost.API.Controllers
{
    public class LocalApplication
    {
        private readonly LocalApplicationHub _localApplicationHub;
        private readonly Log _logger;

        private long? maxResponseSizeB;

        public LocalApplication(Log logger, LocalApplicationHub localApplicationHub, int? maxResponseSizeMB)
        {
            _localApplicationHub = localApplicationHub;
            _logger = logger;

            if (maxResponseSizeMB.HasValue)
            {
                maxResponseSizeB = (maxResponseSizeMB * 1024 * 1024);
            }
        }

        public async Task<DocumentResponse> GetDocument(string applicationId, string document)
        {
            ExternalDocument selectedFile = await _localApplicationHub.GetDocument(applicationId, document);

            if (selectedFile != null)
            {
                if (maxResponseSizeB.HasValue)
                {
                    _logger.Info($"Checking to see if file exceeds the max response size: {maxResponseSizeB}");

                    if (selectedFile.Size > maxResponseSizeB.Value)
                    {
                        _logger.Info($"{selectedFile.Path} exceeds the maximum file size: {selectedFile.Size}B");

                        return new DocumentResponse()
                        {
                            DocumentPath = document,
                            DocumentData = null
                        };
                    }
                    else
                    {
                        _logger.Info($"{selectedFile.Path} does not exceed the maximum file size: {selectedFile.Size}B");
                    }
                }

                _logger.Info("Retrieved the selected file: " + document);

                int chunkSize = await _localApplicationHub.GetChunkSize(applicationId, document);

                _logger.Info("Retrieved file size: " + selectedFile.Size);
                _logger.Info("Retrieved chunk size: " + chunkSize);

                int numberOfChunks = (int)Math.Ceiling((double)selectedFile.Size / chunkSize);

                _logger.Info("Number of chunks: " + numberOfChunks);

                if (numberOfChunks > 1)
                {
                    List<byte> allData = new List<byte>();

                    for (int i = 0; i < numberOfChunks; i++)
                    {
                        long startRange = i * chunkSize;
                        long endRange = (i + 1) * chunkSize;
                        if (endRange > selectedFile.Size) endRange = (long)selectedFile.Size;

                        _logger.Info($"Retrieving document chunk: {startRange} - {endRange}");

                        allData.AddRange(await _localApplicationHub.SendDocumentChunk(applicationId, document, startRange, endRange));

                        _logger.Info($"Retrived document chunk number {i}");
                    }

                    _logger.Info("Sending chunked data");

                    return new DocumentResponse()
                    {
                        DocumentPath = document,
                        DocumentData = allData.ToArray()
                    };
                }
                else
                {
                    return new DocumentResponse()
                    {
                        DocumentPath = document,
                        DocumentData = await _localApplicationHub.SendDocument(applicationId, document)
                    };
                }
            }
            else
            {
                return new DocumentResponse()
                {
                    DocumentPath = document,
                    DocumentData = null
                };
            }
        }
    }
}