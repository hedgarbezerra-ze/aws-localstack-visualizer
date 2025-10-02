# Script para inicializar recursos de exemplo no LocalStack (Windows)
# Execute este script após o LocalStack estar rodando

Write-Host "🚀 Inicializando recursos de exemplo no LocalStack..." -ForegroundColor Green

$endpoint = "http://localhost:4566"

# Aguardar LocalStack estar pronto
Write-Host "⏳ Aguardando LocalStack estar pronto..." -ForegroundColor Yellow
do {
    try {
        $health = Invoke-RestMethod -Uri "$endpoint/_localstack/health" -ErrorAction SilentlyContinue
        if ($health.s3 -eq "available") {
            break
        }
    } catch {
        # Continua tentando
    }
    Write-Host "Aguardando LocalStack..."
    Start-Sleep -Seconds 2
} while ($true)

Write-Host "✅ LocalStack está pronto!" -ForegroundColor Green

# Criar bucket S3 e adicionar objetos
Write-Host "📦 Criando bucket S3..." -ForegroundColor Cyan
aws --endpoint-url=$endpoint s3 mb s3://exemplo-bucket

Write-Host "📄 Adicionando objetos ao bucket..." -ForegroundColor Cyan
"Olá, mundo!" | aws --endpoint-url=$endpoint s3 cp - s3://exemplo-bucket/hello.txt

$jsonData = @{
    message = "Hello from S3"
    timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

$jsonData | aws --endpoint-url=$endpoint s3 cp - s3://exemplo-bucket/data.json

# Criar fila SQS e adicionar mensagens
Write-Host "📬 Criando fila SQS..." -ForegroundColor Cyan
aws --endpoint-url=$endpoint sqs create-queue --queue-name exemplo-fila

Write-Host "💌 Enviando mensagens para a fila..." -ForegroundColor Cyan
aws --endpoint-url=$endpoint sqs send-message `
    --queue-url "$endpoint/000000000000/exemplo-fila" `
    --message-body "Primeira mensagem de exemplo"

$messageBody = @{
    tipo = "json"
    dados = @{
        usuario = "admin"
        acao = "login"
    }
} | ConvertTo-Json -Compress

aws --endpoint-url=$endpoint sqs send-message `
    --queue-url "$endpoint/000000000000/exemplo-fila" `
    --message-body $messageBody `
    --message-attributes '{\"ContentType\":{\"StringValue\":\"application/json\",\"DataType\":\"String\"}}'

# Criar tópico SNS e assinatura
Write-Host "📢 Criando tópico SNS..." -ForegroundColor Cyan
aws --endpoint-url=$endpoint sns create-topic --name exemplo-topico

Write-Host "📧 Criando assinatura email..." -ForegroundColor Cyan
aws --endpoint-url=$endpoint sns subscribe `
    --topic-arn "arn:aws:sns:us-east-1:000000000000:exemplo-topico" `
    --protocol email `
    --notification-endpoint "admin@exemplo.com"

Write-Host "🔗 Criando assinatura SQS..." -ForegroundColor Cyan
aws --endpoint-url=$endpoint sns subscribe `
    --topic-arn "arn:aws:sns:us-east-1:000000000000:exemplo-topico" `
    --protocol sqs `
    --notification-endpoint "arn:aws:sqs:us-east-1:000000000000:exemplo-fila"

Write-Host ""
Write-Host "🎉 Recursos de exemplo criados com sucesso!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Recursos criados:" -ForegroundColor White
Write-Host "   S3: bucket 'exemplo-bucket' com 2 objetos" -ForegroundColor Gray
Write-Host "   SQS: fila 'exemplo-fila' com 2 mensagens" -ForegroundColor Gray
Write-Host "   SNS: tópico 'exemplo-topico' com 2 assinaturas" -ForegroundColor Gray
Write-Host ""
Write-Host "🌐 Acesse o visualizador em: http://localhost:8080" -ForegroundColor Cyan
Write-Host ""



