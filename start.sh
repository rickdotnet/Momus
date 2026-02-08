#!/bin/bash

# Environment variables for NATS configuration
NATS_URL="nats://localhost:4222"
NATS_USER="your_username"
NATS_PASS="your_password"

# Run Momus container with environment variables
docker run -d \
    --network host \
    --name momus \
    --restart always \
    -e MOMUS_NatsUrl="$NATS_URL" \
    -e MOMUS_NatsUser="$NATS_USER" \
    -e MOMUS_NatsPass="$NATS_PASS" \
    -e ASPNETCORE_HTTP_PORTS=80 \
    -v momus-dataprotection:/root/.aspnet/DataProtection-Keys \
    momus:latest
