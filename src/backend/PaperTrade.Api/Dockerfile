FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY ["src/backend/PaperTrade.Api/PaperTrade.Api.csproj", "src/backend/PaperTrade.Api/"]
COPY ["src/backend/PaperTrade.Application/PaperTrade.Application.csproj", "src/backend/PaperTrade.Application/"]
COPY ["src/backend/PaperTrade.Domain/PaperTrade.Domain.csproj", "src/backend/PaperTrade.Domain/"]
COPY ["src/backend/PaperTrade.Infrastructure/PaperTrade.Infrastructure.csproj", "src/backend/PaperTrade.Infrastructure/"]

RUN dotnet restore "src/backend/PaperTrade.Api/PaperTrade.Api.csproj"

COPY . .

RUN dotnet publish "src/backend/PaperTrade.Api/PaperTrade.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER root

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R ${APP_UID}:${APP_UID} /home/app/.aspnet

USER $APP_UID

ENTRYPOINT ["dotnet", "PaperTrade.Api.dll"]
