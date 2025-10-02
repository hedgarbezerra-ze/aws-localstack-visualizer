using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AwsLocalStackVisualizer.Models;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SnsService : ISnsService
{
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly ILogger<SnsService> _logger;
    private readonly INotificationService _notificationService;

    public SnsService(AmazonSimpleNotificationServiceClient snsClient, ILogger<SnsService> logger, INotificationService notificationService)
    {
        _snsClient = snsClient;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetTopicsAsync()
    {
        try
        {
            var response = await _snsClient.ListTopicsAsync();
            var topics = new List<SnsTopicInfo>();

            foreach (var topic in response.Topics)
            {
                var topicName = topic.TopicArn.Split(':').Last();
                var subscriptions = await _snsClient.ListSubscriptionsByTopicAsync(topic.TopicArn);
                
                topics.Add(new SnsTopicInfo(
                    topicName,
                    topic.TopicArn,
                    subscriptions.Subscriptions.Count,
                    DateTime.UtcNow // SNS não fornece data de criação via API
                ));
            }

            return new OperationResult<IReadOnlyList<SnsTopicInfo>>(true, topics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar tópicos SNS");
            _notificationService.ShowError($"Erro ao carregar tópicos SNS: {ex.Message}");
            return new OperationResult<IReadOnlyList<SnsTopicInfo>>(false, Array.Empty<SnsTopicInfo>(), ex.Message, ex);
        }
    }

    public async Task<OperationResult<SnsTopicDetails>> GetTopicDetailsAsync(string topicArn)
    {
        try
        {
            var topicsResult = await GetTopicsAsync();
            if (!topicsResult.IsSuccess || topicsResult.Data == null)
                return new OperationResult<SnsTopicDetails>(false, null, "Falha ao obter lista de tópicos");
                
            var topicInfo = topicsResult.Data.FirstOrDefault(t => t.Arn == topicArn);
            
            if (topicInfo == null)
            {
                var topicName = topicArn.Split(':').Last();
                topicInfo = new SnsTopicInfo(topicName, topicArn, 0, DateTime.UtcNow);
            }

            var response = await _snsClient.ListSubscriptionsByTopicAsync(topicArn);
            var subscriptions = response.Subscriptions.Select(sub => new SnsSubscriptionInfo(
                sub.SubscriptionArn,
                sub.Protocol,
                sub.Endpoint,
                !string.IsNullOrEmpty(sub.SubscriptionArn) && sub.SubscriptionArn != "PendingConfirmation",
                sub.Owner
            )).ToList();

            return new OperationResult<SnsTopicDetails>(true, new SnsTopicDetails(topicInfo, subscriptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do tópico {TopicArn}", topicArn);
            _notificationService.ShowError($"Erro ao carregar detalhes do tópico: {ex.Message}");
            var topicName = topicArn.Split(':').Last();
            var defaultTopic = new SnsTopicInfo(topicName, topicArn, 0, DateTime.UtcNow);
            return new OperationResult<SnsTopicDetails>(false, new SnsTopicDetails(defaultTopic, Array.Empty<SnsSubscriptionInfo>()), ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> CreateTopicAsync(string topicName, Dictionary<string, string>? attributes = null)
    {
        try
        {
            var request = new CreateTopicRequest
            {
                Name = topicName
            };

            if (attributes != null && attributes.Any())
            {
                request.Attributes = attributes;
            }

            var response = await _snsClient.CreateTopicAsync(request);
            
            _logger.LogInformation("Tópico SNS {TopicName} criado com sucesso", topicName);
            _notificationService.ShowSuccess($"Tópico '{topicName}' criado com sucesso!");
            return new OperationResult<string>(true, response.TopicArn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar tópico SNS {TopicName}", topicName);
            _notificationService.ShowError($"Erro ao criar tópico: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> PublishMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null)
    {
        try
        {
            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                Subject = subject
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

            var response = await _snsClient.PublishAsync(request);
            
            _logger.LogInformation("Mensagem publicada no tópico SNS {TopicArn}", topicArn);
            _notificationService.ShowSuccess("Mensagem publicada com sucesso!");
            return new OperationResult<string>(true, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem no tópico SNS {TopicArn}", topicArn);
            _notificationService.ShowError($"Erro ao publicar mensagem: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> SubscribeAsync(string topicArn, string protocol, string endpoint, Dictionary<string, string>? attributes = null)
    {
        try
        {
            var request = new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = protocol,
                Endpoint = endpoint
            };

            if (attributes != null && attributes.Any())
            {
                request.Attributes = attributes;
            }

            var response = await _snsClient.SubscribeAsync(request);
            
            _logger.LogInformation("Assinatura criada para tópico SNS {TopicArn}", topicArn);
            _notificationService.ShowSuccess("Assinatura criada com sucesso!");
            return new OperationResult<string>(true, response.SubscriptionArn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar assinatura para tópico SNS {TopicArn}", topicArn);
            _notificationService.ShowError($"Erro ao criar assinatura: {ex.Message}");
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> DeleteTopicAsync(string topicArn)
    {
        try
        {
            await _snsClient.DeleteTopicAsync(topicArn);
            
            _logger.LogInformation("Tópico SNS {TopicArn} excluído com sucesso", topicArn);
            _notificationService.ShowSuccess("Tópico excluído com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir tópico SNS {TopicArn}", topicArn);
            _notificationService.ShowError($"Erro ao excluir tópico: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> UnsubscribeAsync(string subscriptionArn)
    {
        try
        {
            await _snsClient.UnsubscribeAsync(subscriptionArn);
            
            _logger.LogInformation("Assinatura SNS {SubscriptionArn} removida com sucesso", subscriptionArn);
            _notificationService.ShowSuccess("Assinatura removida com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover assinatura SNS {SubscriptionArn}", subscriptionArn);
            _notificationService.ShowError($"Erro ao remover assinatura: {ex.Message}");
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
