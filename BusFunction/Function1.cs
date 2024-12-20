using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BusFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run(
            [ServiceBusTrigger("azure-webjobs-secrets", Connection = "AzureWebJobsStorage2")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            try
            {
                // Extraire le nom du fichier depuis le message
                string fileName = message.Body.ToString();
                _logger.LogInformation($"Processing file: {fileName}");

                // Configuration des connexions Azure Storage
                string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage2");
                string sourceContainerName = "images";
                string destinationContainerName = "processed-images";

                // Initialiser les clients pour les conteneurs source et destination
                var blobServiceClient = new BlobServiceClient(storageConnectionString);
                var sourceContainerClient = blobServiceClient.GetBlobContainerClient(sourceContainerName);
                var destinationContainerClient = blobServiceClient.GetBlobContainerClient(destinationContainerName);

                // Télécharger le fichier blob source
                var sourceBlobClient = sourceContainerClient.GetBlobClient(fileName);
                var tempFilePath = Path.GetTempFileName();
                await sourceBlobClient.DownloadToAsync(tempFilePath);
                _logger.LogInformation($"File downloaded: {fileName}");

                // Traiter le fichier (ajout d'un watermark simulé)
                var processedFilePath = Path.GetTempFileName();
                File.WriteAllText(processedFilePath, $"Processed file: {fileName} at {DateTime.UtcNow}");
                _logger.LogInformation($"File processed: {fileName}");

                // Télécharger le fichier traité vers le conteneur de destination
                var destinationBlobClient = destinationContainerClient.GetBlobClient(fileName);
                using (var fileStream = File.OpenRead(processedFilePath))
                {
                    await destinationBlobClient.UploadAsync(fileStream, overwrite: true);
                }
                _logger.LogInformation($"Processed file uploaded: {fileName}");

                // Supprimer le fichier temporaire local
                File.Delete(tempFilePath);
                File.Delete(processedFilePath);

                // Supprimer le fichier original du conteneur source
                await sourceBlobClient.DeleteIfExistsAsync();
                _logger.LogInformation($"Original file deleted: {fileName}");

                // Marquer le message comme complété
                await messageActions.CompleteMessageAsync(message);
                _logger.LogInformation("Message completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                // Message non complété, il reviendra dans la file d'attente (selon la configuration).
                throw;
            }
        }
    }
}
