# AWS LocalStack Visualizer

Uma aplicação Blazor para visualizar e gerenciar recursos AWS tanto no LocalStack quanto na AWS real.

## Características

- **S3 Browser**: Navegação, upload, download e gerenciamento de buckets
- **SQS Manager**: Visualização e gerenciamento de filas
- **SNS Viewer**: Gerenciamento de tópicos e assinaturas
- **Secrets Manager**: Visualização e gerenciamento de secrets
- **Upload Inteligente**: Extensão automática baseada no arquivo
- **Interface Responsiva**: Design moderno com Bootstrap
- **Modo ReadOnly**: Desabilita ações quando conectado à AWS real
- **Página de Ajuda**: Documentação integrada com Markdown


## Funcionalidades

### S3 (Simple Storage Service)
- Listagem de buckets com estatísticas
- Navegação por objetos e pastas
- Upload com extensão automática
- Download de arquivos
- Criação e exclusão de buckets
- Visualização de metadados

### SQS (Simple Queue Service)
- Listagem de filas com métricas
- Visualização de mensagens
- Envio de mensagens de teste
- Gerenciamento de Dead Letter Queues
- Estatísticas em tempo real

### SNS (Simple Notification Service)
- Listagem de tópicos
- Gerenciamento de assinaturas
- Envio de mensagens de teste
- Configuração de endpoints

### Secrets Manager
- Listagem de secrets
- Visualização de valores (mascarados)
- Criação e exclusão de secrets
- Gerenciamento de versões

## Início Rápido

### 1. **Docker (Recomendado para LocalStack)**
```bash
# Build e execução completa
docker-compose up --build

# Ou build individual
docker build -f AwsLocalStackVisualizer/Dockerfile AwsLocalStackVisualizer

docker run -d -p 8081:8080 --name aws-localstack-visualizer aws-localstack-visualizer
```
- **Acesso**: http://localhost:8080 (docker-compose) ou http://localhost:8081 (build individual)
- **LocalStack**: http://localhost:4566
- **Modo Completo**: Todas as ações habilitadas
- **Ambiente Seguro**: Não afeta AWS real
- **Configuração Automática**: Usa `appsettings.Docker.json`

### 2. **Desenvolvimento Local (IDE)**
```bash
# Executar com LocalStack
dotnet run --environment Development
```
- **Acesso**: http://localhost:5266
- **LocalStack**: http://localhost:4566
- **Modo Completo**: Todas as ações habilitadas
- **Ambiente Seguro**: Não afeta AWS real
- **Configuração**: Usa `appsettings.Development.json`

### 3. **AWS Real (Produção)**
```bash
# Executar aplicação
dotnet run --environment Production
```
- **Modo ReadOnly**: Apenas visualização
- **Ambiente Real**: Conecta à AWS real
- **Sem Modificações**: Botões desabilitados
- **Configuração**: Usa `appsettings.Production.json`

## 🐳 Docker - Específico para LocalStack

### **Características do Docker**
- ✅ **Funciona apenas com LocalStack**: Não conecta à AWS real
- ✅ **Configuração isolada**: `appsettings.Docker.json` específico
- ✅ **Logs detalhados**: Serilog configurado para console
- ✅ **Porta flexível**: 8080 (docker-compose) ou 8081 (build individual)
- ✅ **IDE integrado**: Perfil "Docker" no launchSettings.json

### **Limitações do Docker**
- ❌ **Não funciona com AWS real**: Apenas LocalStack
- ❌ **Sem autenticação AWS**: Usa credenciais de teste
- ❌ **Ambiente isolado**: Não acessa recursos AWS externos

### **Quando usar Docker**
- 🎯 **Desenvolvimento com LocalStack**
- 🎯 **Testes isolados**
- 🎯 **Demonstrações**
- 🎯 **Ambiente de desenvolvimento**

### **Quando NÃO usar Docker**
- ❌ **Produção com AWS real**
- ❌ **Acesso a recursos AWS existentes**
- ❌ **Autenticação AWS SSO/STS**

### **Usando Docker na IDE**
1. **Selecione o perfil "Docker"** no launchSettings.json
2. **Configure o LocalStack** para rodar na porta 4566
3. **Execute o projeto** - será buildado e executado no Docker
4. **Acesse**: http://localhost:8081
5. **Logs**: Visíveis no console da IDE

### **Comandos Docker Úteis**
```bash
# Build individual
docker build -f AwsLocalStackVisualizer/Dockerfile AwsLocalStackVisualizer

# Executar com porta específica
docker run -d -p 8081:8080 --name aws-localstack-visualizer aws-localstack-visualizer

# Ver logs
docker logs -f aws-localstack-visualizer

# Parar container
docker stop aws-localstack-visualizer

# Remover container
docker rm aws-localstack-visualizer
```

## Configuração por Ambiente

### **Docker (LocalStack)**
- **Arquivo**: `appsettings.Docker.json`
- **Uso**: Docker build/run ou perfil "Docker" na IDE
- **Modo**: Completo (criar/editar/excluir)
- **URL LocalStack**: `http://host.docker.internal:4566`
- **Logs**: Configurados para console com Serilog

```json
{
  "AWS": {
    "UseLocalStack": true,
    "Region": "us-west-2",
    "ServiceUrl": "http://host.docker.internal:4566",
    "Credentials": {
      "Type": "Basic",
      "AccessKey": "test",
      "SecretKey": "test"
    }
  }
}
```

### **AWS Real (Produção)**
- **Arquivo**: `appsettings.Production.json`
- **Uso**: `dotnet run --environment Production --project AwsLocalStackVisualizer`
- **Modo**: ReadOnly (apenas visualização)
- **Credenciais**: Configuradas manualmente no `appsettings.Production.json`

```json
{
  "AWS": {
    "UseLocalStack": false,
    "Region": "us-west-2",
    "Credentials": {
      "Type": "Session",
      "AccessKey": "ASIA...",
      "SecretKey": "...",
      "SessionToken": "IQoJb3JpZ2luX2VjEC8aCXVzLXdlc3QtMiJHMEUCI..."
    }
  }
}
```

## Docker

### **Execução com Docker**
O projeto inclui configuração completa para Docker com LocalStack:

```bash
# Iniciar todos os serviços
docker-compose up --build

# Acessar aplicação
# http://localhost:8080

# Acessar LocalStack
# http://localhost:4566
```

### **Serviços Docker**
- **aws-localstack-visualizer**: Aplicação .NET 9
- **localstack**: Simulador AWS
- **init-localstack**: Configuração automática de recursos

### **Docker Compose (Recomendado)**
```bash
# Execução completa
docker-compose up --build

# Com limpeza
docker-compose down -v
```

### **Comandos Úteis**
```bash
# Ver logs
docker-compose logs -f

# Parar serviços
docker-compose down

# Reiniciar
docker-compose restart

# Status
docker-compose ps
```

## Configuração Manual para AWS Real

### **Credenciais AWS**
**Uso**: Configurar credenciais para ambiente de produção

```bash
# 1. Verificar perfis disponíveis
aws configure list-profiles

# 2. Configurar credenciais
aws configure

# 3. Executar aplicação
dotnet run --environment Production
```

**Configuração manual:**
- Configure credenciais AWS via `aws configure`
- Ou edite diretamente o `appsettings.Production.json`
- Execute a aplicação com ambiente Production

### **LocalStack Setup**
**Uso**: Configurar recursos de exemplo no LocalStack

```bash
# 1. Executar LocalStack
docker-compose up localstack -d

# 2. Verificar recursos
aws --endpoint-url=http://localhost:4566 s3 ls
```

**Recursos criados automaticamente:**
- **S3**: Buckets com arquivos de exemplo
- **SQS**: Filas com mensagens de teste
- **SNS**: Tópicos com assinaturas
- **Secrets**: Secrets com valores de exemplo

## Autenticação AWS

### **Para LocalStack (Desenvolvimento)**
```json
{
  "AWS": {
    "UseLocalStack": true,
    "Credentials": {
      "Type": "Basic",
      "AccessKey": "test",
      "SecretKey": "test"
    }
  }
}
```

### **Para AWS Real (Produção)**
```bash
# 1. Configurar credenciais AWS
aws configure

# 2. Editar appsettings.Production.json
{
  "AWS": {
    "UseLocalStack": false,
    "Credentials": {
      "Type": "Session",
      "AccessKey": "ASIA...",
      "SecretKey": "...",
      "SessionToken": "IQoJb3JpZ2luX2VjEC8aCXVzLXdlc3QtMiJHMEUCI..."
    }
  }
}
```

### **Tipos de Credenciais Suportadas**

| Tipo | Uso | Ambiente | Modo |
|------|-----|----------|------|
| **Basic** | LocalStack | Desenvolvimento | Completo |
| **Session** | AWS Real | Produção | ReadOnly |
| **Anonymous** | LocalStack | Teste | Completo |
| **Default** | AWS Real | Desenvolvimento | ReadOnly |

## Docker Compose (Recomendado)

### Opção 1: LocalStack + Visualizer

```yaml
version: '3.8'
services:
  localstack:
    image: localstack/localstack:latest
    ports:
      - "4566:4566"
    environment:
      - SERVICES=s3,sqs,sns,secretsmanager
      - DEBUG=1
      - DATA_DIR=/tmp/localstack/data
    volumes:
      - "./tmp/localstack:/tmp/localstack"
      - "/var/run/docker.sock:/var/run/docker.sock"

  visualizer:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=LocalStack
    depends_on:
      - localstack
```

### Opção 2: Apenas LocalStack

```yaml
version: '3.8'
services:
  localstack:
    image: localstack/localstack:latest
    ports:
      - "4566:4566"
    environment:
      - SERVICES=s3,sqs,sns,secretsmanager
      - DEBUG=1
      - DATA_DIR=/tmp/localstack/data
    volumes:
      - "./tmp/localstack:/tmp/localstack"
      - "/var/run/docker.sock:/var/run/docker.sock"
```

## Execução

### **1. LocalStack (Desenvolvimento)**
```bash
# Opção A: Apenas aplicação
dotnet run --environment LocalStack

# Opção B: Com Docker Compose
docker-compose up
```

**Características:**
- **Modo Completo**: Criar, editar, excluir recursos
- **Ambiente Seguro**: Não afeta AWS real
- **Dados de Exemplo**: Scripts criam recursos automaticamente

### **2. AWS Real (Produção)**
```bash
# 1. Configurar credenciais AWS
aws configure

# 2. Atualizar appsettings.Production.json

# 3. Executar aplicação
dotnet run --environment Production
```

**Características:**
- **Modo ReadOnly**: Apenas visualização
- **Ambiente Real**: Conecta à AWS real
- **Sem Modificações**: Botões desabilitados para segurança

### **3. Docker Compose**
```bash
# LocalStack + Visualizer
docker-compose up

# Apenas LocalStack
docker-compose up localstack
```

## Recursos Criados Automaticamente

### **LocalStack (Desenvolvimento)**
O docker-compose cria automaticamente:

#### **S3 Buckets**
- `sample-bucket-1` - Com arquivos de exemplo
- `sample-bucket-2` - Bucket vazio  
- `logs-bucket` - Para logs

#### **SQS Queues**
- `sample-queue` - Fila simples
- `sample-dlq` - Dead Letter Queue
- `orders-queue` - Com DLQ configurada

#### **SNS Topics**
- `order-notifications` - Notificações de pedidos
- `user-events` - Eventos de usuário
- `system-alerts` - Alertas do sistema

#### **Secrets Manager**
- `database-credentials` - Credenciais do banco
- `api-keys` - Chaves de APIs
- `app-config` - Configurações da aplicação

### **AWS Real (Produção)**
- **Recursos**: Existentes na sua conta AWS
- **Modo**: Apenas visualização (ReadOnly)
- **Segurança**: Botões de modificação desabilitados

## Estrutura do Projeto

```
AwsLocalStackVisualizer/
├── Components/           # Componentes Blazor
│   ├── Dashboard/        # Dashboard principal
│   ├── Layout/          # Layout e navegação
│   ├── Pages/           # Páginas da aplicação
│   ├── S3/              # Componentes S3
│   ├── SQS/             # Componentes SQS
│   ├── SNS/             # Componentes SNS
│   ├── SecretsManager/  # Componentes Secrets Manager
│   └── Shared/          # Componentes compartilhados
├── Services/            # Serviços de negócio
├── Configuration/       # Configurações e factories
├── Models/              # Modelos de dados
├── Abstractions/        # Interfaces
├── Validators/          # Validações
├── Handlers/            # Handlers de exceção
└── Extensions/          # Extensões
```

## Acessos

### **Aplicação**
- **URL**: http://localhost:8080
- **Página de Ajuda**: http://localhost:8080/help
- **Dashboard**: http://localhost:8080

### **LocalStack**
- **Health Check**: http://localhost:4566/_localstack/health
- **Porta**: 4566 (S3, SQS, SNS, SecretsManager)

### **Recursos AWS**
- **S3**: Buckets e objetos
- **SQS**: Filas e mensagens  
- **SNS**: Tópicos e assinaturas
- **Secrets Manager**: Secrets e versões

## Comandos Úteis

### Verificar Conexão AWS

```bash
# Verificar identidade
aws sts get-caller-identity --profile seu-perfil

# Listar perfis
aws configure list-profiles

# Renovar token SSO
aws sso login --profile seu-perfil
```

### Verificar LocalStack

```bash
# Health check
curl http://localhost:4566/_localstack/health

# Listar buckets
aws --endpoint-url=http://localhost:4566 s3 ls

# Listar filas
aws --endpoint-url=http://localhost:4566 sqs list-queues
```

## Troubleshooting

### **Problemas Comuns**

#### **Token Expirado (AWS Real)**
- **Problema**: `The security token included in the request is invalid`
- **Solução**: 
  1. Renove o token: `aws sso login --profile seu-perfil`
  2. Configure credenciais: `aws configure`
  3. Atualize o `appsettings.Production.json`

#### **Credenciais Inválidas**
- **Problema**: `The AWS Access Key Id you provided does not exist in our records`
- **Solução**: 
  1. Verifique se o perfil existe: `aws configure list-profiles`
  2. Configure credenciais: `aws configure`
  3. Copie as credenciais corretas para `appsettings.Production.json`

#### **LocalStack Não Conecta**
- **Problema**: Erro de conexão com LocalStack
- **Solução**: 
  1. Verifique se `UseLocalStack: true`
  2. Verifique se LocalStack está rodando: `docker-compose up localstack`
  3. Teste: `curl http://localhost:4566/_localstack/health`

#### **Configuração AWS Não Funciona**
- **Problema**: `Credenciais inválidas ou expiradas`
- **Solução**: 
  1. Execute: `aws sso login --profile seu-perfil`
  2. Aguarde o login no navegador
  3. Configure: `aws configure`

#### **Modo ReadOnly Ativado**
- **Problema**: Botões desabilitados na interface
- **Solução**: 
  - **Normal**: Quando `UseLocalStack: false` (AWS real)
  - **Para testar**: Use `UseLocalStack: true` (LocalStack)

### **Verificações de Saúde**

#### **AWS Real**
```bash
# Verificar identidade
aws sts get-caller-identity --profile seu-perfil

# Testar credenciais
aws sts get-caller-identity
```

#### **LocalStack**
```bash
# Health check
curl http://localhost:4566/_localstack/health

# Testar S3
aws --endpoint-url=http://localhost:4566 s3 ls

# Testar SQS
aws --endpoint-url=http://localhost:4566 sqs list-queues
```

## Fluxos de Trabalho

### **Desenvolvimento (LocalStack)**
```bash
# 1. Iniciar LocalStack
docker-compose up localstack -d

# 2. Criar recursos de exemplo
./scripts/localstack/setup.sh

# 3. Executar aplicação
dotnet run --environment LocalStack

# 4. Acessar: http://localhost:8080
```

**Resultado:**
- **Modo Completo**: Criar, editar, excluir recursos
- **Dados de Exemplo**: Buckets, filas, tópicos, secrets
- **Ambiente Seguro**: Não afeta AWS real

### **Produção (AWS Real)**
```bash
# 1. Configurar credenciais AWS
aws configure

# 2. Atualizar appsettings.Production.json
# (Copiar credenciais do aws configure)

# 3. Executar aplicação
dotnet run --environment Production

# 4. Acessar: http://localhost:8080
```

**Resultado:**
- ⚠️ **Modo ReadOnly**: Apenas visualização
- ⚠️ **Recursos Reais**: Sua conta AWS
- ⚠️ **Segurança**: Botões desabilitados

### **Docker (Completo)**
```bash
# 1. Executar tudo
docker-compose up

# 2. Acessar: http://localhost:8080
```

**Resultado:**
- **LocalStack + Visualizer**: Ambiente completo
- **Recursos Automáticos**: Criados automaticamente
- **Pronto para Usar**: Sem configuração adicional

## Logs

A aplicação usa Serilog para logging estruturado. Logs importantes:

- `[INFO] Processamento SQS concluído. X filas processadas com sucesso`
- `[ERROR] Erro ao obter detalhes da fila, parando processamento`
- `[WARN] Tentativa X/Y falhou para operação, tentando novamente`

## Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para detalhes.