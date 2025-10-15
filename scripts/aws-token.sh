#!/bin/bash

echo "=== Credenciais AWS SSO Atual ==="

# Verificar se o perfil foi especificado
if [ -z "$1" ]; then
    echo "❌ Uso: $0 <perfil-aws>"
    echo "💡 Exemplo: $0 clube-do-ze-dev.clube-do-ze-backend"
    echo ""
    echo "📋 Perfis disponíveis:"
    aws configure list-profiles
    exit 1
fi

PROFILE="$1"

# Verificar se o perfil existe
if ! aws configure list-profiles | grep -q "^$PROFILE$"; then
    echo "❌ Perfil '$PROFILE' não encontrado"
    echo "💡 Perfis disponíveis:"
    aws configure list-profiles
    exit 1
fi

echo "🔍 Verificando credenciais para o perfil: $PROFILE"

# Verificar se as credenciais estão válidas
if ! aws sts get-caller-identity --profile "$PROFILE" >/dev/null 2>&1; then
    echo "❌ Credenciais inválidas ou expiradas para o perfil: $PROFILE"
    echo "💡 Execute: aws sso login --profile $PROFILE"
    exit 1
fi

echo "✅ Credenciais válidas para o perfil: $PROFILE"

# Exportar credenciais usando aws configure export-credentials
echo -e "\n🔑 Exportando credenciais..."

# Exportar em formato env
CREDENTIALS_ENV=$(aws configure export-credentials --profile "$PROFILE" --format env 2>/dev/null)

if [ $? -eq 0 ] && [ -n "$CREDENTIALS_ENV" ]; then
    echo "✅ Credenciais exportadas com sucesso!"
    
    # Extrair valores individuais
    ACCESS_KEY=$(echo "$CREDENTIALS_ENV" | grep "AWS_ACCESS_KEY_ID" | cut -d'=' -f2)
    SECRET_KEY=$(echo "$CREDENTIALS_ENV" | grep "AWS_SECRET_ACCESS_KEY" | cut -d'=' -f2)
    SESSION_TOKEN=$(echo "$CREDENTIALS_ENV" | grep "AWS_SESSION_TOKEN" | cut -d'=' -f2)
    REGION=$(echo "$CREDENTIALS_ENV" | grep "AWS_DEFAULT_REGION" | cut -d'=' -f2)
    
    echo -e "\n📋 Credenciais:"
    echo "🔑 Access Key: $ACCESS_KEY"
    echo "🔐 Secret Key: $SECRET_KEY"
    echo "🎫 Session Token: $SESSION_TOKEN"
    echo "🌍 Region: $REGION"
    
    # Verificar expiração
    EXPIRES_AT=$(aws sts get-caller-identity --profile "$PROFILE" --query 'Credentials.Expiration' --output text 2>/dev/null)
    if [ -n "$EXPIRES_AT" ]; then
        echo "⏰ Expira em: $EXPIRES_AT"
    fi
    
    echo -e "\n=== Comandos para usar as credenciais ==="
    echo "1. Exportar para variáveis de ambiente:"
    echo "   export AWS_ACCESS_KEY_ID=\"$ACCESS_KEY\""
    echo "   export AWS_SECRET_ACCESS_KEY=\"$SECRET_KEY\""
    echo "   export AWS_SESSION_TOKEN=\"$SESSION_TOKEN\""
    echo "   export AWS_DEFAULT_REGION=\"$REGION\""
    echo ""
    echo "2. Usar no appsettings.Development.json:"
    echo "   \"AccessKey\": \"$ACCESS_KEY\","
    echo "   \"SecretKey\": \"$SECRET_KEY\","
    echo "   \"SessionToken\": \"$SESSION_TOKEN\""
    echo ""
    echo "3. Verificar identidade:"
    echo "   aws sts get-caller-identity --profile $PROFILE"
    echo ""
    echo "4. Renovar credenciais:"
    echo "   aws sso login --profile $PROFILE"
    
else
    echo "❌ Erro ao exportar credenciais"
    echo "💡 Verifique se o perfil está configurado corretamente"
    exit 1
fi
