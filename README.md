# LocalStack Visualizer

Um visualizador moderno e intuitivo para recursos AWS LocalStack, desenvolvido em .NET 9 Blazor Server.

## 🚀 Características

- **Dashboard Interativo**: Visão geral de todos os serviços AWS habilitados
- **Navegador S3**: Visualize buckets, objetos e conteúdos
- **Gerenciador SQS**: Monitore filas e visualize mensagens
- **Visualizador SNS**: Gerencie tópicos e assinaturas
- **Interface Responsiva**: Design moderno usando Bootstrap
- **Suporte Docker**: Deploy fácil com docker-compose
- **Configuração Flexível**: Todas as configurações via appsettings.json

## 🛠️ Tecnologias Utilizadas

- **.NET 9** - Framework principal
- **Blazor Server** - Interface de usuário interativa
- **AWS SDK** - Integração com serviços AWS
- **Bootstrap 5** - Design responsivo
- **Docker** - Containerização

## 📋 Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) e [Docker Compose](https://docs.docker.com/compose/)
- [LocalStack](https://localstack.cloud/) (incluído no docker-compose)

## 🚀 Início Rápido

### Opção 1: Docker Compose (Recomendado)

1. Clone o repositório:
```bash
git clone <repository-url>
cd aws-localstack-visualizer
```

2. Inicie os serviços:
```bash
docker-compose up -d
```

3. Acesse a aplicação:
- **Visualizador**: http://localhost:8080
- **LocalStack**: http://localhost:4566

### Opção 2: Desenvolvimento Local

1. Clone o repositório:
```bash
git clone <repository-url>
cd aws-localstack-visualizer/AwsLocalStackVisualizer
```

2. Inicie o LocalStack separadamente:
```bash
docker run --rm -it -p 4566:4566 -e SERVICES=s3,sqs,sns localstack/localstack
```

3. Execute a aplicação:
```bash
dotnet run
```

4. Acesse: https://localhost:7000 ou http://localhost:5000

## ⚙️ Configuração

### appsettings.json

```json
{
  "LocalStack": {
    "ServiceUrl": "http://localhost:4566",
    "Region": "us-east-1",
    "AccessKey": "test",
    "SecretKey": "test",
    "Services": {
      "S3": { "Enabled": true },
      "SQS": { "Enabled": true },
      "SNS": { "Enabled": true }
    }
  }
}
```

### Variáveis de Ambiente (Docker)

```bash
LocalStack__ServiceUrl=http://localstack:4566
LocalStack__Region=us-east-1
LocalStack__AccessKey=test
LocalStack__SecretKey=test
LocalStack__Services__S3__Enabled=true
LocalStack__Services__SQS__Enabled=true
LocalStack__Services__SNS__Enabled=true
```

## 📱 Funcionalidades

### Dashboard
- Visão geral de todos os serviços
- Status de conectividade
- Contagem de recursos
- Navegação rápida

### S3 Browser
- Listagem de buckets
- Navegação de objetos
- Visualização de conteúdo
- Informações de tamanho e data

### SQS Manager
- Listagem de filas
- Visualização de mensagens
- Contadores de mensagens
- Formatação JSON automática

### SNS Viewer
- Listagem de tópicos
- Detalhes de assinaturas
- Status de confirmação
- Estatísticas por protocolo

## 🐳 Docker

### Build Manual

```bash
cd AwsLocalStackVisualizer
docker build -t localstack-visualizer .
```

### Executar Container

```bash
docker run -p 8080:8080 \
  -e LocalStack__ServiceUrl=http://host.docker.internal:4566 \
  localstack-visualizer
```

## 🧪 Testando com LocalStack

### Criar recursos de exemplo:

```bash
# S3 Bucket
aws --endpoint-url=http://localhost:4566 s3 mb s3://test-bucket
aws --endpoint-url=http://localhost:4566 s3 cp README.md s3://test-bucket/

# SQS Queue
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name test-queue
aws --endpoint-url=http://localhost:4566 sqs send-message --queue-url http://localhost:4566/000000000000/test-queue --message-body "Hello World"

# SNS Topic
aws --endpoint-url=http://localhost:4566 sns create-topic --name test-topic
aws --endpoint-url=http://localhost:4566 sns subscribe --topic-arn arn:aws:sns:us-east-1:000000000000:test-topic --protocol email --notification-endpoint test@example.com
```

## 🔧 Desenvolvimento

### Estrutura do Projeto

```
AwsLocalStackVisualizer/
├── Components/
│   ├── Dashboard/          # Componentes do dashboard
│   ├── S3/                 # Componentes do S3
│   ├── SQS/                # Componentes do SQS
│   ├── SNS/                # Componentes do SNS
│   └── Shared/             # Componentes compartilhados
├── Configuration/          # Classes de configuração
├── Models/                 # Modelos de dados
├── Services/              # Serviços de negócio
└── Program.cs             # Ponto de entrada
```

### Executar em Desenvolvimento

```bash
dotnet watch run
```

## 🚀 Deploy

### Docker Compose (Produção)

```yaml
version: '3.8'
services:
  visualizer:
    image: localstack-visualizer
    ports:
      - "80:8080"
    environment:
      - LocalStack__ServiceUrl=http://your-localstack:4566
```

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

## 🤝 Contribuição

Contribuições são bem-vindas! Por favor, abra uma issue ou pull request.

## 📞 Suporte

Para suporte e dúvidas, abra uma issue no repositório do projeto.


