using lohost.API.Hubs;
using lohost.API.Logging;
using lohost.API.Response;
using lohost.Models;

namespace lohost.API.Controllers
{
    public class LocalApplication
    {
        private readonly LocalApplicationHub _localApplicationHub;
        private readonly SystemLogging _systemLogging;

        private long? maxResponseSizeB;

        public LocalApplication(SystemLogging systemLogging, LocalApplicationHub localApplicationHub, int? maxResponseSizeMB)
        {
            _localApplicationHub = localApplicationHub;
            _systemLogging = systemLogging;

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
                    _systemLogging.Info($"Checking to see if file exceeds the max response size: {maxResponseSizeB}");

                    if (selectedFile.Size > maxResponseSizeB.Value)
                    {
                        _systemLogging.Info($"{selectedFile.Path} exceeds the maximum file size: {selectedFile.Size}B");

                        return new DocumentResponse()
                        {
                            DocumentPath = document,
                            DocumentData = null
                        };
                    }
                }

                _systemLogging.Info("Retrieved the selected file: " + document);

                int chunkSize = await _localApplicationHub.GetChunkSize(applicationId, document);

                _systemLogging.Info("Retrieved file size: " + selectedFile.Size);
                _systemLogging.Info("Retrieved chunk size: " + chunkSize);

                int numberOfChunks = (int)Math.Ceiling((double)selectedFile.Size / chunkSize);

                _systemLogging.Info("Number of chunks: " + numberOfChunks);

                if (numberOfChunks > 1)
                {
                    List<byte> allData = new List<byte>();

                    for (int i = 0; i < numberOfChunks; i++)
                    {
                        long startRange = i * chunkSize;
                        long endRange = (i + 1) * chunkSize;
                        if (endRange > selectedFile.Size) endRange = (long)selectedFile.Size;

                        _systemLogging.Info($"Retrieving document chunk: {startRange} - {endRange}");

                        allData.AddRange(await _localApplicationHub.SendDocumentChunk(applicationId, document, startRange, endRange));

                        _systemLogging.Info($"Retrived document chunk number {i}");
                    }

                    _systemLogging.Info("Sending chunked data");

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