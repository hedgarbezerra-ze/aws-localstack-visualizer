using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SNS;
using System.Runtime.CompilerServices;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SnsService : ISnsService
{
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly ILogger<SnsService> _logger;
    private readonly INotificationService _notificationService;

    public SnsService(AmazonSimpleNotificationServiceClient snsClient, ILogger<SnsService> logger, INotificationService notificationService)
    {
        _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<OperationResult<IReadOnlyList<SnsTopicInfo>>> GetTopicsAsync()
    {
        try
        {
            var response = await _snsClient.ListTopicsAsync();
            
            if (response is { Topics: null or { Count: 0 } })
            {
                _logger.LogWarning("Resposta do SNS ListTopics é nula ou não contém tópicos");
                return new OperationResult<IReadOnlyList<SnsTopicInfo>>(true, []);
            }

            var topicTasks = new List<Task<SnsTopicInfo?>>();
            
            foreach (var topic in response.Topics)
            {
                if (string.IsNullOrWhiteSpace(topic.TopicArn))
                {
                    _logger.LogWarning("Tópico com ARN inválido encontrado, ignorando");
                    continue;
                }

                topicTasks.Add(GetTopicInfoAsync(topic.TopicArn));
            }

            var topicResults = await Task.WhenAll(topicTasks);
            var topics = topicResults.Where(t => t is not null).Cast<SnsTopicInfo>().ToList();
            
            return new OperationResult<IReadOnlyList<SnsTopicInfo>>(true, topics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar tópicos SNS");
            return new OperationResult<IReadOnlyList<SnsTopicInfo>>(false, [], ex.Message, ex);
        }
    }

    public async IAsyncEnumerable<SnsTopicInfo> GetTopicsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<Task<SnsTopicInfo?>> validTopics;
        try
        {
            var response = await _snsClient.ListTopicsAsync();
            
            if (response is { Topics: null or { Count: 0 } })
            {
                _logger.LogWarning("Resposta do SNS ListTopics é nula ou não contém tópicos");
                yield break;
            }

            validTopics = response.Topics
                .Where(topic => !string.IsNullOrWhiteSpace(topic.TopicArn))
                .Select(topic => GetTopicInfoAsync(topic.TopicArn))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar tópicos SNS");
            yield break;
        }

        await foreach (var topicInfo in ProcessTopicsSafelyAsync(validTopics, cancellationToken))
        {
            yield return topicInfo;
        }
    }

    private async IAsyncEnumerable<SnsTopicInfo> ProcessTopicsSafelyAsync(List<Task<SnsTopicInfo?>> topicTasks, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var topicTask in topicTasks)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var topicInfo = await GetTopicInfoSafelyAsync(topicTask);
            if (topicInfo is not null)
            {
                yield return topicInfo;
            }
        }
    }

    private async Task<SnsTopicInfo?> GetTopicInfoAsync(string topicArn)
    {
        try
        {
            var topicName = topicArn.Split(':').LastOrDefault();
            if (string.IsNullOrWhiteSpace(topicName))
            {
                _logger.LogWarning("Nome do tópico não pôde ser extraído do ARN {TopicArn}, ignorando", topicArn);
                return null;
            }

            var subscriptions = await _snsClient.ListSubscriptionsByTopicAsync(topicArn);
            var subscriptionCount = subscriptions?.Subscriptions?.Count ?? 0;
            
            return new SnsTopicInfo(
                topicName,
                topicArn,
                subscriptionCount,
                DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter detalhes do tópico {TopicArn}, ignorando", topicArn);
            return null;
        }
    }

    private async Task<SnsTopicInfo?> GetTopicInfoSafelyAsync(Task<SnsTopicInfo?> topicTask)
    {
        try
        {
            return await topicTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter informações do tópico");
            return null;
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
            
            if (topicInfo is null)
            {
                var topicName = topicArn.Split(':').Last();
                topicInfo = new SnsTopicInfo(topicName, topicArn, 0, DateTime.UtcNow);
            }

            var response = await _snsClient.ListSubscriptionsByTopicAsync(topicArn);
            
            if (response is not { Subscriptions: { } subscriptionsList })
                return new OperationResult<SnsTopicDetails>(true, new SnsTopicDetails(topicInfo, []));

            var subscriptions = subscriptionsList
                .Where(sub => sub is not null)
                .Select(sub => new SnsSubscriptionInfo(
                    sub.SubscriptionArn ?? string.Empty,
                    sub.Protocol ?? string.Empty,
                    sub.Endpoint ?? string.Empty,
                    !string.IsNullOrEmpty(sub.SubscriptionArn) && sub.SubscriptionArn != "PendingConfirmation",
                    sub.Owner ?? string.Empty
                )).ToList();

            return new OperationResult<SnsTopicDetails>(true, new SnsTopicDetails(topicInfo, subscriptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do tópico {TopicArn}", topicArn);
            var topicName = topicArn.Split(':').Last();
            var defaultTopic = new SnsTopicInfo(topicName, topicArn, 0, DateTime.UtcNow);
            return new OperationResult<SnsTopicDetails>(false, new SnsTopicDetails(defaultTopic, []), ex.Message, ex);
        }
    }

    public async Task<OperationResult<IReadOnlyList<SnsSubscriptionInfo>>> GetSubscriptionsAsync(string topicArn)
    {
        try
        {
            var response = await _snsClient.ListSubscriptionsByTopicAsync(topicArn);
            if(response is { Subscriptions: null or { Capacity: 0 } })
                return new OperationResult<IReadOnlyList<SnsSubscriptionInfo>>(true, []);
            
            var subscriptions = response.Subscriptions
                .Where(sub => sub is not null)
                .Select(sub => new SnsSubscriptionInfo(
                    sub.SubscriptionArn ?? string.Empty,
                    sub.Protocol ?? string.Empty,
                    sub.Endpoint ?? string.Empty,
                    !string.IsNullOrEmpty(sub.SubscriptionArn) && sub.SubscriptionArn != "PendingConfirmation",
                    sub.Owner ?? string.Empty
                )).ToList();

            return new OperationResult<IReadOnlyList<SnsSubscriptionInfo>>(true, subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter assinaturas do tópico {TopicArn}", topicArn);
            return new OperationResult<IReadOnlyList<SnsSubscriptionInfo>>(false, [], ex.Message, ex);
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
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> PublishMessageAsync(string topicArn, string message, string? subject = null, Dictionary<string, string>? messageAttributes = null)
    {
        try
        {
            var finalSubject = string.IsNullOrWhiteSpace(subject) 
                ? $"Mensagem SNS - {DateTime.Now:dd/MM/yyyy HH:mm:ss}"
                : subject;

            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                Subject = finalSubject
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
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}

