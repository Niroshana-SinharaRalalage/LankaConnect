#!/bin/bash
# LankaConnect Automation Utilities (Bash/Linux/macOS)
# Shared functions for build automation and cultural intelligence validation

# Color coding for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Logging functionality
write_log() {
    local message="$1"
    local level="${2:-INFO}"
    local log_file="${3:-automation.log}"
    
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local log_entry="[$timestamp] [$level] $message"
    
    # Ensure logs directory exists
    local script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    local log_dir="$script_dir/logs"
    mkdir -p "$log_dir"
    
    local full_log_path="$log_dir/$log_file"
    echo "$log_entry" >> "$full_log_path"
    
    # Also output to console with color
    case "$level" in
        "ERROR") echo -e "${RED}❌ $message${NC}" ;;
        "WARN") echo -e "${YELLOW}⚠️ $message${NC}" ;;
        "SUCCESS") echo -e "${GREEN}✅ $message${NC}" ;;
        *) echo -e "${CYAN}ℹ️ $message${NC}" ;;
    esac
}

# Project structure validation
test_project_structure() {
    local root_path="${1:-$(pwd)}"
    
    local required_paths=(
        "src/LankaConnect.Domain"
        "src/LankaConnect.Application"
        "src/LankaConnect.Infrastructure"
        "src/LankaConnect.API"
        "tests/LankaConnect.Domain.Tests"
        "tests/LankaConnect.Application.Tests"
        "tests/LankaConnect.IntegrationTests"
    )
    
    local project_root="$(dirname "$root_path")"
    local missing=()
    
    for path in "${required_paths[@]}"; do
        local full_path="$project_root/$path"
        if [ ! -d "$full_path" ]; then
            missing+=("$path")
        fi
    done
    
    if [ ${#missing[@]} -gt 0 ]; then
        write_log "Missing required project paths: ${missing[*]}" "ERROR"
        return 1
    fi
    
    write_log "Project structure validation passed" "SUCCESS"
    return 0
}

# Cultural intelligence feature detection
test_cultural_features() {
    local project_root="$1"
    
    local cultural_patterns=(
        "CulturalIntelligence"
        "DiasporaMapping"
        "CulturalEvent"
        "CommunityEngagement"
        "CulturalContext"
        "LocalizationService"
    )
    
    local found_features=()
    local search_paths=("src" "tests")
    
    for search_path in "${search_paths[@]}"; do
        local full_search_path="$project_root/$search_path"
        if [ -d "$full_search_path" ]; then
            for pattern in "${cultural_patterns[@]}"; do
                if find "$full_search_path" -name "*.cs" -exec grep -l "$pattern" {} \; | head -1 > /dev/null; then
                    found_features+=("$pattern")
                fi
            done
        fi
    done
    
    # Remove duplicates
    local unique_features=($(printf "%s\n" "${found_features[@]}" | sort -u))
    
    if [ ${#unique_features[@]} -eq 0 ]; then
        write_log "No cultural intelligence features detected" "WARN"
        echo "0"
        return 0
    fi
    
    write_log "Cultural features found: ${unique_features[*]}" "SUCCESS"
    echo "${#unique_features[@]}"
    return 0
}

# Build validation
invoke_build_validation() {
    local project_root="$1"
    local configuration="${2:-Debug}"
    
    write_log "Starting build validation for $configuration configuration" "INFO"
    
    # Check if dotnet is available
    if ! command -v dotnet &> /dev/null; then
        write_log ".NET SDK not found or not accessible" "ERROR"
        return 1
    fi
    
    # Clean build
    write_log "Cleaning previous build artifacts" "INFO"
    if ! dotnet clean "$project_root" --configuration "$configuration" > /dev/null 2>&1; then
        write_log "Clean failed" "ERROR"
        return 1
    fi
    
    # Restore packages
    write_log "Restoring NuGet packages" "INFO"
    if ! dotnet restore "$project_root" > /dev/null 2>&1; then
        write_log "Restore failed" "ERROR"
        return 1
    fi
    
    # Build
    write_log "Building solution" "INFO"
    local build_output
    build_output=$(dotnet build "$project_root" --configuration "$configuration" --no-restore 2>&1)
    local build_exit_code=$?
    
    if [ $build_exit_code -ne 0 ]; then
        write_log "Build failed: $build_output" "ERROR"
        return 1
    fi
    
    write_log "Build validation completed successfully" "SUCCESS"
    return 0
}

# Test execution with metrics
invoke_test_execution() {
    local project_path="$1"
    local test_pattern="${2:-*}"
    local configuration="${3:-Debug}"
    local collect_coverage="${4:-false}"
    
    write_log "Executing tests for: $project_path" "INFO"
    
    if [ ! -d "$project_path" ]; then
        write_log "Test project not found: $project_path" "ERROR"
        return 1
    fi
    
    local test_args=("test" "$project_path" "--configuration" "$configuration" "--logger" "console;verbosity=detailed" "--no-build")
    
    if [ "$collect_coverage" = "true" ]; then
        test_args+=("--collect" "XPlat Code Coverage")
    fi
    
    if [ "$test_pattern" != "*" ]; then
        test_args+=("--filter" "$test_pattern")
    fi
    
    local test_output
    test_output=$(dotnet "${test_args[@]}" 2>&1)
    local test_exit_code=$?
    
    if [ $test_exit_code -eq 0 ]; then
        write_log "Tests passed for $project_path" "SUCCESS"
        return 0
    else
        write_log "Tests failed for $project_path. Output: $test_output" "ERROR"
        return 1
    fi
}

# Quality metrics collection
get_quality_metrics() {
    local project_root="$1"
    
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local build_status="Unknown"
    local cultural_features_count=0
    
    # Collect build metrics
    if invoke_build_validation "$project_root"; then
        build_status="Success"
    else
        build_status="Failed"
    fi
    
    # Collect cultural intelligence metrics
    cultural_features_count=$(test_cultural_features "$project_root")
    
    write_log "Quality metrics collected: Build=$build_status, Cultural Features=$cultural_features_count" "INFO"
    
    # Output metrics as JSON-like format for parsing
    cat << EOF
{
    "timestamp": "$timestamp",
    "buildStatus": "$build_status",
    "culturalFeatures": $cultural_features_count,
    "projectRoot": "$project_root"
}
EOF
}

# Performance monitoring
measure_performance_metrics() {
    local operation_name="$1"
    local command="$2"
    
    local start_time=$(date +%s.%N)
    
    # Execute the command
    local output
    local exit_code
    if output=$(eval "$command" 2>&1); then
        exit_code=0
    else
        exit_code=$?
    fi
    
    local end_time=$(date +%s.%N)
    local duration=$(echo "$end_time - $start_time" | bc -l)
    local duration_ms=$(echo "$duration * 1000" | bc -l | cut -d. -f1)
    
    if [ $exit_code -eq 0 ]; then
        write_log "Performance: $operation_name completed in ${duration_ms}ms" "INFO"
    else
        write_log "Performance: $operation_name failed after ${duration_ms}ms" "ERROR"
    fi
    
    echo "{\"success\": $([ $exit_code -eq 0 ] && echo true || echo false), \"durationMs\": $duration_ms, \"output\": \"$output\"}"
    return $exit_code
}

# Report generation
new_automation_report() {
    local metrics="$1"
    local report_path="${2:-automation-report.json}"
    
    local script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    local report_dir="$script_dir/reports"
    mkdir -p "$report_dir"
    
    local full_report_path="$report_dir/$report_path"
    echo "$metrics" > "$full_report_path"
    
    write_log "Automation report saved to: $full_report_path" "SUCCESS"
    echo "$full_report_path"
}

# Check if script is sourced or executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    write_log "Shared utilities loaded successfully" "INFO"
    echo "LankaConnect Automation Utilities - Available functions:"
    echo "  - write_log <message> [level] [log_file]"
    echo "  - test_project_structure [root_path]"
    echo "  - test_cultural_features <project_root>"
    echo "  - invoke_build_validation <project_root> [configuration]"
    echo "  - invoke_test_execution <project_path> [pattern] [configuration] [collect_coverage]"
    echo "  - get_quality_metrics <project_root>"
    echo "  - measure_performance_metrics <operation_name> <command>"
    echo "  - new_automation_report <metrics> [report_path]"
fi