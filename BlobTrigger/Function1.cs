using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlobTrigger
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        // Le constructeur injecte le logger pour enregistrer les événements.
        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run(
            [BlobTrigger("blob/{name}", Connection = "AzureWebJobsStorage1")] Stream stream, string name)
        {
            _logger.LogInformation($"Blob Trigger activated for blob: {name}");

            try
            {
                // Vérification si le flux est null ou vide
                if (stream == null || stream.Length == 0)
                {
                    _logger.LogWarning($"Blob {name} is empty or null. Skipping processing.");
                    return;
                }

                // Lire le contenu du blob en tant que texte
                using var blobStreamReader = new StreamReader(stream);
                var content = await blobStreamReader.ReadToEndAsync();
                _logger.LogInformation($"Blob content successfully read for blob: {name}");

                // Logique métier simulée : afficher le contenu du blob
                _logger.LogInformation($"Processed blob content:\n{content}");

                // (Optionnel) : Suppression du blob après traitement
                // Activer cette fonctionnalité si tu veux supprimer automatiquement le fichier
                var blobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage1"), "blob", name);
                await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation($"Blob {name} has been deleted after processing.");
            }
            catch (Exception ex)
            {
                // Gestion des erreurs avec un log détaillé
                _logger.LogError($"Error processing blob {name}: {ex.Message}");
                throw; // Relance l'exception pour des tentatives ultérieures (si configuré)
            }
        }
    }
}
