using Amazon.SQS;
using Amazon.SQS.Model;
using AwsLocalStackVisualizer.Abstractions;
using AwsLocalStackVisualizer.Configuration;
using AwsLocalStackVisualizer.Models.Common;
using AwsLocalStackVisualizer.Models.SQS;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AwsLocalStackVisualizer.Services.AWS;

public class SqsService : ISqsService
{
    private readonly IAppAwsClients _awsClients;
    private readonly ILogger<SqsService> _logger;
    private readonly INotificationService _notificationService;

    public SqsService(IAppAwsClients awsClients, ILogger<SqsService> logger, INotificationService notificationService)
    {
        _awsClients = awsClients ?? throw new ArgumentNullException(nameof(awsClients));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<OperationResult<IReadOnlyList<SqsQueueInfo>>> GetQueuesAsync()
    {
        try
        {
            var response = await _awsClients.SQS.ListQueuesAsync(new ListQueuesRequest());
            
            if (response is not { QueueUrls: { } queueUrlsList })
            {
                _logger.LogWarning("Resposta do SQS ListQueues é nula ou não contém URLs de filas");
                return new OperationResult<IReadOnlyList<SqsQueueInfo>>(true, []);
            }

            var queueTasks = new List<Task<SqsQueueInfo?>>();

            foreach (var queueUrl in queueUrlsList)
            {
                if (string.IsNullOrWhiteSpace(queueUrl))
                {
                    _logger.LogWarning("URL de fila inválida encontrada, ignorando");
                    continue;
                }

                queueTasks.Add(GetQueueInfoAsync(queueUrl));
            }

            var queueResults = await Task.WhenAll(queueTasks);
            var queues = queueResults.Where(q => q is not null).Cast<SqsQueueInfo>().ToList();

            return new OperationResult<IReadOnlyList<SqsQueueInfo>>(true, queues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar filas SQS");
            return new OperationResult<IReadOnlyList<SqsQueueInfo>>(false, [], ex.Message, ex);
        }
    }

    public async IAsyncEnumerable<SqsQueueInfo> GetQueuesStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<Task<SqsQueueInfo?>> validQueues;
        try
        {
            var response = await _awsClients.SQS.ListQueuesAsync(new ListQueuesRequest());
            
            if (response is not { QueueUrls: { } queueUrlsList })
            {
                _logger.LogWarning("Resposta do SQS ListQueues é nula ou não contém URLs de filas");
                yield break;
            }

            validQueues = queueUrlsList
                .Where(queueUrl => !string.IsNullOrWhiteSpace(queueUrl))
                .Select(queueUrl => GetQueueInfoAsync(queueUrl))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar filas SQS");
            yield break;
        }

        await foreach (var queueInfo in ProcessQueuesSafelyAsync(validQueues, cancellationToken))
        {
            yield return queueInfo;
        }
    }

    private async IAsyncEnumerable<SqsQueueInfo> ProcessQueuesSafelyAsync(List<Task<SqsQueueInfo?>> queueTasks, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var queueTask in queueTasks)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var queueInfo = await GetQueueInfoSafelyAsync(queueTask);
            if (queueInfo is not null)
            {
                yield return queueInfo;
            }
        }
    }

    private async Task<SqsQueueInfo?> GetQueueInfoAsync(string queueUrl)
    {
        try
        {
            var queueName = queueUrl.Split('/').LastOrDefault();
            if (string.IsNullOrWhiteSpace(queueName))
            {
                _logger.LogWarning("Nome de fila não pôde ser extraído da URL {QueueUrl}, ignorando", queueUrl);
                return null;
            }

            var attributes = await _awsClients.SQS.GetQueueAttributesAsync(queueUrl, new List<string> 
            { 
                "ApproximateNumberOfMessages", 
                "ApproximateNumberOfMessagesNotVisible",
                "CreatedTimestamp"
            });

            if (attributes is not { Attributes: { } attributeDict })
            {
                _logger.LogWarning("Atributos da fila {QueueName} não puderam ser obtidos, usando valores padrão", queueName);
                return new SqsQueueInfo(queueName, queueUrl, 0, 0, DateTime.MinValue);
            }

            var messageCount = int.TryParse(attributeDict.GetValueOrDefault("ApproximateNumberOfMessages", "0"), out var msgCount) ? msgCount : 0;
            var notVisibleCount = int.TryParse(attributeDict.GetValueOrDefault("ApproximateNumberOfMessagesNotVisible", "0"), out var notVisCount) ? notVisCount : 0;
            var createdTimestamp = long.TryParse(attributeDict.GetValueOrDefault("CreatedTimestamp", "0"), out var timestamp) && timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime
                : DateTime.MinValue;

            return new SqsQueueInfo(queueName, queueUrl, messageCount, notVisibleCount, createdTimestamp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter detalhes da fila {QueueUrl}, ignorando", queueUrl);
            return null;
        }
    }

    private async Task<SqsQueueInfo?> GetQueueInfoSafelyAsync(Task<SqsQueueInfo?> queueTask)
    {
        try
        {
            return await queueTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter informações da fila");
            return null;
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
                return new OperationResult<SqsQueueDetails>(true, new SqsQueueDetails(defaultQueue, []));
            }

            var messages = new List<SqsMessageInfo>();
            var response = await _awsClients.SQS.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueInfo.Url,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 1,
                MessageSystemAttributeNames = new List<string> { "All" },
                MessageAttributeNames = new List<string> { "All" }
            });
            
            if (response is { Messages: null  or { Count: 0 } })
                return new OperationResult<SqsQueueDetails>(true, 
                    new SqsQueueDetails(queueInfo, []),
                    "Fila '{queueName}' está vazia ou não pôde ser lida");

            foreach (var message in response.Messages)
            {
                if (message is null or { MessageId: null or "" })
                {
                    _logger.LogWarning("Mensagem inválida encontrada na fila {QueueName}, ignorando", queueName);
                    continue;
                }

                var sentTimestamp = message.Attributes?.GetValueOrDefault("SentTimestamp") is { } sentStr && long.TryParse(sentStr, out var sentLong)
                    ? DateTime.UnixEpoch.AddMilliseconds(sentLong)
                    : DateTime.UnixEpoch;

                var receiveCount = message.Attributes?.GetValueOrDefault("ApproximateReceiveCount") is { } countStr && int.TryParse(countStr, out var count)
                    ? count
                    : 0;

                messages.Add(new SqsMessageInfo(
                    message.MessageId,
                    message.Body ?? string.Empty,
                    message.ReceiptHandle ?? string.Empty,
                    sentTimestamp,
                    receiveCount,
                    message.Attributes ?? new Dictionary<string, string>()
                ));
            }

            return new OperationResult<SqsQueueDetails>(true, new SqsQueueDetails(queueInfo, messages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes da fila {QueueName}", queueName);
            var defaultQueue = new SqsQueueInfo(queueName, "", 0, 0, DateTime.MinValue);
            return new OperationResult<SqsQueueDetails>(false, new SqsQueueDetails(defaultQueue, []), ex.Message, ex);
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

            var response = await _awsClients.SQS.CreateQueueAsync(request);
            
            _logger.LogInformation("Fila SQS {QueueName} criada com sucesso", queueName);
            _notificationService.ShowSuccess($"Fila '{queueName}' criada com sucesso!");
            return new OperationResult<string>(true, response.QueueUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fila SQS {QueueName}", queueName);
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

            var response = await _awsClients.SQS.SendMessageAsync(request);
            
            _logger.LogInformation("Mensagem enviada para fila SQS {QueueUrl}", queueUrl);
            _notificationService.ShowSuccess("Mensagem enviada com sucesso!");
            return new OperationResult<string>(true, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem para fila SQS {QueueUrl}", queueUrl);
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> DeleteQueueAsync(string queueUrl)
    {
        try
        {
            await _awsClients.SQS.DeleteQueueAsync(queueUrl);
            
            _logger.LogInformation("Fila SQS {QueueUrl} excluída com sucesso", queueUrl);
            _notificationService.ShowSuccess("Fila excluída com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir fila SQS {QueueUrl}", queueUrl);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> PurgeQueueAsync(string queueUrl)
    {
        try
        {
            await _awsClients.SQS.PurgeQueueAsync(queueUrl);
            
            _logger.LogInformation("Fila SQS {QueueUrl} limpa com sucesso", queueUrl);
            _notificationService.ShowSuccess("Fila limpa com sucesso!");
            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar fila SQS {QueueUrl}", queueUrl);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }

    public async Task<OperationResult<string>> GetQueueArnAsync(string queueUrl)
    {
        try
        {
            var response = await _awsClients.SQS.GetQueueAttributesAsync(queueUrl, new List<string> { "QueueArn" });
            if (response.Attributes != null &&
                response.Attributes.TryGetValue("QueueArn", out var arn) &&
                !string.IsNullOrWhiteSpace(arn))
            {
                return new OperationResult<string>(true, arn);
            }

            return new OperationResult<string>(false, null, "QueueArn não encontrado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter ARN da fila {QueueUrl}", queueUrl);
            return new OperationResult<string>(false, null, ex.Message, ex);
        }
    }

    public async Task<OperationResult<bool>> EnsureQueuePolicyAllowsSnsAsync(string queueUrl, string queueArn, string topicArn)
    {
        try
        {
            var current = await _awsClients.SQS.GetQueueAttributesAsync(queueUrl, new List<string> { "Policy" });

            JsonObject root;
            JsonArray statementArray;

            if (current.Attributes != null &&
                current.Attributes.TryGetValue("Policy", out var existingJson) &&
                !string.IsNullOrWhiteSpace(existingJson))
            {
                var parsed = JsonNode.Parse(existingJson);
                root = parsed as JsonObject ?? new JsonObject { ["Version"] = "2012-10-17" };
                statementArray = root["Statement"] switch
                {
                    JsonArray a => a,
                    JsonObject o => new JsonArray(o),
                    _ => new JsonArray()
                };
            }
            else
            {
                root = new JsonObject { ["Version"] = "2012-10-17" };
                statementArray = new JsonArray();
            }

            foreach (var node in statementArray)
            {
                var sourceArn = node?["Condition"]?["ArnEquals"]?["aws:SourceArn"]?.GetValue<string>();
                if (string.Equals(sourceArn, topicArn, StringComparison.Ordinal))
                    return new OperationResult<bool>(true, true);
            }

            var newStatement = new JsonObject
            {
                ["Sid"] = $"sns-{Guid.NewGuid():N}",
                ["Effect"] = "Allow",
                ["Principal"] = new JsonObject { ["Service"] = "sns.amazonaws.com" },
                ["Action"] = "sqs:SendMessage",
                ["Resource"] = queueArn,
                ["Condition"] = new JsonObject
                {
                    ["ArnEquals"] = new JsonObject { ["aws:SourceArn"] = topicArn }
                }
            };

            statementArray.Add(newStatement);
            root["Statement"] = statementArray;

            var policyJson = root.ToJsonString();

            await _awsClients.SQS.SetQueueAttributesAsync(new SetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                Attributes = new Dictionary<string, string> { ["Policy"] = policyJson }
            });

            return new OperationResult<bool>(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao definir política SNS para fila {QueueUrl}", queueUrl);
            return new OperationResult<bool>(false, false, ex.Message, ex);
        }
    }
}
