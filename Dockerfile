FROM hub.lumigame.com/base/dotnet/core/sdk:9.0-alpine AS builder

WORKDIR /src
COPY ["ApiEventConsumer.Service/ApiEventConsumer.Service.csproj", "ApiEventConsumer.Service/"]
COPY ["ApiEventConsumer.Core/ApiEventConsumer.Core.csproj", "ApiEventConsumer.Core/"]
COPY ["ApiEventConsumer.Database/ApiEventConsumer.Database.csproj", "ApiEventConsumer.Database/"]

WORKDIR /src/ApiEventConsumer.Service
RUN dotnet restore -v diag

COPY . /src
RUN dotnet publish -r linux-x64 --self-contained false -o /app 

FROM hub.lumigame.com/base/dotnet/core/sdk:9.0-alpine AS base

WORKDIR /app
RUN adduser app_user --disabled-password
USER app_user
COPY --from=builder /app .

EXPOSE 5000

ENTRYPOINT ["dotnet", "ApiEventConsumer.Service.dll"]
