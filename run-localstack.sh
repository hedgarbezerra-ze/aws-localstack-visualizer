#!/bin/bash

cd "C:\Users\99846737\RiderProjects\aws-localstack-visualizer"

nohup dotnet run --project AwsLocalStackVisualizer/AwsLocalStackVisualizer.csproj --launch-profile LocalStack > localstack.log 2>&1 &

echo "Projeto iniciado em background. PID: $!"
echo "Logs: localstack.log"





