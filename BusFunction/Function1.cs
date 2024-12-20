using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ServiceBusTrigger
{
    public class Function2
    {
        private readonly ILogger<Function2> _logger;

        public Function2(ILogger<Function2> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function2))]
        public async Task Run(
            [ServiceBusTrigger("file-processing-queue", Connection = "AzureServiceBusConnection")] string fileName)
        {
            _logger.LogInformation($"Service Bus Trigger activated for file: {fileName}");

            try
            {
                // Configuration des conteneurs source et destination
                var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage1");
                var sourceContainerName = "images";
                var destinationContainerName = "processed-images";

                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var sourceBlobClient = blobServiceClient.GetBlobContainerClient(sourceContainerName).GetBlobClient(fileName);
                var destinationBlobClient = blobServiceClient.GetBlobContainerClient(destinationContainerName).GetBlobClient(fileName);

                // Télécharger le fichier source
                var tempFilePath = Path.GetTempFileName();
                await sourceBlobClient.DownloadToAsync(tempFilePath);
                _logger.LogInformation($"File {fileName} downloaded for processing.");

                // Simuler une opération sur le fichier (ex. ajout d'un watermark ou redimensionnement)
                var processedFilePath = Path.GetTempFileName();
                File.WriteAllText(processedFilePath, $"Processed file: {fileName} at {DateTime.UtcNow}");
                _logger.LogInformation($"File {fileName} has been processed.");

                // Télécharger le fichier traité vers le conteneur de destination
                using (var fileStream = File.OpenRead(processedFilePath))
                {
                    await destinationBlobClient.UploadAsync(fileStream, overwrite: true);
                }
                _logger.LogInformation($"File {fileName} uploaded to container: {destinationContainerName}");

                // Supprimer le fichier original du conteneur source
                await sourceBlobClient.DeleteIfExistsAsync();
                _logger.LogInformation($"File {fileName} deleted from container: {sourceContainerName}");

                // Supprimer les fichiers temporaires locaux
                File.Delete(tempFilePath);
                File.Delete(processedFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file {fileName}: {ex.Message}");
                throw; // Relance l'exception pour des tentatives ultérieures
            }
        }
    }
}
