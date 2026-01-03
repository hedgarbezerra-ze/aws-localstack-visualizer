# Script para executar AWS LocalStack Visualizer com perfil LocalStack
Write-Host "Executando AWS LocalStack Visualizer com perfil LocalStack..." -ForegroundColor Green
Write-Host ""

# Navegar para o diretório do projeto
Set-Location "C:\Users\99846737\RiderProjects\aws-localstack-visualizer"

# Executar o projeto com perfil LocalStack
dotnet run --project "AwsLocalStackVisualizer\AwsLocalStackVisualizer.csproj" --launch-profile LocalStack

Read-Host "Pressione Enter para continuar"

