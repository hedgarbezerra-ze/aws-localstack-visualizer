using Amazon.SQS;
using Amazon.SQS.Model;
using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SqsService : ISqsService
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly ILogger<SqsService> _logger;
    private readonly INotificationService _notificationService;

    public SqsService(AmazonSQSClient sqsClient, ILogger<SqsService> logger, INotificationService notificationService)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetQueuesAsync()
    {
        try
        {
            var response = await _sqsClient.ListQueuesAsync(new ListQueuesRequest());
            var queues = new List<SqsQueueInfo>();

            foreach (var queueUrl in response.QueueUrls)
            {
                var queueName = queueUrl.Split('/').Last();
                var attributes = await _sqsClient.GetQueueAttributesAsync(queueUrl, new List<string> 
                { 
                    "ApproximateNumberOfMessages", 
                    "ApproximateNumberOfMessagesNotVisible",
                    "CreatedTimestamp"
                });

                var messageCount = int.Parse(attributes.Attributes.GetValueOrDefault("ApproximateNumberOfMessages", "0"));
                var notVisibleCount = int.Parse(attributes.Attributes.GetValueOrDefault("ApproximateNumberOfMessagesNotVisible", "0"));
                var createdTimestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(attributes.Attributes.GetValueOrDefault("CreatedTimestamp", "0"))).DateTime;

                queues.Add(new SqsQueueInfo(queueName, queueUrl, messageCount, notVisibleCount, createdTimestamp));
            }

            return new OperationResult<IReadOnlyList<SqsQueueInfo>>(true, queues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar filas SQS");
            _notificationService.ShowError($"Erro ao carregar filas SQS: {ex.Message}");
            return new OperationResult<IReadOnlyList<SqsQueueInfo>>(false, Array.Empty<SqsQueueInfo>(), ex.Message, ex);
        }
    }

    public async Task<OperationResult<SqsQueueDetails>> GetQueueDetailsAsync(string queueName)
    {
        try
        {
            var queuesResult = await GetQueuesAsync();
            if (!queuesResult.IsSuccess || queuesResult.Data == null)
                return new OperationResult<SqsQueueDetails>(false, null, "Falha ao obter lista de filas");
                
            var queueInfo = queuesResult.Data.FirstOrDefault(q => q.Name == queueName);
            
            if (queueInfo == null)
            {
                var defaultQueue = new SqsQueueInfo(queueName, "", 0, 0, DateTime.MinValue);
                return new OperationResult<SqsQueueDetails>(true, new SqsQueueDetails(defaultQueue, Array.Empty<SqsMessageInfo>()));
            }

            var messages = new List<SqsMessageInfo>();
            var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueInfo.Url,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 1,
                MessageSystemAttributeNames = new List<string> { "All" },
                MessageAttributeNames = new List<string> { "All" }
            });

            foreach (var message in response.Messages)
            {
                var sentTimestamp = DateTime.UnixEpoch.AddMilliseconds(long.Parse(message.Attributes.GetValueOrDefault("SentTimestamp", "0")));
                var receiveCount = int.Parse(message.Attributes.GetValueOrDefault("ApproximateReceiveCount", "0"));

                messages.Add(new SqsMessageInfo(
                    message.MessageId,
                    message.Body,
                    message.ReceiptHandle,
                    sentTimestamp,
                    receiveCount,
                    message.Attributes
                ));
            }

            return new OperationResult<SqsQueueDetails>(true, new SqsQueueDetails(queueInfo, messages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes da fila {QueueName}", queueName);
            _notificationService.ShowError($"Erro ao carregar detalhes da fila: {ex.Message}");
            var defaultQueue = new SqsQueueInfo(queueName, "", 0, 0, DateTime.MinValue);
            return new OperationResult<SqsQueueDetails>(false, new SqsQueueDetails(defaultQueue, Array.Empty<SqsMessageInfo>()), ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> CreateQueueAsync(string queueName, Dictionary<string, string>? attributes = null)
    {
        try
        {
            var request = new CreateQueueRequest
            {
                QueueName = queueName
            };

            if (attributes != null && attributes.Any())
            {
                request.Attributes = attributes;
            }

            var response = await _sqsClient.CreateQueueAsync(request);
            
            _logger.LogInformation("Fila SQS {QueueName} criada com sucesso", queueName);
            _notificationService.ShowSuccess($"Fila '{queueName}' criada com sucesso!");
            return new OperationResult<string>(true, response.QueueUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fila SQS {QueueName}", queueName);
            _notificationService.ShowError($"Erro ao criar fila: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> SendMessageAsync(string queueUrl, string messageBody, Dictionary<string, string>? messageAttributes = null)
    {
        try
        {
            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageBody
            };

            if (messageAttributes != null && messageAttributes.Any())
            {
                request.MessageAttributes = messageAttributes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new MessageAttributeValue
                    {
                        StringValue = kvp.Value,
                        DataType = "String"
                    });
            }

            var response = await _sqsClient.SendMessageAsync(request);
            
            _logger.LogInformation("Mensagem enviada para fila SQS {QueueUrl}", queueUrl);
            _notificationService.ShowSuccess("Mensagem enviada com sucesso!");
            return new OperationResult<string>(true, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem para fila SQS {QueueUrl}", queueUrl);
            _notificationService.ShowError($"Erro ao enviar mensagem: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> DeleteQueueAsync(string queueUrl)
    {
        try
        {
            await _sqsClient.DeleteQueueAsync(queueUrl);
            
            _logger.LogInformation("Fila SQS {QueueUrl} excluída com sucesso", queueUrl);
            _notificationService.ShowSuccess("Fila excluída com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir fila SQS {QueueUrl}", queueUrl);
            _notificationService.ShowError($"Erro ao excluir fila: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> PurgeQueueAsync(string queueUrl)
    {
        try
        {
            await _sqsClient.PurgeQueueAsync(queueUrl);
            
            _logger.LogInformation("Fila SQS {QueueUrl} limpa com sucesso", queueUrl);
            _notificationService.ShowSuccess("Fila limpa com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar fila SQS {QueueUrl}", queueUrl);
            _notificationService.ShowError($"Erro ao limpar fila: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
