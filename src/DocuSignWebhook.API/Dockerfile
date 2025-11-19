# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DocuSignWebhook.sln", "./"]
COPY ["src/DocuSignWebhook.Domain/DocuSignWebhook.Domain.csproj", "src/DocuSignWebhook.Domain/"]
COPY ["src/DocuSignWebhook.Application/DocuSignWebhook.Application.csproj", "src/DocuSignWebhook.Application/"]
COPY ["src/DocuSignWebhook.Infrastructure/DocuSignWebhook.Infrastructure.csproj", "src/DocuSignWebhook.Infrastructure/"]
COPY ["src/DocuSignWebhook.API/DocuSignWebhook.API.csproj", "src/DocuSignWebhook.API/"]

# Restore dependencies
RUN dotnet restore "DocuSignWebhook.sln"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/DocuSignWebhook.API"
RUN dotnet build "DocuSignWebhook.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DocuSignWebhook.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocuSignWebhook.API.dll"]
