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
# Phase 6A.34: API.csproj now links Infrastructure email templates via <Content Include>
# This ensures templates are automatically included in build and publish output
COPY ["src/LankaConnect.API/", "LankaConnect.API/"]
COPY ["src/LankaConnect.Application/", "LankaConnect.Application/"]
COPY ["src/LankaConnect.Domain/", "LankaConnect.Domain/"]
COPY ["src/LankaConnect.Infrastructure/", "LankaConnect.Infrastructure/"]

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

# Phase 6A.35: Define APP_UID and create non-root user for security
# This ensures Templates directory has correct ownership and read permissions
ARG APP_UID=1654
RUN groupadd -g $APP_UID appgroup && \
    useradd -m -u $APP_UID -g appgroup appuser

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published output
# Phase 6A.34: Templates are automatically included via API.csproj linked content
# No need for separate COPY command - they're already in /app/publish
COPY --from=publish /app/publish .

# Phase 6A.35: Set world-readable permissions for Templates (Azure Container Apps runs as uid=1654 but doesn't respect our chown)
# Make templates readable by all users since Azure might override ownership
RUN if [ -d ./Templates ]; then chmod -R a+rX ./Templates; fi

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
