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

### 4. `aws-token.sh` (Token de Autenticação AWS)
- **Extrai token de autenticação AWS SSO atual**
- Busca dinamicamente o arquivo de token mais recente
- Exibe informações do token (não salva em arquivo)
- Funciona com perfis AWS SSO configurados

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

### Opção 4: Token de Autenticação AWS
```bash
# Extrair token SSO atual
./scripts/aws-token.sh

# Exportar diretamente
aws configure export-credentials --profile clube-do-ze-dev.clube-do-ze-backend --format env

# Verificar identidade
aws sts get-caller-identity --profile SEU_PERFIL

# Renovar token quando expirar
aws sso login --profile SEU_PERFIL
```

## Script de Token AWS

### Funcionalidades
- **Busca Dinâmica**: Encontra automaticamente o arquivo de token mais recente
- **Filtragem Inteligente**: Exclui arquivos `botocore-client-id` e procura apenas tokens válidos
- **Compatibilidade**: Funciona no Windows (Git Bash), Linux e Mac
- **Extração Robusta**: Extrai token, data de expiração, região e URL

### Como Funciona
1. Procura em `~/.aws/sso/cache/`
2. Filtra arquivos que contêm `accessToken`
3. Exclui arquivos `botocore-client-id-*`
4. Seleciona o arquivo mais recente
5. Extrai e exibe informações do token

### Exemplo de Uso
```bash
# Executar o script
./scripts/aws-token.sh

# Saída esperada:
# === Token AWS SSO Atual ===
# 📄 Arquivo encontrado: /c/Users/usuario/.aws/sso/cache/abc123.json
# 🔑 Access Token: aoaAAAAA...
# ⏰ Expira em: 2025-10-09T02:37:53Z
# 🌍 Região: us-west-2
# ✅ Token válido
```

## Pré-requisitos

- Docker e Docker Compose
- AWS CLI (para execução manual)
- LocalStack rodando na porta 4567
- **Para Token AWS**: Perfil AWS SSO configurado

## Comandos Úteis para Token AWS

### Verificação de Autenticação
```bash
# Verificar identidade atual
aws sts get-caller-identity --profile SEU_PERFIL

# Listar perfis disponíveis
aws configure list-profiles

# Verificar configuração atual
aws configure list
```

### Gerenciamento de Token
```bash
# Renovar token SSO
aws sso login --profile SEU_PERFIL

# Verificar se token é válido
./scripts/aws-token.sh
```

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
