# 🚀 Quick Start - LocalStack Visualizer

## Execução Rápida com Docker

### 1. Iniciar todos os serviços
```bash
docker-compose up -d
```

### 2. Aguardar inicialização (30-60 segundos)
```bash
# Verificar status
docker-compose logs -f localstack
```

### 3. Acessar aplicação
- **Visualizador**: http://localhost:8080
- **LocalStack**: http://localhost:4566

### 4. Criar dados de exemplo
```bash
# Linux/Mac
bash scripts/init-localstack.sh

# Windows PowerShell
.\scripts\init-localstack.ps1
```

## Execução Local (Desenvolvimento)

### 1. Iniciar LocalStack
```bash
docker run --rm -it -p 4566:4566 -e SERVICES=s3,sqs,sns localstack/localstack
```

### 2. Executar aplicação
```bash
cd AwsLocalStackVisualizer
dotnet run
```

### 3. Acessar
- **HTTPS**: https://localhost:7000
- **HTTP**: http://localhost:5000

## Comandos Úteis

### Docker
```bash
# Parar serviços
docker-compose down

# Ver logs
docker-compose logs -f visualizer
docker-compose logs -f localstack

# Rebuild
docker-compose up --build -d
```

### LocalStack CLI
```bash
# Status dos serviços
curl http://localhost:4566/_localstack/health

# Listar buckets S3
aws --endpoint-url=http://localhost:4566 s3 ls

# Listar filas SQS
aws --endpoint-url=http://localhost:4566 sqs list-queues

# Listar tópicos SNS
aws --endpoint-url=http://localhost:4566 sns list-topics
```

## Funcionalidades Disponíveis

### ✅ Dashboard
- Visão geral de todos os serviços
- Status de conectividade
- Contagem de recursos

### ✅ S3 Browser
- Listar buckets
- Navegar objetos
- Visualizar conteúdo de arquivos
- Informações de tamanho e data

### ✅ SQS Manager
- Listar filas
- Visualizar mensagens
- Contadores em tempo real
- Formatação JSON automática

### ✅ SNS Viewer
- Listar tópicos
- Detalhes de assinaturas
- Status de confirmação
- Estatísticas por protocolo

## Configuração

Edite `AwsLocalStackVisualizer/appsettings.json`:

```json
{
  "LocalStack": {
    "ServiceUrl": "http://localhost:4566",
    "Region": "us-east-1",
    "Services": {
      "S3": { "Enabled": true },
      "SQS": { "Enabled": true },
      "SNS": { "Enabled": true }
    }
  }
}
```

## Troubleshooting

### LocalStack não inicia
```bash
# Verificar portas em uso
netstat -an | grep 4566

# Limpar containers
docker-compose down -v
docker system prune -f
```

### Aplicação não conecta
1. Verificar se LocalStack está rodando
2. Verificar URL de conexão no appsettings.json
3. Verificar logs: `docker-compose logs visualizer`

### Dados não aparecem
1. Criar recursos de exemplo com os scripts
2. Verificar se serviços estão habilitados na configuração
3. Atualizar página no visualizador

---

**🎉 Pronto para usar!** Acesse http://localhost:8080 e explore seus recursos LocalStack!







