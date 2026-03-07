# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TaskProcessor.API/TaskProcessor.API.csproj", "TaskProcessor.API/"]
COPY ["TaskProcessor.Worker/TaskProcessor.Worker.csproj", "TaskProcessor.Worker/"]
COPY ["TaskProcessor.Application/TaskProcessor.Application.csproj", "TaskProcessor.Application/"]
COPY ["TaskProcessor.Infrastructure/TaskProcessor.Infrastructure.csproj", "TaskProcessor.Infrastructure/"]
COPY ["TaskProcessor.Domain/TaskProcessor.Domain.csproj", "TaskProcessor.Domain/"]

RUN dotnet restore "TaskProcessor.API/TaskProcessor.API.csproj"
RUN dotnet restore "TaskProcessor.Worker/TaskProcessor.Worker.csproj"

COPY . .

RUN dotnet publish "TaskProcessor.API/TaskProcessor.API.csproj" -c Release -o /app/api
RUN dotnet publish "TaskProcessor.Worker/TaskProcessor.Worker.csproj" -c Release -o /app/worker

# API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
COPY --from=build /app/api .
ENTRYPOINT ["dotnet", "TaskProcessor.API.dll"]

# Worker
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS worker
WORKDIR /app
COPY --from=build /app/worker .
ENTRYPOINT ["dotnet", "TaskProcessor.Worker.dll"]