# Docker Development Environment Setup

This document provides comprehensive instructions for setting up and managing the LankaConnect Docker development environment.

## Overview

The development environment includes the following services:

| Service | Port | Description |
|---------|------|-------------|
| PostgreSQL | 5432 | Main database |
| Redis | 6379 | Caching and session store |
| MailHog | 1025/8025 | Email testing (SMTP/Web UI) |
| Azurite | 10000-10002 | Azure Storage emulation |
| Seq | 5341/8080 | Structured logging (Ingestion/Web UI) |
| pgAdmin | 8081 | Database management UI |
| Redis Commander | 8082 | Redis management UI |

## Quick Start

### 1. Prerequisites

- Docker Desktop installed and running
- Git (for cloning the repository)

### 2. Setup Environment

```bash
# Clone the repository (if not already done)
git clone <repository-url>
cd LankaConnect

# Copy environment configuration
cp .env.docker .env.local

# Start all services
docker-compose up -d
```

### 3. Verify Setup

After starting the services, verify they're running:

```bash
docker-compose ps
```

## Service Details

### PostgreSQL Database

- **Container**: `lankaconnect-postgres`
- **Port**: 5432
- **Database**: LankaConnectDB
- **Username**: lankaconnect
- **Password**: dev_password_123

**Connection String**:
```
postgresql://lankaconnect:dev_password_123@localhost:5432/LankaConnectDB
```

**Initial Setup**:
- Creates schemas for each bounded context (identity, events, community, business, content, shared)
- Sets up database roles and permissions
- Includes UUID and case-insensitive text extensions
- Creates audit log table

### Redis Cache

- **Container**: `lankaconnect-redis`
- **Port**: 6379
- **Password**: dev_redis_123

**Connection String**:
```
redis://:dev_redis_123@localhost:6379
```

**Configuration**:
- Memory limit: 128MB
- Persistence enabled with AOF
- Slow query logging enabled
- Dangerous commands disabled

### MailHog (Email Testing)

- **Container**: `lankaconnect-mailhog`
- **SMTP Port**: 1025
- **Web UI Port**: 8025
- **URL**: http://localhost:8025

**Usage**:
- Configure your application to send emails to localhost:1025
- View sent emails in the web interface
- No authentication required for development

### Azurite (Azure Storage Emulation)

- **Container**: `lankaconnect-azurite`
- **Blob Service**: 10000
- **Queue Service**: 10001
- **Table Service**: 10002

**Connection String**:
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;
```

### Seq (Structured Logging)

- **Container**: `lankaconnect-seq`
- **Ingestion Port**: 5341
- **Web UI Port**: 8080
- **URL**: http://localhost:8080

**Usage**:
- Configure your application to send logs to http://localhost:5341
- View and analyze logs in the web interface
- Supports structured logging with JSON

## Management Scripts

### PowerShell Script (Windows)

```powershell
# Start all services
.\scripts\docker-setup.ps1 up

# Start specific service
.\scripts\docker-setup.ps1 up -Service postgres

# Stop all services
.\scripts\docker-setup.ps1 down

# Restart services
.\scripts\docker-setup.ps1 restart

# View logs
.\scripts\docker-setup.ps1 logs

# View status
.\scripts\docker-setup.ps1 status

# Clean up (removes volumes)
.\scripts\docker-setup.ps1 clean
```

### Bash Script (Linux/macOS)

```bash
# Start all services
./scripts/docker-setup.sh up

# Start specific service
./scripts/docker-setup.sh up -s postgres

# Stop all services
./scripts/docker-setup.sh down

# Restart services
./scripts/docker-setup.sh restart

# View logs
./scripts/docker-setup.sh logs

# View status
./scripts/docker-setup.sh status

# Clean up (removes volumes)
./scripts/docker-setup.sh clean
```

## Development Workflow

### 1. Daily Development

```bash
# Start development environment
docker-compose up -d

# Check service health
docker-compose ps

# View application logs (when running)
docker-compose logs -f
```

### 2. Database Operations

```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U lankaconnect -d LankaConnectDB

# Run database migrations (from application)
dotnet ef database update

# Backup database
docker-compose exec postgres pg_dump -U lankaconnect LankaConnectDB > backup.sql

# Restore database
docker-compose exec -T postgres psql -U lankaconnect -d LankaConnectDB < backup.sql
```

### 3. Cache Operations

```bash
# Connect to Redis CLI
docker-compose exec redis redis-cli -a dev_redis_123

# Clear Redis cache
docker-compose exec redis redis-cli -a dev_redis_123 FLUSHALL
```

### 4. Debugging

```bash
# View service logs
docker-compose logs postgres
docker-compose logs redis
docker-compose logs mailhog

# Check service health
docker-compose exec postgres pg_isready -U lankaconnect
docker-compose exec redis redis-cli -a dev_redis_123 ping

# Inspect service configuration
docker-compose exec postgres env
docker-compose exec redis redis-cli -a dev_redis_123 CONFIG GET "*"
```

## Environment Configuration

### Application Settings

Update your `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://lankaconnect:dev_password_123@localhost:5432/LankaConnectDB"
  },
  "Redis": {
    "ConnectionString": "redis://:dev_redis_123@localhost:6379"
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "SmtpUser": "",
    "SmtpPassword": ""
  },
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1"
  },
  "Seq": {
    "ServerUrl": "http://localhost:5341"
  }
}
```

### Entity Framework Configuration

```csharp
// Program.cs or Startup.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Seq Logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.Seq("http://localhost:5341"));
```

## Troubleshooting

### Common Issues

1. **Port conflicts**: Ensure ports 5432, 6379, 1025, 8025, 10000-10002, 5341, 8080-8082 are available
2. **Docker not running**: Start Docker Desktop before running commands
3. **Permission issues**: Ensure Docker has proper permissions to access volumes
4. **Network issues**: Restart Docker Desktop if containers can't communicate

### Health Checks

All services include health checks. View health status:

```bash
docker-compose ps
```

Healthy services will show "Up (healthy)" status.

### Data Persistence

Data is persisted in Docker volumes:
- `postgres_data`: PostgreSQL database files
- `redis_data`: Redis persistence files
- `mailhog_data`: Email storage
- `azurite_data`: Azure Storage emulation files
- `seq_data`: Seq logs and configuration

### Reset Environment

To completely reset the development environment:

```bash
# Stop and remove everything
docker-compose down -v --remove-orphans

# Remove all unused Docker resources
docker system prune -a

# Start fresh
docker-compose up -d
```

## Security Notes

⚠️ **Development Only**: This configuration is for development purposes only. Never use these credentials or configurations in production.

- All passwords are hardcoded development values
- Services are accessible without authentication
- SSL/TLS is not configured
- Firewall rules may need adjustment

## Support

For issues related to the Docker setup:
1. Check the troubleshooting section above
2. Review Docker Desktop logs
3. Ensure all prerequisites are installed
4. Verify network connectivity between containers