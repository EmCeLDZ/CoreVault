# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files (to restore packages)
COPY ["src/CoreVault.API/CoreVault.API.csproj", "src/CoreVault.API/"]
COPY ["src/CoreVault.Shared/CoreVault.Shared.csproj", "src/CoreVault.Shared/"]

# Restore main project (will pull Shared as well)
RUN dotnet restore "src/CoreVault.API/CoreVault.API.csproj"

# Copy rest of the code
COPY . .

# Build
WORKDIR "/src/src/CoreVault.API"
RUN dotnet build "CoreVault.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "CoreVault.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .

# Create directory for file storage
RUN mkdir -p /app/storage/files

# Set proper permissions
RUN chown -R app:app /app/storage /app/logs
USER app

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Tu (opcjonalnie) dodasz Litestream w przyszłości
# ...

ENTRYPOINT ["dotnet", "CoreVault.API.dll"]
