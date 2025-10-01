#!/bin/bash
# LankaConnect Environment Verification Script (Linux/macOS)
# This script verifies the complete development environment setup

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
SKIP_DOCKER=false
VERBOSE=false
CONFIGURATION="Debug"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

echo -e "${GREEN}🚀 Starting LankaConnect Environment Verification${NC}"
echo -e "${CYAN}Configuration: $CONFIGURATION${NC}"
echo -e "${GREEN}===============================================${NC}"

# Test results tracking
declare -A test_results
test_results[dotnet_build]=false
test_results[unit_tests]=false
test_results[integration_tests]=false
test_results[docker_services]=false
test_results[database_connection]=false
test_results[logging_configuration]=false

# Function to log test results
log_test_result() {
    local test_name="$1"
    local success="$2"
    local message="$3"
    
    if [ "$success" = true ]; then
        echo -e "${GREEN}✅ PASS${NC} $test_name"
    else
        echo -e "${RED}❌ FAIL${NC} $test_name"
        if [ -n "$message" ]; then
            echo -e "${YELLOW}   └─ $message${NC}"
        fi
    fi
}

# Function for verbose logging
log_verbose() {
    if [ "$VERBOSE" = true ]; then
        echo -e "${CYAN}$1${NC}"
    fi
}

# Test 1: .NET Build
echo -e "${CYAN}📦 Testing .NET Build...${NC}"
if dotnet build --configuration "$CONFIGURATION" --verbosity minimal > /dev/null 2>&1; then
    test_results[dotnet_build]=true
    log_test_result "DotNet Build" true
else
    log_test_result "DotNet Build" false "Build failed"
fi

# Test 2: Unit Tests
echo -e "${CYAN}🧪 Running Unit Tests...${NC}"
if dotnet test --configuration "$CONFIGURATION" --no-build --verbosity minimal --filter "Category!=Integration" > test_output.tmp 2>&1; then
    test_results[unit_tests]=true
    log_test_result "Unit Tests" true
    
    # Extract test counts
    if grep -q "Passed!" test_output.tmp; then
        test_line=$(grep "Passed!" test_output.tmp | head -1)
        echo -e "${GREEN}   └─ $test_line${NC}"
    fi
else
    log_test_result "Unit Tests" false "$(tail -3 test_output.tmp | tr '\n' ' ')"
fi
rm -f test_output.tmp

# Test 3: Docker Services
if [ "$SKIP_DOCKER" = false ]; then
    echo -e "${CYAN}🐳 Testing Docker Services...${NC}"
    
    if ! docker info > /dev/null 2>&1; then
        log_test_result "Docker Services" false "Docker is not running"
    else
        log_verbose "Starting Docker services..."
        docker-compose up -d --wait > /dev/null 2>&1 || true
        
        # Test individual services
        services_up=true
        
        check_service() {
            local service_name="$1"
            local port="$2"
            
            if timeout 5 bash -c "</dev/tcp/localhost/$port" > /dev/null 2>&1; then
                log_verbose "$service_name is accessible on port $port"
                return 0
            else
                log_test_result "$service_name Service" false "Port $port not accessible"
                return 1
            fi
        }
        
        check_service "PostgreSQL" 5432 || services_up=false
        check_service "Redis" 6379 || services_up=false
        check_service "MailHog" 8025 || services_up=false
        check_service "Seq" 8080 || services_up=false
        check_service "Azurite" 10000 || services_up=false
        
        test_results[docker_services]=$services_up
        log_test_result "Docker Services" $services_up
    fi
else
    echo -e "${YELLOW}⏭️  Skipping Docker Services tests${NC}"
fi

# Test 4: Database Connection
echo -e "${CYAN}🗄️  Testing Database Connection...${NC}"
# For simplicity, we'll use dotnet test for database connectivity
if dotnet test --configuration "$CONFIGURATION" --no-build --verbosity minimal --filter "FullyQualifiedName~DatabaseConnectivityTests" > /dev/null 2>&1; then
    test_results[database_connection]=true
    log_test_result "Database Connection" true
else
    log_test_result "Database Connection" false "Database connectivity tests failed"
fi

# Test 5: Integration Tests
echo -e "${CYAN}🔗 Running Integration Tests...${NC}"
if dotnet test --configuration "$CONFIGURATION" --no-build --verbosity minimal --filter "Category=Integration" > integration_output.tmp 2>&1; then
    test_results[integration_tests]=true
    log_test_result "Integration Tests" true
    
    if grep -q "Passed!" integration_output.tmp; then
        test_line=$(grep "Passed!" integration_output.tmp | head -1)
        echo -e "${GREEN}   └─ $test_line${NC}"
    fi
else
    log_test_result "Integration Tests" false "Some integration tests may require running services"
fi
rm -f integration_output.tmp

# Test 6: Logging Configuration
echo -e "${CYAN}📝 Testing Logging Configuration...${NC}"
if [ -f "src/LankaConnect.API/appsettings.json" ] && [ -f "src/LankaConnect.API/appsettings.Development.json" ]; then
    # Check if Serilog configuration exists
    if grep -q '"Serilog"' "src/LankaConnect.API/appsettings.json"; then
        test_results[logging_configuration]=true
        log_test_result "Logging Configuration" true
    else
        log_test_result "Logging Configuration" false "Serilog configuration missing"
    fi
else
    log_test_result "Logging Configuration" false "Configuration files missing"
fi

# Summary
echo ""
echo -e "${GREEN}🎯 Environment Verification Summary${NC}"
echo -e "${GREEN}===================================${NC}"

passed_tests=0
total_tests=0
for result in "${test_results[@]}"; do
    total_tests=$((total_tests + 1))
    if [ "$result" = true ]; then
        passed_tests=$((passed_tests + 1))
    fi
done

success_rate=$(( (passed_tests * 100) / total_tests ))

if [ $passed_tests -eq $total_tests ]; then
    echo -e "${GREEN}Passed: $passed_tests/$total_tests tests ($success_rate%)${NC}"
    echo -e "${GREEN}🎉 All environment checks passed! Ready for development.${NC}"
    exit 0
else
    echo -e "${YELLOW}Passed: $passed_tests/$total_tests tests ($success_rate%)${NC}"
    echo -e "${YELLOW}⚠️  Some environment checks failed. Please review and fix issues above.${NC}"
    
    # Show specific failed tests
    echo -e "${RED}\nFailed Tests:${NC}"
    for test_name in "${!test_results[@]}"; do
        if [ "${test_results[$test_name]}" = false ]; then
            echo -e "${RED}  - $test_name${NC}"
        fi
    done
    
    exit 1
fi