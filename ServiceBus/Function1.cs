using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ServiceBus
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
            [ServiceBusTrigger("azure-webjobs-hosts", Connection = "AzureWebJobsStorage3")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            try
            {
                // Vérification que le message n'est pas null
                if (message == null)
                {
                    _logger.LogWarning("Received a null message. Skipping processing.");
                    return;
                }

                // Extraction des informations du message
                string messageId = message.MessageId ?? "No MessageId";
                string body = message.Body.ToString();
                string contentType = message.ContentType ?? "No Content-Type";

                _logger.LogInformation($"Message ID: {messageId}");
                _logger.LogInformation($"Message Body: {body}");
                _logger.LogInformation($"Message Content-Type: {contentType}");

                // Valider le contenu du message (optionnel)
                if (string.IsNullOrEmpty(body))
                {
                    _logger.LogWarning("Received an empty message body. Skipping processing.");
                    return;
                }

                // Simuler une logique métier (à personnaliser selon ton besoin)
                _logger.LogInformation($"Processing message content: {body}");

                // Logique métier ici (par exemple, traiter un fichier Blob ou effectuer une opération)

                // Compléter le message après un traitement réussi
                await messageActions.CompleteMessageAsync(message);
                _logger.LogInformation("Message successfully completed.");
            }
            catch (Exception ex)
            {
                // Gestion des erreurs
                _logger.LogError($"Error while processing the message: {ex.Message}");
                throw; // Le message restera dans la file d'attente pour un réessai
            }
        }
    }
}
