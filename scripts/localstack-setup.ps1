# LocalStack Setup Script for AWS Visualizer (PowerShell)
# This script creates sample resources for testing the visualizer

$ErrorActionPreference = "Stop"

$ENDPOINT_URL = "http://localhost:4567"
$AWS_REGION = "us-west-2"

Write-Host "🚀 Setting up LocalStack resources..." -ForegroundColor Green

Write-Host "📦 Creating S3 Buckets..." -ForegroundColor Yellow
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://sample-bucket-1 --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://sample-bucket-2 --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL s3 mb s3://logs-bucket --region $AWS_REGION

Write-Host "📄 Adding sample files to S3..." -ForegroundColor Yellow
"Hello World from S3!" | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://sample-bucket-1/hello.txt
$jsonContent = @{
    message = "Sample JSON file"
    timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json -Compress
$jsonContent | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://sample-bucket-1/data.json
"Log entry $(Get-Date)" | aws --endpoint-url=$ENDPOINT_URL s3 cp - s3://logs-bucket/app.log

Write-Host "📬 Creating SQS Queues..." -ForegroundColor Yellow
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name sample-queue --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name sample-dlq --region $AWS_REGION

$queueAttributes = @{
    RedrivePolicy = @{
        deadLetterTargetArn = "arn:aws:sqs:$($AWS_REGION):000000000000:sample-dlq"
        maxReceiveCount = 3
    } | ConvertTo-Json -Compress
    VisibilityTimeoutSeconds = "30"
    MessageRetentionPeriod = "1209600"
} | ConvertTo-Json -Compress

aws --endpoint-url=$ENDPOINT_URL sqs create-queue --queue-name orders-queue --attributes $queueAttributes --region $AWS_REGION

Write-Host "📨 Sending sample messages to SQS..." -ForegroundColor Yellow
aws --endpoint-url=$ENDPOINT_URL sqs send-message --queue-url "http://localhost:4567/000000000000/sample-queue" --message-body "Hello from SQS!" --region $AWS_REGION

$orderMessage = @{
    orderId = "12345"
    customerId = "user123"
    amount = 99.99
    status = "pending"
} | ConvertTo-Json -Compress

$messageAttributes = @{
    OrderType = @{
        StringValue = "online"
        DataType = "String"
    }
    Priority = @{
        StringValue = "high"
        DataType = "String"
    }
} | ConvertTo-Json -Compress

aws --endpoint-url=$ENDPOINT_URL sqs send-message --queue-url "http://localhost:4567/000000000000/orders-queue" --message-body $orderMessage --message-attributes $messageAttributes --region $AWS_REGION

Write-Host "📢 Creating SNS Topics..." -ForegroundColor Yellow
aws --endpoint-url=$ENDPOINT_URL sns create-topic --name order-notifications --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL sns create-topic --name user-events --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL sns create-topic --name system-alerts --region $AWS_REGION

Write-Host "🔗 Creating SNS Subscriptions..." -ForegroundColor Yellow
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn "arn:aws:sns:$($AWS_REGION):000000000000:order-notifications" --protocol sqs --notification-endpoint "arn:aws:sqs:$($AWS_REGION):000000000000:orders-queue" --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn "arn:aws:sns:$($AWS_REGION):000000000000:user-events" --protocol email --notification-endpoint "user@example.com" --region $AWS_REGION

Write-Host "🔐 Creating Secrets Manager secrets..." -ForegroundColor Yellow
$dbCredentials = @{
    username = "admin"
    password = "super-secret-password"
    host = "localhost"
    port = 5432
    database = "myapp"
} | ConvertTo-Json -Compress

aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret --name database-credentials --secret-string $dbCredentials --description "Database connection credentials" --region $AWS_REGION

$apiKeys = @{
    stripe = "sk_test_123456789"
    sendgrid = "SG.abc123def456"
    jwt_secret = "my-jwt-secret-key"
} | ConvertTo-Json -Compress

aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret --name api-keys --secret-string $apiKeys --description "External API keys and tokens" --region $AWS_REGION

$appConfig = @{
    debug_mode = $true
    max_connections = 100
    timeout = 30
    feature_flags = @{
        new_ui = $true
        beta_features = $false
    }
} | ConvertTo-Json -Compress

aws --endpoint-url=$ENDPOINT_URL secretsmanager create-secret --name app-config --secret-string $appConfig --description "Application configuration settings" --region $AWS_REGION

Write-Host "📊 Publishing test messages to SNS..." -ForegroundColor Yellow
aws --endpoint-url=$ENDPOINT_URL sns publish --topic-arn "arn:aws:sns:$($AWS_REGION):000000000000:order-notifications" --message "New order received: #12345" --subject "Order Notification" --region $AWS_REGION
aws --endpoint-url=$ENDPOINT_URL sns publish --topic-arn "arn:aws:sns:$($AWS_REGION):000000000000:system-alerts" --message "System health check: All services operational" --subject "Health Check" --region $AWS_REGION

Write-Host "✅ LocalStack setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Resources created:" -ForegroundColor Cyan
Write-Host "   🪣 S3 Buckets: sample-bucket-1, sample-bucket-2, logs-bucket" -ForegroundColor White
Write-Host "   📬 SQS Queues: sample-queue, sample-dlq, orders-queue" -ForegroundColor White
Write-Host "   📢 SNS Topics: order-notifications, user-events, system-alerts" -ForegroundColor White
Write-Host "   🔐 Secrets: database-credentials, api-keys, app-config" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Access the visualizer at: http://localhost:8080" -ForegroundColor Magenta
Write-Host "📊 LocalStack dashboard at: http://localhost:4566/_localstack/health" -ForegroundColor Magenta
