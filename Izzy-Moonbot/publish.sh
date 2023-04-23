#!/bin/bash

TARGETARCH=$1

if [ "$TARGETARCH" == "amd64" ]; then
    dotnet publish "Izzy-Moonbot.csproj" -c Release -o /app/publish /p:UseAppHost=false --runtime linux-x64 --self-contained false
elif [ "$TARGETARCH" == "arm64" ]; then
    dotnet publish "Izzy-Moonbot.csproj" -c Release -o /app/publish /p:UseAppHost=false --runtime linux-arm64 --self-contained false
fi
