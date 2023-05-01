#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Izzy-Moonbot/Izzy-Moonbot.csproj", "Izzy-Moonbot/"]
RUN dotnet restore "Izzy-Moonbot/Izzy-Moonbot.csproj"
COPY . .
WORKDIR "/src/Izzy-Moonbot"
RUN dotnet build "Izzy-Moonbot.csproj" -c Release -o /app/build

FROM build AS publish
ARG TARGETARCH
COPY ./Izzy-Moonbot/publish.sh .
RUN chmod u+x ./publish.sh && ./publish.sh $TARGETARCH

FROM base AS final
ARG BUILD_DATE=null
ARG BUILD_COMMIT=null
LABEL org.opencontainers.image.authors="Manechat Mod Dev Team"
LABEL org.opencontainers.image.url="https://github.com/Manechat/izzy-moonbot"
LABEL org.opencontainers.image.source="https://github.com/Manechat/izzy-moonbot"
LABEL org.opencontainers.image.revision=$BUILD_COMMIT
LABEL org.opencontainers.image.licenses="MIT"
LABEL org.opencontainers.image.title="Izzy Moonbot"
LABEL org.opencontainers.image.description="Moderation bot for the Manechat Discord server."
LABEL org.opencontainers.image.created=$BUILD_DATE
WORKDIR /app
COPY --from=publish /app/publish .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "Izzy-Moonbot.dll"]
