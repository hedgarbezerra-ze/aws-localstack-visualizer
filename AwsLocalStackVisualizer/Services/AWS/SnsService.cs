using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SNS;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SnsService : ISnsService
{
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly ILogger<SnsService> _logger;
    private readonly INotificationService _notificationService;
    private readonly HttpClient _httpClient;
    private readonly string _localStackUrl;

    public SnsService(
        AmazonSimpleNotificationServiceClient snsClient,
        ILogger<SnsService> logger,
        INotificationService notificationService,
        HttpClient httpClient,
        IOptions<AwsConfiguration> config)
    {
        _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _localStackUrl = config.Value.ServiceUrl ?? "http://localhost:4566";
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
            var messagesResult = await GetTopicMessagesAsync(topicArn);
            var messages = messagesResult.IsSuccess && messagesResult.Data != null
                ? messagesResult.Data
                : (IReadOnlyList<SnsMessageInfo>)[];

            if (response is not { Subscriptions: { } subscriptionsList })
                return new OperationResult<SnsTopicDetails>(true, new SnsTopicDetails(topicInfo, [], messages, messages.Count));

            var subscriptions = subscriptionsList
                .Where(sub => sub is not null)
                .Select(sub => new SnsSubscriptionInfo(
                    sub.SubscriptionArn ?? string.Empty,
                    sub.Protocol ?? string.Empty,
                    sub.Endpoint ?? string.Empty,
                    !string.IsNullOrEmpty(sub.SubscriptionArn) && sub.SubscriptionArn != "PendingConfirmation",
                    sub.Owner ?? string.Empty
                )).ToList();

            return new OperationResult<SnsTopicDetails>(true, new SnsTopicDetails(topicInfo, subscriptions, messages, messages.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do tópico {TopicArn}", topicArn);
            var topicName = topicArn.Split(':').Last();
            var defaultTopic = new SnsTopicInfo(topicName, topicArn, 0, DateTime.UtcNow);
            return new OperationResult<SnsTopicDetails>(false, new SnsTopicDetails(defaultTopic, [], [], 0), ex.Message, ex);
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

    public async Task<OperationResult<IReadOnlyList<SnsMessageInfo>>> GetTopicMessagesAsync(string topicArn)
    {
        try
        {
            var topicName = topicArn.Split(':').LastOrDefault() ?? string.Empty;
            var encodedArn = Uri.EscapeDataString(topicArn);
            var url = $"{_localStackUrl}/_aws/sns/messages?topicArn={encodedArn}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Endpoint de mensagens SNS não disponível ou sem mensagens para {TopicArn}", topicArn);
                return new OperationResult<IReadOnlyList<SnsMessageInfo>>(true, []);
            }

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content) || content == "{}" || content == "[]")
                return new OperationResult<IReadOnlyList<SnsMessageInfo>>(true, []);

            var messages = ParseSnsMessages(content, topicArn);
            return new OperationResult<IReadOnlyList<SnsMessageInfo>>(true, messages);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Não foi possível acessar endpoint de mensagens SNS para {TopicArn}", topicArn);
            return new OperationResult<IReadOnlyList<SnsMessageInfo>>(true, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter mensagens do tópico {TopicArn}", topicArn);
            return new OperationResult<IReadOnlyList<SnsMessageInfo>>(false, [], ex.Message, ex);
        }
    }

    private List<SnsMessageInfo> ParseSnsMessages(string jsonContent, string topicArn)
    {
        var messages = new List<SnsMessageInfo>();

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("messages", out var messagesArray))
            {
                foreach (var msgElement in messagesArray.EnumerateArray())
                {
                    var message = ParseMessageElement(msgElement, topicArn);
                    if (message != null)
                        messages.Add(message);
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var msgElement in root.EnumerateArray())
                {
                    var message = ParseMessageElement(msgElement, topicArn);
                    if (message != null)
                        messages.Add(message);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var msgElement in property.Value.EnumerateArray())
                        {
                            var message = ParseMessageElement(msgElement, topicArn);
                            if (message != null)
                                messages.Add(message);
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Erro ao parsear JSON de mensagens SNS");
        }

        return messages.OrderByDescending(m => m.Timestamp).ToList();
    }

    private SnsMessageInfo? ParseMessageElement(JsonElement element, string topicArn)
    {
        try
        {
            var messageId = element.TryGetProperty("MessageId", out var idProp)
                ? idProp.GetString() ?? Guid.NewGuid().ToString()
                : element.TryGetProperty("messageId", out var idProp2)
                    ? idProp2.GetString() ?? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString();

            var subject = element.TryGetProperty("Subject", out var subjectProp)
                ? subjectProp.GetString() ?? string.Empty
                : element.TryGetProperty("subject", out var subjectProp2)
                    ? subjectProp2.GetString() ?? string.Empty
                    : string.Empty;

            var messageBody = element.TryGetProperty("Message", out var msgProp)
                ? msgProp.GetString() ?? string.Empty
                : element.TryGetProperty("message", out var msgProp2)
                    ? msgProp2.GetString() ?? string.Empty
                    : element.TryGetProperty("body", out var bodyProp)
                        ? bodyProp.GetString() ?? string.Empty
                        : string.Empty;

            var timestamp = DateTime.UtcNow;
            if (element.TryGetProperty("Timestamp", out var tsProp) || element.TryGetProperty("timestamp", out tsProp))
            {
                if (tsProp.ValueKind == JsonValueKind.String)
                {
                    DateTime.TryParse(tsProp.GetString(), out timestamp);
                }
                else if (tsProp.ValueKind == JsonValueKind.Number)
                {
                    timestamp = DateTimeOffset.FromUnixTimeMilliseconds(tsProp.GetInt64()).UtcDateTime;
                }
            }

            var attributes = new Dictionary<string, string>();
            if (element.TryGetProperty("MessageAttributes", out var attrProp) || element.TryGetProperty("messageAttributes", out attrProp))
            {
                foreach (var attr in attrProp.EnumerateObject())
                {
                    if (attr.Value.TryGetProperty("Value", out var valueProp) || attr.Value.TryGetProperty("value", out valueProp))
                    {
                        attributes[attr.Name] = valueProp.GetString() ?? string.Empty;
                    }
                    else if (attr.Value.ValueKind == JsonValueKind.String)
                    {
                        attributes[attr.Name] = attr.Value.GetString() ?? string.Empty;
                    }
                }
            }

            return new SnsMessageInfo(messageId, topicArn, subject, messageBody, timestamp, attributes);
        }
        catch
        {
            return null;
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

