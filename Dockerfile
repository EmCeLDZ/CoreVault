# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["CoreVault.csproj", "./"]
RUN dotnet restore "./CoreVault.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "CoreVault.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreVault.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for file storage
RUN mkdir -p /app/storage/files

# Set proper permissions
RUN chown -R app:app /app/storage /app/logs
USER app

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "CoreVault.dll"]
