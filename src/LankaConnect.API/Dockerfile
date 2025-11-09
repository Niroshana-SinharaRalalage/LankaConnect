# ============================================================================
# LankaConnect API - Production-Ready Dockerfile
# ============================================================================
# Multi-stage build optimized for .NET 8, Azure Container Apps deployment
# Follows Clean Architecture and TDD principles
# ============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore (layer caching optimization)
# Build context is repository root, so paths start with src/
COPY ["src/LankaConnect.API/LankaConnect.API.csproj", "LankaConnect.API/"]
COPY ["src/LankaConnect.Application/LankaConnect.Application.csproj", "LankaConnect.Application/"]
COPY ["src/LankaConnect.Domain/LankaConnect.Domain.csproj", "LankaConnect.Domain/"]
COPY ["src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj", "LankaConnect.Infrastructure/"]

# Restore dependencies (this will restore ALL project dependencies including analyzers)
RUN dotnet restore "LankaConnect.API/LankaConnect.API.csproj"

# Copy ALL source code (including all projects)
COPY ["src/LankaConnect.API/", "LankaConnect.API/"]
COPY ["src/LankaConnect.Application/", "LankaConnect.Application/"]
COPY ["src/LankaConnect.Domain/", "LankaConnect.Domain/"]
COPY ["src/LankaConnect.Infrastructure/", "LankaConnect.Infrastructure/"]

# Copy email templates (needed for runtime)
COPY ["Templates/", "LankaConnect.API/Templates/"]

# Build application
WORKDIR "/src/LankaConnect.API"
RUN dotnet build "LankaConnect.API.csproj" \
    -c Release \
    -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "LankaConnect.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Copy email templates to runtime
COPY --from=build /src/LankaConnect.API/Templates ./Templates

# Set correct permissions for Templates directory (before switching to non-root user)
RUN chown -R $APP_UID:$APP_UID ./Templates && chmod -R 755 ./Templates

# Expose port (Container Apps expects HTTP on 5000)
EXPOSE 5000

# Health check endpoint (Azure Container Apps uses this for liveness probe)
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5000/health || exit 1

# Run as non-root user for security
USER $APP_UID

# Set ASP.NET Core environment (override via Container App environment variable)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Entry point
ENTRYPOINT ["dotnet", "LankaConnect.API.dll"]
