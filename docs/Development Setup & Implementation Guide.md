# LankaConnect - Development Setup & Implementation Guide
## Complete Environment Configuration

**Version:** 1.0  
**Last Updated:** January 2025  
**Status:** Final  
**Owner:** Development Team  
**Target Audience:** Developers, Claude Code Agents, DevOps Engineers

---

## 1. Executive Summary

This guide provides step-by-step instructions for setting up the complete LankaConnect development environment, including Azure resource provisioning, local development configuration, repository structure, and Claude Code agent workspace optimization.

### 1.1 Prerequisites
- Windows 10/11, macOS, or Linux
- 16GB RAM minimum (32GB recommended)
- 50GB free disk space
- Administrator/sudo access
- Azure subscription
- GitHub account

### 1.2 Time Estimates
- Azure Setup: 2 hours
- Local Environment: 1 hour
- Repository Setup: 30 minutes
- First Build: 30 minutes
- Total: ~4 hours

---

## 2. Required Software Installation

### 2.1 Core Development Tools
```bash
# Windows (using winget)
winget install Microsoft.DotNet.SDK.8
winget install Microsoft.VisualStudioCode
winget install Git.Git
winget install Docker.DockerDesktop
winget install Microsoft.AzureCLI
winget install OpenJS.NodeJS.LTS

# macOS (using homebrew)
brew install --cask dotnet-sdk
brew install --cask visual-studio-code
brew install git
brew install --cask docker
brew install azure-cli
brew install node

# Linux (Ubuntu/Debian)
# Install .NET 8 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Other tools
sudo apt update
sudo apt install -y git docker.io docker-compose azure-cli nodejs npm
```

### 2.2 VS Code Extensions
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "ms-azuretools.vscode-docker",
    "ms-azuretools.vscode-azureresourcegroups",
    "ms-vscode.azurecli",
    "streetsidesoftware.code-spell-checker",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "eamodio.gitlens",
    "humao.rest-client",
    "ms-vscode.live-server",
    "redhat.vscode-yaml",
    "ms-kubernetes-tools.vscode-kubernetes-tools",
    "github.copilot"
  ]
}
```

### 2.3 Additional Tools
```bash
# Entity Framework CLI
dotnet tool install --global dotnet-ef

# HTTP REPL for API testing
dotnet tool install --global Microsoft.dotnet-httprepl

# Report Generator for code coverage
dotnet tool install --global dotnet-reportgenerator-globaltool

# Bicep CLI for Azure IaC
az bicep install

# PostgreSQL client
# Windows: Download from https://www.postgresql.org/download/windows/
# macOS: brew install postgresql
# Linux: sudo apt install postgresql-client
```

---

## 3. Azure Account Setup

### 3.1 Azure Subscription Configuration
```bash
# Login to Azure
az login

# Set subscription
az account list --output table
az account set --subscription "Your-Subscription-Name"

# Create resource group
az group create \
  --name rg-lankaconnect-dev \
  --location "South India"

# Register required providers
az provider register --namespace Microsoft.ContainerRegistry
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.DBforPostgreSQL
az provider register --namespace Microsoft.Cache
az provider register --namespace Microsoft.Storage
az provider register --namespace Microsoft.CognitiveServices
```

### 3.2 Service Principal Creation
```bash
# Create service principal for deployment
az ad sp create-for-rbac \
  --name "sp-lankaconnect-dev" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-lankaconnect-dev \
  --sdk-auth > azure-credentials.json

# Save the JSON output securely - needed for GitHub Actions
```

### 3.3 Azure AD B2C Setup
```bash
# Create B2C tenant (do this in Azure Portal)
# Note: B2C requires manual setup in portal

# After creating B2C tenant, configure app registrations
az ad app create --display-name "LankaConnect-API" \
  --sign-in-audience AzureADandPersonalMicrosoftAccount

az ad app create --display-name "LankaConnect-Web" \
  --sign-in-audience AzureADandPersonalMicrosoftAccount \
  --spa-redirect-uris "http://localhost:3000" "https://app.lankaconnect.lk"
```

---

## 4. Local Development Environment

### 4.1 Docker Configuration

#### docker-compose.yml
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: lankaconnect-postgres
    environment:
      POSTGRES_DB: lankaconnect_dev
      POSTGRES_USER: lankaconnect
      POSTGRES_PASSWORD: DevPassword123!
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U lankaconnect"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: lankaconnect-redis
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  mailhog:
    image: mailhog/mailhog
    container_name: lankaconnect-mailhog
    ports:
      - "1025:1025" # SMTP
      - "8025:8025" # Web UI
    environment:
      MH_STORAGE: memory

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    container_name: lankaconnect-azurite
    ports:
      - "10000:10000" # Blob
      - "10001:10001" # Queue
      - "10002:10002" # Table
    command: azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0

  seq:
    image: datalust/seq:latest
    container_name: lankaconnect-seq
    environment:
      ACCEPT_EULA: Y
    ports:
      - "5341:80"
      - "5342:5341"
    volumes:
      - seq_data:/data

volumes:
  postgres_data:
  redis_data:
  seq_data:
```

#### docker-compose.override.yml
```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/LankaConnect.API/Dockerfile
    container_name: lankaconnect-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=lankaconnect_dev;Username=lankaconnect;Password=DevPassword123!
      - ConnectionStrings__Redis=redis:6379
      - AzureAdB2C__Instance=https://lankaconnectdev.b2clogin.com
    ports:
      - "5000:80"
      - "5001:443"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    volumes:
      - ./src:/src
      - ${APPDATA}/Microsoft/UserSecrets:/root/.microsoft/usersecrets:ro
      - ${APPDATA}/ASP.NET/Https:/root/.aspnet/https:ro
```

### 4.2 Environment Configuration

#### .env.development
```bash
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=lankaconnect_dev
DB_USER=lankaconnect
DB_PASSWORD=DevPassword123!

# Redis
REDIS_CONNECTION=localhost:6379

# Azure Storage (Azurite)
AZURE_STORAGE_CONNECTION=DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;

# Email (MailHog)
SMTP_HOST=localhost
SMTP_PORT=1025
SMTP_FROM=noreply@lankaconnect.dev

# Application Insights (optional for dev)
APPINSIGHTS_INSTRUMENTATIONKEY=

# Azure AD B2C
AZURE_AD_B2C_INSTANCE=https://lankaconnectdev.b2clogin.com
AZURE_AD_B2C_CLIENT_ID=your-client-id
AZURE_AD_B2C_DOMAIN=lankaconnectdev.onmicrosoft.com
AZURE_AD_B2C_SIGNUP_SIGNIN_POLICY=B2C_1_SignUpSignIn

# Logging
SEQ_URL=http://localhost:5341
LOG_LEVEL=Debug
```

### 4.3 HTTPS Development Certificates
```bash
# Generate development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# Export certificate for Docker (if needed)
dotnet dev-certs https -ep ${HOME}/.aspnet/https/lankaconnect.pfx -p DevCertPassword123!

# For Docker container
# Add to docker-compose environment:
# - ASPNETCORE_Kestrel__Certificates__Default__Password=DevCertPassword123!
# - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/lankaconnect.pfx
```

---

## 5. Repository Structure

### 5.1 Solution Structure
```
LankaConnect/
├── src/
│   ├── LankaConnect.Domain/           # Domain models and logic
│   │   ├── Common/
│   │   ├── Identity/
│   │   ├── Events/
│   │   ├── Community/
│   │   └── Business/
│   ├── LankaConnect.Application/      # Use cases and DTOs
│   │   ├── Common/
│   │   ├── Identity/
│   │   ├── Events/
│   │   └── Community/
│   ├── LankaConnect.Infrastructure/   # External concerns
│   │   ├── Persistence/
│   │   ├── Identity/
│   │   ├── Services/
│   │   └── Caching/
│   ├── LankaConnect.API/             # REST API
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Extensions/
│   └── LankaConnect.Web/             # Blazor frontend
│       ├── Pages/
│       ├── Components/
│       └── Services/
├── tests/
│   ├── LankaConnect.Domain.Tests/
│   ├── LankaConnect.Application.Tests/
│   ├── LankaConnect.Infrastructure.Tests/
│   ├── LankaConnect.API.Tests/
│   └── LankaConnect.IntegrationTests/
├── infrastructure/
│   ├── bicep/
│   ├── kubernetes/
│   └── scripts/
├── docs/
│   ├── architecture/
│   ├── api/
│   └── deployment/
├── tools/
│   └── scripts/
├── .github/
│   └── workflows/
├── docker-compose.yml
├── docker-compose.override.yml
├── .gitignore
├── .editorconfig
├── Directory.Build.props
├── Directory.Packages.props
└── README.md
```

### 5.2 Project Creation Script
```powershell
# create-solution.ps1
param(
    [string]$RootPath = "."
)

$solutionName = "LankaConnect"
$projects = @(
    @{Name="$solutionName.Domain"; Type="classlib"; Framework="net8.0"},
    @{Name="$solutionName.Application"; Type="classlib"; Framework="net8.0"},
    @{Name="$solutionName.Infrastructure"; Type="classlib"; Framework="net8.0"},
    @{Name="$solutionName.API"; Type="webapi"; Framework="net8.0"},
    @{Name="$solutionName.Web"; Type="blazorwasm"; Framework="net8.0"},
    @{Name="$solutionName.Domain.Tests"; Type="xunit"; Framework="net8.0"},
    @{Name="$solutionName.Application.Tests"; Type="xunit"; Framework="net8.0"},
    @{Name="$solutionName.Infrastructure.Tests"; Type="xunit"; Framework="net8.0"},
    @{Name="$solutionName.API.Tests"; Type="xunit"; Framework="net8.0"},
    @{Name="$solutionName.IntegrationTests"; Type="xunit"; Framework="net8.0"}
)

# Create solution
dotnet new sln -n $solutionName -o $RootPath

# Create projects
foreach ($project in $projects) {
    $projectPath = if ($project.Name -like "*.Tests") { "tests" } else { "src" }
    $fullPath = Join-Path $RootPath $projectPath $project.Name
    
    Write-Host "Creating project: $($project.Name)"
    dotnet new $project.Type -n $project.Name -f $project.Framework -o $fullPath
    dotnet sln "$RootPath/$solutionName.sln" add $fullPath
}

# Add project references
dotnet add "$RootPath/src/$solutionName.Application" reference "$RootPath/src/$solutionName.Domain"
dotnet add "$RootPath/src/$solutionName.Infrastructure" reference "$RootPath/src/$solutionName.Application"
dotnet add "$RootPath/src/$solutionName.API" reference "$RootPath/src/$solutionName.Application"
dotnet add "$RootPath/src/$solutionName.API" reference "$RootPath/src/$solutionName.Infrastructure"

Write-Host "Solution created successfully!"
```

### 5.3 Git Configuration

#### .gitignore
```gitignore
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio 2015/2017 cache/options directory
.vs/

# Visual Studio Code
.vscode/*
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/launch.json
!.vscode/extensions.json

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# Files built by Visual Studio
*.obj
*.pdb
*.tmp
*.tmp_proj
*_wpftmp.csproj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# NuGet Packages
*.nupkg
*.snupkg
# The packages folder can be ignored because of Package Restore
**/[Pp]ackages/*
# except build/, which is used as an MSBuild target.
!**/[Pp]ackages/build/
# NuGet v3's project.json files produces more ignorable files
*.nuget.props
*.nuget.targets

# Azure credentials
azure-credentials.json
*.publishsettings

# Local environment files
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# Docker volumes
postgres_data/
redis_data/
seq_data/

# macOS
.DS_Store

# Windows
Thumbs.db
ehthumbs.db

# JetBrains Rider
.idea/
*.sln.iml
```

#### .editorconfig
```ini
# EditorConfig is awesome: https://EditorConfig.org

# top-most EditorConfig file
root = true

# All files
[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
insert_final_newline = true
charset = utf-8-bom

# C# files
[*.cs]
# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = flush_left

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_after_comma = true
csharp_space_after_dot = false

# Wrapping preferences
csharp_preserve_single_line_statements = true
csharp_preserve_single_line_blocks = true

# Code style rules
dotnet_sort_system_directives_first = true
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# var preferences
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Expression-bodied members
csharp_style_expression_bodied_methods = false:none
csharp_style_expression_bodied_constructors = false:none
csharp_style_expression_bodied_operators = false:none
csharp_style_expression_bodied_properties = true:none
csharp_style_expression_bodied_indexers = true:none
csharp_style_expression_bodied_accessors = true:none

# JSON files
[*.json]
indent_size = 2

# YAML files
[*.{yml,yaml}]
indent_size = 2

# XML files
[*.{xml,csproj,props,targets}]
indent_size = 2

# Markdown files
[*.md]
trim_trailing_whitespace = false

# Web files
[*.{html,css,scss,js,ts,tsx,jsx}]
indent_size = 2
```

---

## 6. Package Management

### 6.1 Directory.Build.props
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <PropertyGroup>
    <Authors>LankaConnect Team</Authors>
    <Company>LankaConnect</Company>
    <Product>LankaConnect Platform</Product>
    <Copyright>Copyright © LankaConnect 2025</Copyright>
    <RepositoryUrl>https://github.com/lankaconnect/platform</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
  </ItemGroup>
</Project>
```

### 6.2 Directory.Packages.props
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- ASP.NET Core -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.1.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer" Version="5.1.0" />
    
    <!-- Entity Framework Core -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.1" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    
    <!-- MediatR -->
    <PackageVersion Include="MediatR" Version="12.2.0" />
    <PackageVersion Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.0.0" />
    
    <!-- FluentValidation -->
    <PackageVersion Include="FluentValidation" Version="11.9.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
    
    <!-- AutoMapper -->
    <PackageVersion Include="AutoMapper" Version="13.0.0" />
    <PackageVersion Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="13.0.0" />
    
    <!-- Caching -->
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.1" />
    <PackageVersion Include="StackExchange.Redis" Version="2.7.10" />
    
    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
    
    <!-- Azure -->
    <PackageVersion Include="Azure.Storage.Blobs" Version="12.19.1" />
    <PackageVersion Include="Azure.Messaging.ServiceBus" Version="7.17.1" />
    <PackageVersion Include="Microsoft.Azure.CognitiveServices.Language.TextAnalytics" Version="5.1.0" />
    
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.6.6" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageVersion Include="Moq" Version="4.20.70" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="Bogus" Version="35.4.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.1" />
    <PackageVersion Include="Testcontainers" Version="3.7.0" />
    <PackageVersion Include="Respawn" Version="6.1.0" />
    
    <!-- Other -->
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageVersion Include="Polly" Version="8.2.1" />
    <PackageVersion Include="Humanizer.Core" Version="2.14.1" />
  </ItemGroup>
</Project>
```

---

## 7. Database Setup

### 7.1 Initial Migration
```bash
# Navigate to Infrastructure project
cd src/LankaConnect.Infrastructure

# Add initial migration
dotnet ef migrations add InitialCreate \
  --startup-project ../LankaConnect.API \
  --context AppDbContext \
  --output-dir Persistence/Migrations

# Update database
dotnet ef database update \
  --startup-project ../LankaConnect.API \
  --context AppDbContext
```

### 7.2 Database Initialization Script
```sql
-- scripts/init-db.sql
-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "postgis";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create custom types
CREATE TYPE user_status AS ENUM ('Active', 'Inactive', 'Suspended', 'Deleted');
CREATE TYPE event_status AS ENUM ('Draft', 'Published', 'Cancelled', 'Completed');
CREATE TYPE booking_status AS ENUM ('Pending', 'Confirmed', 'Cancelled', 'Completed');

-- Create schemas
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS events;
CREATE SCHEMA IF NOT EXISTS community;
CREATE SCHEMA IF NOT EXISTS business;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA identity TO lankaconnect;
GRANT ALL PRIVILEGES ON SCHEMA events TO lankaconnect;
GRANT ALL PRIVILEGES ON SCHEMA community TO lankaconnect;
GRANT ALL PRIVILEGES ON SCHEMA business TO lankaconnect;
```

---

## 8. Build and Run

### 8.1 First Build
```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
cd src/LankaConnect.API
dotnet run

# Or use watch for hot reload
dotnet watch run
```

### 8.2 Docker Build
```bash
# Build all services
docker-compose build

# Start infrastructure services
docker-compose up -d postgres redis mailhog azurite seq

# Run API in Docker
docker-compose up api

# Or run everything
docker-compose up
```

### 8.3 Verify Installation
```bash
# Check API health
curl https://localhost:5001/health

# Check Swagger UI
open https://localhost:5001/swagger

# Check Seq logs
open http://localhost:5341

# Check MailHog
open http://localhost:8025

# Check database connection
dotnet ef database-connection test
```

---

## 9. Claude Code Agent Configuration

### 9.1 Workspace Settings
```json
// .vscode/settings.json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll": true,
    "source.organizeImports": true
  },
  "files.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/.vs": true
  },
  "csharp.semanticHighlighting.enabled": true,
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "dotnet.defaultSolution": "LankaConnect.sln"
}
```

### 9.2 Launch Configuration
```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/LankaConnect.API/bin/Debug/net8.0/LankaConnect.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/LankaConnect.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:5001;http://localhost:5000"
      }
    },
    {
      "name": "Launch Web",
      "type": "blazorwasm",
      "request": "launch",
      "url": "https://localhost:5003"
    }
  ],
  "compounds": [
    {
      "name": "API + Web",
      "configurations": ["Launch API", "Launch Web"],
      "stopAll": true
    }
  ]
}
```

### 9.3 Tasks Configuration
```json
// .vscode/tasks.json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/LankaConnect.sln"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": [
        "test",
        "${workspaceFolder}/LankaConnect.sln",
        "--logger:trx",
        "--collect:\"XPlat Code Coverage\""
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "ef-migrate",
      "command": "dotnet",
      "type": "process",
      "args": [
        "ef",
        "database",
        "update",
        "--project",
        "${workspaceFolder}/src/LankaConnect.Infrastructure",
        "--startup-project",
        "${workspaceFolder}/src/LankaConnect.API"
      ],
      "problemMatcher": []
    },
    {
      "label": "docker-up",
      "command": "docker-compose",
      "type": "shell",
      "args": ["up", "-d"],
      "problemMatcher": []
    }
  ]
}
```

---

## 10. Troubleshooting

### 10.1 Common Issues

#### PostgreSQL Connection Issues
```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Check logs
docker logs lankaconnect-postgres

# Test connection
psql -h localhost -U lankaconnect -d lankaconnect_dev

# Reset database
docker-compose down -v
docker-compose up -d postgres
```

#### Certificate Issues
```bash
# Clean all certificates
dotnet dev-certs https --clean

# Trust new certificate
dotnet dev-certs https --trust

# For Linux, manually trust
sudo cp ~/.dotnet/corefx/cryptography/x509stores/my/* /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

#### Port Conflicts
```bash
# Find process using port
# Windows
netstat -ano | findstr :5432
taskkill /PID <PID> /F

# macOS/Linux
lsof -i :5432
kill -9 <PID>

# Change ports in docker-compose.yml if needed
```

### 10.2 Performance Tips
```yaml
Development Performance:
  - Use SSD for development
  - Allocate 4GB+ RAM to Docker
  - Enable WSL2 on Windows
  - Use .NET Hot Reload
  - Disable antivirus for dev folders

Build Performance:
  - Use incremental builds
  - Enable parallel builds
  - Use build cache
  - Minimize project references
```

---

## 11. Next Steps

### 11.1 After Setup Checklist
- [ ] Verify all services are running
- [ ] Run initial database migration
- [ ] Create first API endpoint
- [ ] Set up authentication
- [ ] Configure monitoring
- [ ] Create first unit test
- [ ] Commit initial code

### 11.2 Recommended Reading
1. [Clean Architecture in .NET](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
2. [Domain-Driven Design Fundamentals](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
3. [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
4. [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

### 11.3 Development Resources
- **API Documentation:** http://localhost:5001/swagger
- **Seq Logs:** http://localhost:5341
- **MailHog:** http://localhost:8025
- **Redis Commander:** Install separately if needed
- **pgAdmin:** Install separately for PostgreSQL GUI

---

## 12. Conclusion

This comprehensive setup guide provides everything needed to start developing LankaConnect. The environment is optimized for both local development and Claude Code agent sessions, with all necessary tools and configurations in place.

Key features of this setup:
- **Complete local environment** with Docker
- **Azure-ready** configuration
- **Development tools** properly configured
- **Testing infrastructure** ready
- **Claude Code optimized** workspace

Following this guide ensures a consistent, efficient development environment across all team members and AI assistants.