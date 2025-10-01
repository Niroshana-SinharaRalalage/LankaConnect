#!/bin/bash
# LankaConnect TDD RED Phase Automation (Bash/Linux/macOS)
# Creates failing tests and validates test infrastructure for cultural intelligence features

set -euo pipefail

# Default parameters
FEATURE_NAME=""
DOMAIN="CulturalIntelligence"
LAYER="Domain"
SKIP_BUILD=false
VERBOSE=false

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"

# Source shared utilities
if [[ -f "$SCRIPT_DIR/shared-utilities.sh" ]]; then
    source "$SCRIPT_DIR/shared-utilities.sh"
else
    echo "ERROR: Shared utilities not found at $SCRIPT_DIR/shared-utilities.sh" >&2
    exit 1
fi

# Usage function
usage() {
    cat << EOF
Usage: $0 --feature-name <name> [options]

Options:
    --feature-name <name>    Feature name (required)
    --domain <domain>        Domain name (default: CulturalIntelligence)
    --layer <layer>          Target layer: Domain|Application|Infrastructure|API (default: Domain)
    --skip-build            Skip build validation
    --verbose               Verbose output
    --help                  Show this help message

Example:
    $0 --feature-name "DiasporaMapping" --domain "CulturalIntelligence" --layer "Domain"
EOF
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --feature-name)
                FEATURE_NAME="$2"
                shift 2
                ;;
            --domain)
                DOMAIN="$2"
                shift 2
                ;;
            --layer)
                if [[ "$2" =~ ^(Domain|Application|Infrastructure|API)$ ]]; then
                    LAYER="$2"
                else
                    echo "ERROR: Invalid layer '$2'. Must be Domain, Application, Infrastructure, or API" >&2
                    exit 1
                fi
                shift 2
                ;;
            --skip-build)
                SKIP_BUILD=true
                shift
                ;;
            --verbose)
                VERBOSE=true
                shift
                ;;
            --help)
                usage
                exit 0
                ;;
            *)
                echo "ERROR: Unknown option '$1'" >&2
                usage
                exit 1
                ;;
        esac
    done
    
    if [[ -z "$FEATURE_NAME" ]]; then
        echo "ERROR: --feature-name is required" >&2
        usage
        exit 1
    fi
}

# Initialize RED phase
initialize_red_phase() {
    write_log "🔴 Starting TDD RED Phase for feature: $FEATURE_NAME" "INFO"
    write_log "Target Domain: $DOMAIN, Layer: $LAYER" "INFO"
    
    # Validate project structure
    if ! test_project_structure "$PROJECT_ROOT"; then
        write_log "Project structure validation failed" "ERROR"
        return 1
    fi
    
    write_log "Project structure validated successfully" "SUCCESS"
    return 0
}

# Create failing test
create_failing_test() {
    local feature_name="$1"
    local domain="$2"
    local layer="$3"
    
    local test_project=""
    case "$layer" in
        "Domain") test_project="tests/LankaConnect.Domain.Tests" ;;
        "Application") test_project="tests/LankaConnect.Application.Tests" ;;
        "Infrastructure") test_project="tests/LankaConnect.Infrastructure.Tests" ;;
        "API") test_project="tests/LankaConnect.IntegrationTests" ;;
    esac
    
    local test_path="$PROJECT_ROOT/$test_project"
    local domain_path="$test_path/$domain"
    
    # Ensure test directory exists
    mkdir -p "$domain_path"
    write_log "Created test directory: $domain_path" "INFO"
    
    local test_file_name="${feature_name}Tests.cs"
    local test_file_path="$domain_path/$test_file_name"
    
    # Generate failing test based on layer
    case "$layer" in
        "Domain") create_domain_test_template "$feature_name" "$domain" > "$test_file_path" ;;
        "Application") create_application_test_template "$feature_name" "$domain" > "$test_file_path" ;;
        "Infrastructure") create_infrastructure_test_template "$feature_name" "$domain" > "$test_file_path" ;;
        "API") create_api_test_template "$feature_name" "$domain" > "$test_file_path" ;;
    esac
    
    write_log "Created failing test: $test_file_path" "SUCCESS"
    echo "$test_file_path"
}

# Domain test template
create_domain_test_template() {
    local feature_name="$1"
    local domain="$2"
    
    cat << EOF
using LankaConnect.Domain.$domain;
using LankaConnect.Domain.Common;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Domain.Tests.$domain
{
    /// <summary>
    /// TDD RED Phase tests for $feature_name
    /// These tests are designed to FAIL initially to drive implementation
    /// </summary>
    public class ${feature_name}Tests
    {
        [Fact]
        public void ${feature_name}_ShouldHaveValidCulturalContext()
        {
            // RED: This test should fail - no implementation exists yet
            // Arrange
            var culturalContext = new CulturalContext("LK", "Sinhala");
            
            // Act & Assert
            Assert.True(false, "RED PHASE: ${feature_name} cultural context validation not implemented");
        }
        
        [Fact]
        public void ${feature_name}_ShouldValidateBusinessRules()
        {
            // RED: This test should fail - business rules not defined
            // Arrange & Act & Assert
            Assert.True(false, "RED PHASE: ${feature_name} business rule validation not implemented");
        }
        
        [Fact]
        public void ${feature_name}_ShouldRaiseDomainEvents()
        {
            // RED: This test should fail - domain events not implemented
            // Arrange & Act & Assert
            Assert.True(false, "RED PHASE: ${feature_name} domain events not implemented");
        }
        
        [Theory]
        [InlineData("LK", "Sinhala")]
        [InlineData("LK", "Tamil")]
        [InlineData("US", "English")]
        public void ${feature_name}_ShouldSupportMultipleCultures(string countryCode, string language)
        {
            // RED: This test should fail - multicultural support not implemented
            Assert.True(false, \$"RED PHASE: ${feature_name} multicultural support for {countryCode}-{language} not implemented");
        }
    }
}
EOF
}

# Application test template
create_application_test_template() {
    local feature_name="$1"
    local domain="$2"
    
    cat << EOF
using LankaConnect.Application.$domain;
using LankaConnect.Application.Common.Interfaces;
using MediatR;
using Moq;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Application.Tests.$domain
{
    /// <summary>
    /// TDD RED Phase application tests for $feature_name
    /// These tests focus on use cases and application services
    /// </summary>
    public class ${feature_name}ApplicationTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMediator> _mockMediator;
        
        public ${feature_name}ApplicationTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMediator = new Mock<IMediator>();
        }
        
        [Fact]
        public async Task Handle_${feature_name}Command_ShouldProcessSuccessfully()
        {
            // RED: This test should fail - command handler not implemented
            Assert.True(false, "RED PHASE: ${feature_name} command handler not implemented");
        }
        
        [Fact]
        public async Task Handle_${feature_name}Query_ShouldReturnExpectedResult()
        {
            // RED: This test should fail - query handler not implemented
            Assert.True(false, "RED PHASE: ${feature_name} query handler not implemented");
        }
        
        [Fact]
        public async Task Validate_${feature_name}Request_ShouldEnforceCulturalRules()
        {
            // RED: This test should fail - cultural validation not implemented
            Assert.True(false, "RED PHASE: ${feature_name} cultural validation not implemented");
        }
        
        [Fact]
        public async Task Process_${feature_name}_ShouldIntegrateWithCulturalIntelligence()
        {
            // RED: This test should fail - cultural intelligence integration not implemented
            Assert.True(false, "RED PHASE: ${feature_name} cultural intelligence integration not implemented");
        }
    }
}
EOF
}

# Infrastructure test template
create_infrastructure_test_template() {
    local feature_name="$1"
    local domain="$2"
    
    cat << EOF
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.$domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Infrastructure.Tests.$domain
{
    /// <summary>
    /// TDD RED Phase infrastructure tests for $feature_name
    /// These tests focus on data access and external integrations
    /// </summary>
    public class ${feature_name}InfrastructureTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        
        public ${feature_name}InfrastructureTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }
        
        [Fact]
        public async Task Repository_Should_PersistCulturalData()
        {
            // RED: This test should fail - repository not implemented
            Assert.True(false, "RED PHASE: ${feature_name} repository persistence not implemented");
        }
        
        [Fact]
        public async Task ExternalService_Should_IntegrateWithCulturalAPIs()
        {
            // RED: This test should fail - external service integration not implemented
            Assert.True(false, "RED PHASE: ${feature_name} external API integration not implemented");
        }
        
        [Fact]
        public async Task Cache_Should_OptimizeCulturalQueries()
        {
            // RED: This test should fail - caching mechanism not implemented
            Assert.True(false, "RED PHASE: ${feature_name} cultural data caching not implemented");
        }
        
        [Fact]
        public async Task Migration_Should_SupportCulturalSchema()
        {
            // RED: This test should fail - database schema not implemented
            Assert.True(false, "RED PHASE: ${feature_name} cultural database schema not implemented");
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
EOF
}

# API test template
create_api_test_template() {
    local feature_name="$1"
    local domain="$2"
    
    cat << EOF
using LankaConnect.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace LankaConnect.IntegrationTests.$domain
{
    /// <summary>
    /// TDD RED Phase API integration tests for $feature_name
    /// These tests validate end-to-end cultural intelligence workflows
    /// </summary>
    public class ${feature_name}ApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        
        public ${feature_name}ApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }
        
        [Fact]
        public async Task POST_${feature_name}_ShouldReturnCreated()
        {
            // RED: This test should fail - API endpoint not implemented
            Assert.True(false, "RED PHASE: ${feature_name} POST endpoint not implemented");
        }
        
        [Fact]
        public async Task GET_${feature_name}_ShouldReturnCulturalData()
        {
            // RED: This test should fail - API endpoint not implemented
            Assert.True(false, "RED PHASE: ${feature_name} GET endpoint not implemented");
        }
        
        [Fact]
        public async Task PUT_${feature_name}_ShouldUpdateWithCulturalValidation()
        {
            // RED: This test should fail - API endpoint not implemented
            Assert.True(false, "RED PHASE: ${feature_name} PUT endpoint with cultural validation not implemented");
        }
        
        [Theory]
        [InlineData("en-US")]
        [InlineData("si-LK")]
        [InlineData("ta-LK")]
        public async Task ${feature_name}_ShouldSupportMultipleLocales(string locale)
        {
            // RED: This test should fail - localization not implemented
            Assert.True(false, \$"RED PHASE: ${feature_name} localization for {locale} not implemented");
        }
    }
}
EOF
}

# Test that tests are failing (RED phase requirement)
test_failing_tests() {
    local test_file_path="$1"
    
    write_log "Validating that tests fail (RED phase requirement)" "INFO"
    
    local test_project
    test_project=$(dirname "$(dirname "$test_file_path")")
    
    if invoke_test_execution "$test_project" "*" "Debug" "false"; then
        write_log "WARNING: Tests are passing in RED phase - this violates TDD methodology" "WARN"
        return 1
    else
        write_log "✅ Tests are failing as expected in RED phase" "SUCCESS"
        return 0
    fi
}

# RED phase validation
invoke_red_phase_validation() {
    write_log "Running RED phase validation checks" "INFO"
    
    # Build validation (should compile but tests should fail)
    if [[ "$SKIP_BUILD" != "true" ]]; then
        if ! invoke_build_validation "$PROJECT_ROOT"; then
            write_log "Build failed - cannot proceed with RED phase" "ERROR"
            return 1
        fi
    fi
    
    # Cultural intelligence feature validation
    local cultural_features_count
    cultural_features_count=$(test_cultural_features "$PROJECT_ROOT")
    write_log "Cultural features in development: $cultural_features_count" "INFO"
    
    return 0
}

# Generate RED phase report
create_red_phase_report() {
    local test_file_path="$1"
    
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local build_status="Unknown"
    local tests_failing_as_expected="false"
    
    # Collect metrics
    if invoke_build_validation "$PROJECT_ROOT"; then
        build_status="Success"
    else
        build_status="Failed"
    fi
    
    if test_failing_tests "$test_file_path"; then
        tests_failing_as_expected="true"
    fi
    
    local cultural_features_count
    cultural_features_count=$(test_cultural_features "$PROJECT_ROOT")
    
    local report_data
    report_data=$(cat << EOF
{
    "phase": "RED",
    "featureName": "$FEATURE_NAME",
    "domain": "$DOMAIN",
    "layer": "$LAYER",
    "testFile": "$test_file_path",
    "timestamp": "$timestamp",
    "buildStatus": "$build_status",
    "testsFailingAsExpected": $tests_failing_as_expected,
    "culturalFeatures": $cultural_features_count
}
EOF
    )
    
    local report_path="red-phase-$(date '+%Y%m%d-%H%M%S').json"
    new_automation_report "$report_data" "$report_path"
    
    echo "$report_data"
}

# Write build summary
write_build_summary() {
    local test_file_path="$1"
    local build_status="$2"
    local tests_failing="$3"
    
    echo
    echo -e "${CYAN}=== RED PHASE SUMMARY ===${NC}"
    echo -e "${CYAN}Feature: $FEATURE_NAME${NC}"
    echo -e "${CYAN}Domain: $DOMAIN${NC}"
    echo -e "${CYAN}Layer: $LAYER${NC}"
    echo -e "${CYAN}Test File: $test_file_path${NC}"
    echo -e "${CYAN}Tests Failing: $tests_failing${NC}"
    echo -e "${CYAN}Build Status: $build_status${NC}"
    echo -e "${CYAN}=========================${NC}"
    echo
}

# Main execution
main() {
    # Parse arguments
    parse_arguments "$@"
    
    write_log "🔴 TDD RED Phase Automation Started" "INFO"
    
    # Initialize RED phase
    if ! initialize_red_phase; then
        write_log "RED phase initialization failed" "ERROR"
        exit 1
    fi
    
    # Create failing test
    local test_file_path
    test_file_path=$(create_failing_test "$FEATURE_NAME" "$DOMAIN" "$LAYER")
    write_log "Created failing test at: $test_file_path" "SUCCESS"
    
    # Validate RED phase requirements
    if ! invoke_red_phase_validation; then
        write_log "RED phase validation failed" "ERROR"
        exit 1
    fi
    
    # Generate report
    local report
    report=$(create_red_phase_report "$test_file_path")
    
    # Extract report values for summary
    local build_status
    build_status=$(echo "$report" | grep -o '"buildStatus": *"[^"]*"' | cut -d'"' -f4)
    local tests_failing
    tests_failing=$(echo "$report" | grep -o '"testsFailingAsExpected": *[^,}]*' | cut -d':' -f2 | tr -d ' ')
    
    write_log "🔴 TDD RED Phase completed successfully" "SUCCESS"
    write_log "Next step: Run tdd-green-phase.sh to implement the feature" "INFO"
    
    # Output summary
    write_build_summary "$test_file_path" "$build_status" "$tests_failing"
    
    exit 0
}

# Handle script termination
cleanup() {
    local exit_code=$?
    if [[ $exit_code -ne 0 ]]; then
        write_log "RED phase automation failed with exit code $exit_code" "ERROR"
        if [[ "$VERBOSE" == "true" ]]; then
            write_log "Stack trace available in logs" "ERROR"
        fi
    fi
}

# Set trap for cleanup
trap cleanup EXIT

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi