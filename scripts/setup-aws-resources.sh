#!/bin/bash

# Script para configurar todos os recursos AWS no LocalStack
# Execute este script após o LocalStack estar rodando

set -e

ENDPOINT_URL="http://localhost:4566"
AWS_REGION="us-west-2"

echo "Configurando recursos AWS no LocalStack..."
echo ""

# Aguardar LocalStack estar pronto
echo "Aguardando LocalStack estar pronto..."
until curl -s $ENDPOINT_URL/_localstack/health | grep -q '"s3": "available"'; do
    echo "Aguardando LocalStack inicializar..."
    sleep 2
done
echo "LocalStack está pronto!"
echo ""

# Configurar buckets S3
echo "Criando buckets S3..."
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://sample-bucket-1 --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://sample-bucket-2 --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://logs-bucket --region $AWS_REGION

echo "Adicionando arquivos de exemplo ao S3..."
echo "Hello World from S3!" | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://sample-bucket-1/hello.txt
echo '{"message": "Sample JSON file", "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}' | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://sample-bucket-1/data.json
echo "Application log entry - $(date)" | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://logs-bucket/app.log
echo "Error log entry - $(date)" | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://logs-bucket/error.log
echo ""

# Configurar filas SQS
echo "Criando filas SQS..."
aws --endpoint-url=$ENDPOINT_URL sqs create-queue \
  --queue-name sample-queue \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sqs create-queue \
  --queue-name sample-dlq \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sqs create-queue \
  --queue-name orders-queue \
  --attributes '{
    "RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:'$AWS_REGION':000000000000:sample-dlq\",\"maxReceiveCount\":3}",
    "VisibilityTimeoutSeconds": "30",
    "MessageRetentionPeriod": "1209600"
  }' \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sqs create-queue \
  --queue-name notifications-queue \
  --region $AWS_REGION

echo "Enviando mensagens de exemplo para SQS..."
aws --endpoint-url=$ENDPOINT_URL sqs send-message \
  --queue-url $ENDPOINT_URL/000000000000/sample-queue \
  --message-body "Hello from SQS - Sample message" \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sqs send-message \
  --queue-url $ENDPOINT_URL/000000000000/orders-queue \
  --message-body '{"orderId": "12345", "customerId": "user123", "amount": 99.99, "status": "pending", "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}' \
  --message-attributes '{
    "OrderType": {"StringValue": "online", "DataType": "String"},
    "Priority": {"StringValue": "high", "DataType": "String"}
  }' \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sqs send-message \
  --queue-url $ENDPOINT_URL/000000000000/orders-queue \
  --message-body '{"orderId": "12346", "customerId": "user456", "amount": 149.99, "status": "processing", "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}' \
  --message-attributes '{
    "OrderType": {"StringValue": "store", "DataType": "String"},
    "Priority": {"StringValue": "normal", "DataType": "String"}
  }' \
  --region $AWS_REGION
echo ""

# Configurar tópicos SNS
echo "Criando tópicos SNS..."
aws --endpoint-url=$ENDPOINT_URL sns create-topic \
  --name order-notifications \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns create-topic \
  --name user-events \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns create-topic \
  --name system-alerts \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns create-topic \
  --name payment-events \
  --region $AWS_REGION

echo "Criando assinaturas SNS..."
aws --endpoint-url=$ENDPOINT_URL sns subscribe \
  --topic-arn arn:aws:sns:$AWS_REGION:000000000000:order-notifications \
  --protocol sqs \
  --notification-endpoint arn:aws:sqs:$AWS_REGION:000000000000:orders-queue \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns subscribe \
  --topic-arn arn:aws:sns:$AWS_REGION:000000000000:user-events \
  --protocol email \
  --notification-endpoint user@example.com \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns subscribe \
  --topic-arn arn:aws:sns:$AWS_REGION:000000000000:system-alerts \
  --protocol sqs \
  --notification-endpoint arn:aws:sqs:$AWS_REGION:000000000000:notifications-queue \
  --region $AWS_REGION

echo "Publicando mensagens de teste no SNS..."
aws --endpoint-url=$ENDPOINT_URL sns publish \
  --topic-arn arn:aws:sns:$AWS_REGION:000000000000:order-notifications \
  --message "New order received: #12345 - Amount: $99.99" \
  --subject "Order Notification" \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns publish \
  --topic-arn arn:aws:sns:$AWS_REGION:000000000000:system-alerts \
  --message "System health check completed successfully - All services operational" \
  --subject "Health Check Alert" \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL sns publish \
  --topic-arn arn:aws:sns:$AWS_REGION:000000000000:payment-events \
  --message "Payment processed successfully for order #12345" \
  --subject "Payment Confirmation" \
  --region $AWS_REGION
echo ""

# Configurar secrets no Secrets Manager
echo "Criando secrets no Secrets Manager..."
aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret \
  --name database-credentials \
  --secret-string '{"username": "admin", "password": "super-secret-password", "host": "localhost", "port": 5432, "database": "myapp", "connection_timeout": 30}' \
  --description "Database connection credentials for main application" \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret \
  --name api-keys \
  --secret-string '{"stripe": "sk_test_123456789abcdef", "sendgrid": "SG.abc123def456ghi789", "jwt_secret": "my-super-secure-jwt-secret-key-2024", "encryption_key": "aes256-encryption-key"}' \
  --description "External API keys and authentication tokens" \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret \
  --name app-config \
  --secret-string '{"debug_mode": true, "max_connections": 100, "timeout": 30, "cache_ttl": 3600, "feature_flags": {"new_ui": true, "beta_features": false, "advanced_analytics": true}}' \
  --description "Application configuration and feature flags" \
  --region $AWS_REGION

aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret \
  --name redis-config \
  --secret-string '{"host": "redis.example.com", "port": 6379, "password": "redis-secret-password", "database": 0, "ssl": true}' \
  --description "Redis cache configuration" \
  --region $AWS_REGION
echo ""

echo "Configuração concluída com sucesso!"
echo ""
echo "Recursos criados:"
echo ""
echo "S3 Buckets:"
echo "  - sample-bucket-1 (com hello.txt e data.json)"
echo "  - sample-bucket-2 (vazio)"
echo "  - logs-bucket (com app.log e error.log)"
echo ""
echo "SQS Queues:"
echo "  - sample-queue (1 mensagem)"
echo "  - sample-dlq (dead letter queue)"
echo "  - orders-queue (2 mensagens, com DLQ configurada)"
echo "  - notifications-queue (para alertas do sistema)"
echo ""
echo "SNS Topics:"
echo "  - order-notifications (conectado ao orders-queue)"
echo "  - user-events (assinatura por email)"
echo "  - system-alerts (conectado ao notifications-queue)"
echo "  - payment-events"
echo ""
echo "Secrets Manager:"
echo "  - database-credentials"
echo "  - api-keys"
echo "  - app-config"
echo "  - redis-config"
echo ""
echo "Acesse o visualizador em: http://localhost:8080"
echo "LocalStack dashboard: $ENDPOINT_URL/_localstack/health"
