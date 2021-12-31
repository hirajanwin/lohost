using lohost.API.Hubs;
using lohost.API.Models;
using lohost.Logging;

namespace lohost.API.Controllers
{
    public class LocalApplication
    {
        private readonly LocalApplicationHub _localApplicationHub;
        private readonly SystemLogging _systemLogging;

        public LocalApplication(SystemLogging systemLogging, LocalApplicationHub localApplicationHub)
        {
            _localApplicationHub = localApplicationHub;
            _systemLogging = systemLogging;
        }

        public async Task<DocumentResponse> GetDocument(string applicationId, string document)
        {
            /*ExternalDocument selectedFile = await _localApplicationHub.GetItem(applicationId, document);

            if (selectedFile != null)
            {
                _systemLogging.Info("Retrieved the selected file: " + document);

                int chunkSize = await _localApplicationHub.GetChunkSize(owner.Id, localIntegrationId);

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

                        allData.AddRange(await _localApplicationHub.DownloadDocumentChunk(applicationId, document, startRange, endRange));

                        _systemLogging.Info($"Retrived document chunk number {i}");
                    }

                    _systemLogging.Info("Sending chunked data");

                    return StatusCode(200, allData.ToArray());
                }
                else
                {
                    return StatusCode(200, await _localApplicationHub.DownloadDocument(applicationId, document));
                }
            }
            else
            {
                return StatusCode(404, $"Unable to find the selected file");
            }*/

            return null;
        }
    }
}