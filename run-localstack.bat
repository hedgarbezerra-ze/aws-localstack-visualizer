@echo off
echo Executando AWS LocalStack Visualizer com perfil LocalStack...
echo.

REM Navegar para o diretório do projeto
cd /d "C:\Users\99846737\RiderProjects\aws-localstack-visualizer"

REM Executar o projeto com perfil LocalStack
dotnet run --project "AwsLocalStackVisualizer\AwsLocalStackVisualizer.csproj" --launch-profile LocalStack

pause

