# Use a imagem oficial do .NET 9 SDK para build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar arquivos de projeto e restaurar dependências
COPY AwsLocalStackVisualizer/AwsLocalStackVisualizer.csproj ./
RUN dotnet restore

# Copiar todo o código fonte
COPY AwsLocalStackVisualizer/ ./

# Copiar README.md do diretório raiz para o diretório pai (como esperado pelo .csproj)
# Criar README.md no diretório pai se não existir
RUN if [ -f README.md ]; then cp README.md ../README.md; else echo "# AWS LocalStack Visualizer" > ../README.md; fi

# Build da aplicação
RUN dotnet publish -c Release -o /app/publish

# Use a imagem runtime do .NET 9 para execução
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Instalar dependências necessárias
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=build /app/publish .

# Criar usuário não-root para segurança
RUN useradd -m -s /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

# Expor porta
EXPOSE 8080

# Configurar variáveis de ambiente padrão
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION=1
ENV DOTNET_LOGGING__CONSOLE__DISABLECOLORS=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Comando de inicialização
ENTRYPOINT ["dotnet", "AwsLocalStackVisualizer.dll"]


