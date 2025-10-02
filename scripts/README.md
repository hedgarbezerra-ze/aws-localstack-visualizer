# LocalStack Setup Scripts

Este diretório contém scripts para configurar recursos de exemplo no LocalStack para testar o AWS Visualizer.

## Scripts Disponíveis

### 1. `01-setup-resources.sh` (Execução Automática)
- **Executa automaticamente** quando o LocalStack inicia via Docker Compose
- Usa `awslocal` (comando interno do LocalStack)
- Cria todos os recursos de exemplo

### 2. `localstack-setup.sh` (Execução Manual - Linux/Mac)
- Para execução manual em sistemas Unix
- Requer AWS CLI instalado
- Usa endpoint externo `http://localhost:4567`

### 3. `localstack-setup.ps1` (Execução Manual - Windows)
- Para execução manual no Windows PowerShell
- Requer AWS CLI instalado
- Usa endpoint externo `http://localhost:4567`

## Recursos Criados

### 🪣 S3 Buckets
- `sample-bucket-1` - Bucket com arquivos de exemplo
- `sample-bucket-2` - Bucket vazio
- `logs-bucket` - Bucket para logs

### 📬 SQS Queues
- `sample-queue` - Fila simples com mensagem de teste
- `sample-dlq` - Dead Letter Queue
- `orders-queue` - Fila com DLQ configurada e mensagem JSON

### 📢 SNS Topics
- `order-notifications` - Tópico para notificações de pedidos
- `user-events` - Tópico para eventos de usuário
- `system-alerts` - Tópico para alertas do sistema

### 🔐 Secrets Manager
- `database-credentials` - Credenciais do banco de dados
- `api-keys` - Chaves de APIs externas
- `app-config` - Configurações da aplicação

## Como Usar

### Opção 1: Automática (Recomendada)
```bash
docker-compose up
```
Os recursos serão criados automaticamente quando o LocalStack iniciar.

### Opção 2: Manual - Linux/Mac
```bash
# Certifique-se que o LocalStack está rodando
docker-compose up localstack -d

# Execute o script
./scripts/localstack-setup.sh
```

### Opção 3: Manual - Windows
```powershell
# Certifique-se que o LocalStack está rodando
docker-compose up localstack -d

# Execute o script
.\scripts\localstack-setup.ps1
```

## Pré-requisitos

- Docker e Docker Compose
- AWS CLI (para execução manual)
- LocalStack rodando na porta 4567

## Verificação

Após executar os scripts, você pode verificar os recursos:

```bash
# Listar buckets S3
aws --endpoint-url=http://localhost:4567 s3 ls

# Listar filas SQS
aws --endpoint-url=http://localhost:4567 sqs list-queues

# Listar tópicos SNS
aws --endpoint-url=http://localhost:4567 sns list-topics

# Listar secrets
aws --endpoint-url=http://localhost:4567 secretsmanager list-secrets
```

## Acesso ao Visualizer

- **Aplicação**: http://localhost:8080
- **LocalStack Health**: http://localhost:4567/_localstack/health
