#!/bin/bash

# Script para inicializar recursos AWS no LocalStack
# Este script cria recursos de exemplo para o AWS LocalStack Visualizer

set -e

echo "Iniciando configuracao do LocalStack..."

# Verificar se AWS_REGION está definido
if [ -z "$AWS_REGION" ]; then
  echo "AWS_REGION nao definido. Usando us-west-2 como padrao."
  export AWS_REGION="us-west-2"
fi

echo "Regiao configurada: $AWS_REGION"
echo "Endpoint LocalStack: http://localstack:4566"

# Aguardar LocalStack ficar totalmente disponível
echo "Aguardando LocalStack ficar totalmente disponivel..."
sleep 5

# Verificar se os serviços estão respondendo
max_attempts=10
attempt=1

while [ $attempt -le $max_attempts ]; do
    echo "Verificacao $attempt/$max_attempts..."
    
    # Testar cada serviço individualmente
    sns_ok=false
    sqs_ok=false
    s3_ok=false
    secrets_ok=false
    
    if aws --endpoint-url=http://localstack:4566 --region $AWS_REGION sns list-topics > /dev/null 2>&1; then
        sns_ok=true
    fi
    
    if aws --endpoint-url=http://localstack:4566 --region $AWS_REGION sqs list-queues > /dev/null 2>&1; then
        sqs_ok=true
    fi
    
    if aws --endpoint-url=http://localstack:4566 --region $AWS_REGION s3 ls > /dev/null 2>&1; then
        s3_ok=true
    fi
    
    if aws --endpoint-url=http://localstack:4566 --region $AWS_REGION secretsmanager list-secrets > /dev/null 2>&1; then
        secrets_ok=true
    fi
    
    if [ "$sns_ok" = true ] && [ "$sqs_ok" = true ] && [ "$s3_ok" = true ] && [ "$secrets_ok" = true ]; then
        echo "Todos os servicos estao disponiveis!"
        break
    fi
    
    echo "Aguardando servicos ficarem disponiveis... (SNS: $sns_ok, SQS: $sqs_ok, S3: $s3_ok, Secrets: $secrets_ok)"
    sleep 3
    attempt=$((attempt + 1))
done

if [ $attempt -gt $max_attempts ]; then
    echo "Alguns servicos podem nao estar disponiveis, continuando mesmo assim..."
fi

echo ""
echo "Criando recursos AWS..."

# ================================
# S3 BUCKETS
# ================================
echo ""
echo "Criando buckets S3..."

buckets=(
    "rewards-data-bucket"
    "rewards-logs-bucket"
)

for bucket in "${buckets[@]}"; do
    echo "  Criando bucket: $bucket"
    aws --endpoint-url=http://localstack:4566 \
        --region $AWS_REGION \
        s3 mb s3://$bucket || echo "  Bucket $bucket ja existe"
done

# ================================
# SNS TOPICS
# ================================
echo ""
echo "Criando topicos SNS..."

topics=(
    "rewards-notifications-topic"
    "rewards-events-topic"
    "rewards-alerts-topic"
)

declare -A topic_arns

for topic in "${topics[@]}"; do
    echo "  Criando topico: $topic"
    topic_arn=$(aws --endpoint-url=http://localstack:4566 \
        --region $AWS_REGION \
        sns create-topic \
        --name $topic \
        --query 'TopicArn' \
        --output text 2>/dev/null || echo "")
    
    if [ -n "$topic_arn" ]; then
        topic_arns[$topic]=$topic_arn
        echo "    Topico criado com ARN: $topic_arn"
    else
        echo "  Topico $topic ja existe"
        # Obter ARN do tópico existente
        topic_arn=$(aws --endpoint-url=http://localstack:4566 \
            --region $AWS_REGION \
            sns list-topics \
            --query "Topics[?contains(TopicArn, '$topic')].TopicArn" \
            --output text)
        topic_arns[$topic]=$topic_arn
    fi
done

# ================================
# SQS QUEUES
# ================================
echo ""
echo "Criando filas SQS..."

queues=(
    "rewards-processing-queue"
    "rewards-notifications-queue"
    "rewards-deadletter-queue"
)

declare -A queue_urls

for queue in "${queues[@]}"; do
    echo "  Criando fila: $queue"
    queue_url=$(aws --endpoint-url=http://localstack:4566 \
        --region $AWS_REGION \
        sqs create-queue \
        --queue-name $queue \
        --query 'QueueUrl' \
        --output text 2>/dev/null || echo "")
    
    if [ -n "$queue_url" ]; then
        queue_urls[$queue]=$queue_url
        echo "    Fila criada com URL: $queue_url"
    else
        echo "  Fila $queue ja existe"
        # Obter URL da fila existente
        queue_url=$(aws --endpoint-url=http://localstack:4566 \
            --region $AWS_REGION \
            sqs get-queue-url \
            --queue-name $queue \
            --query 'QueueUrl' \
            --output text 2>/dev/null || echo "")
        queue_urls[$queue]=$queue_url
    fi
done

# ================================
# SNS SUBSCRIPTIONS
# ================================
echo ""
echo "Criando subscricoes SNS para SQS..."

# Função para obter ARN da fila a partir da URL
get_queue_arn() {
    local queue_url=$1
    aws --endpoint-url=http://localstack:4566 \
        --region $AWS_REGION \
        sqs get-queue-attributes \
        --queue-url "$queue_url" \
        --attribute-names QueueArn \
        --query 'Attributes.QueueArn' \
        --output text 2>/dev/null
}

# Conectar rewards-notifications-topic com rewards-processing-queue
if [ -n "${topic_arns[rewards-notifications-topic]}" ] && [ -n "${queue_urls[rewards-processing-queue]}" ]; then
    echo "  Conectando rewards-notifications-topic com rewards-processing-queue"
    queue_arn=$(get_queue_arn "${queue_urls[rewards-processing-queue]}")
    if [ -n "$queue_arn" ]; then
        aws --endpoint-url=http://localstack:4566 \
            --region $AWS_REGION \
            sns subscribe \
            --topic-arn "${topic_arns[rewards-notifications-topic]}" \
            --protocol sqs \
            --notification-endpoint "$queue_arn" > /dev/null && echo "    Subscricao criada com sucesso" || echo "    Erro na subscricao"
    else
        echo "    Erro: nao foi possivel obter ARN da fila"
    fi
fi

# Conectar rewards-events-topic com rewards-notifications-queue
if [ -n "${topic_arns[rewards-events-topic]}" ] && [ -n "${queue_urls[rewards-notifications-queue]}" ]; then
    echo "  Conectando rewards-events-topic com rewards-notifications-queue"
    queue_arn=$(get_queue_arn "${queue_urls[rewards-notifications-queue]}")
    if [ -n "$queue_arn" ]; then
        aws --endpoint-url=http://localstack:4566 \
            --region $AWS_REGION \
            sns subscribe \
            --topic-arn "${topic_arns[rewards-events-topic]}" \
            --protocol sqs \
            --notification-endpoint "$queue_arn" > /dev/null && echo "    Subscricao criada com sucesso" || echo "    Erro na subscricao"
    else
        echo "    Erro: nao foi possivel obter ARN da fila"
    fi
fi

# ================================
# SECRETS MANAGER
# ================================
echo ""
echo "Criando secrets..."

echo "  Criando secret: rewards-database-config"
aws --endpoint-url=http://localstack:4566 \
    --region "$AWS_REGION" \
    secretsmanager \
    create-secret \
    --name rewards-database-config \
    --secret-string '{
        "host": "localhost",
        "port": 5432,
        "database": "rewards_db",
        "username": "rewards_user",
        "password": "rewards_pass_123",
        "ssl_mode": "require",
        "connection_timeout": 30
    }' || echo "  Secret rewards-database-config ja existe"

echo "  Criando secret: rewards-api-config"
aws --endpoint-url=http://localstack:4566 \
    --region "$AWS_REGION" \
    secretsmanager \
    create-secret \
    --name rewards-api-config \
    --secret-string '{
        "api_key": "rewards_api_key_xyz789",
        "api_secret": "rewards_secret_abc123",
        "base_url": "https://api.rewards.example.com",
        "timeout": 5000,
        "retry_attempts": 3,
        "rate_limit": 100
    }' || echo "  Secret rewards-api-config ja existe"

# ================================
# VERIFICAÇÃO FINAL
# ================================
echo ""
echo "Verificando recursos criados..."

echo "  Buckets S3:"
aws --endpoint-url=http://localstack:4566 --region $AWS_REGION s3 ls | sed 's/^/    /'

echo "  Topicos SNS:"
aws --endpoint-url=http://localstack:4566 --region $AWS_REGION sns list-topics --query 'Topics[].TopicArn' --output text | tr '\t' '\n' | sed 's/^/    /'

echo "  Filas SQS:"
aws --endpoint-url=http://localstack:4566 --region $AWS_REGION sqs list-queues --query 'QueueUrls[]' --output text | tr '\t' '\n' | sed 's/^/    /'

echo "  Secrets:"
aws --endpoint-url=http://localstack:4566 --region $AWS_REGION secretsmanager list-secrets --query 'SecretList[].Name' --output text | tr '\t' '\n' | sed 's/^/    /'

echo ""
echo "Configuracao do LocalStack concluida com sucesso!"
echo "Acesse o AWS LocalStack Visualizer em: http://localhost:5266"
echo ""
