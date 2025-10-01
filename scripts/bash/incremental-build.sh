#!/bin/bash
# LankaConnect Incremental Build Validation (Bash/Linux/macOS)
# Provides fast incremental builds with error detection and cultural intelligence validation

set -euo pipefail

# Default parameters
CONFIGURATION="Debug"
SKIP_TESTS=false
SKIP_CULTURAL_VALIDATION=false
CLEAN_BUILD=false
PARALLEL=false
MAX_CPU_COUNT=0
VERBOSE=false
CONTINUOUS_INTEGRATION=false

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

# Build tracking (using associative arrays)
declare -A BUILD_METRICS=(
    ["start_time"]=$(date +%s)
    ["total_duration"]=0
)
declare -a BUILD_ERRORS=()
declare -a BUILD_WARNINGS=()

# Usage function
usage() {
    cat << EOF
Usage: $0 [options]

Options:
    --configuration <config>        Build configuration: Debug|Release (default: Debug)
    --skip-tests                   Skip test execution
    --skip-cultural-validation     Skip cultural intelligence validation
    --clean-build                  Perform clean build
    --parallel                     Enable parallel build
    --max-cpu-count <count>        Maximum CPU count for parallel build (default: auto)
    --continuous-integration       CI mode - continue on failures
    --verbose                      Verbose output
    --help                         Show this help message

Example:
    $0 --configuration Release --parallel --max-cpu-count 4
EOF
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --configuration)
                if [[ "$2" =~ ^(Debug|Release)$ ]]; then
                    CONFIGURATION="$2"
                else
                    echo "ERROR: Invalid configuration '$2'. Must be Debug or Release" >&2
                    exit 1
                fi
                shift 2
                ;;
            --skip-tests)
                SKIP_TESTS=true
                shift
                ;;
            --skip-cultural-validation)
                SKIP_CULTURAL_VALIDATION=true
                shift
                ;;
            --clean-build)
                CLEAN_BUILD=true
                shift
                ;;
            --parallel)
                PARALLEL=true
                shift
                ;;
            --max-cpu-count)
                MAX_CPU_COUNT="$2"
                shift 2
                ;;
            --continuous-integration)
                CONTINUOUS_INTEGRATION=true
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
}

# Initialize incremental build
initialize_incremental_build() {
    write_log "🏗️ Starting incremental build validation" "INFO"
    write_log "Configuration: $CONFIGURATION" "INFO"
    write_log "Parallel: $PARALLEL, MaxCpuCount: $MAX_CPU_COUNT" "INFO"
    
    # Validate project structure
    if ! test_project_structure "$PROJECT_ROOT"; then
        write_log "Project structure validation failed" "ERROR"
        return 1
    fi
    
    # Check for .NET SDK
    if ! command -v dotnet &> /dev/null; then
        write_log ".NET SDK not found or not accessible" "ERROR"
        return 1
    fi
    
    local dotnet_version
    dotnet_version=$(dotnet --version)
    write_log "Using .NET SDK version: $dotnet_version" "INFO"
    
    return 0
}

# Get project dependencies
get_project_dependencies() {
    write_log "Analyzing project dependencies for incremental build" "INFO"
    
    # Define project dependencies as associative arrays
    declare -A projects=(
        ["Domain"]="src/LankaConnect.Domain/LankaConnect.Domain.csproj"
        ["Application"]="src/LankaConnect.Application/LankaConnect.Application.csproj"
        ["Infrastructure"]="src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj"
        ["API"]="src/LankaConnect.API/LankaConnect.API.csproj"
    )
    
    declare -A test_projects=(
        ["Domain"]="tests/LankaConnect.Domain.Tests/LankaConnect.Domain.Tests.csproj"
        ["Application"]="tests/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj"
        ["Infrastructure"]="tests/LankaConnect.Infrastructure.Tests/LankaConnect.Infrastructure.Tests.csproj"
        ["API"]="tests/LankaConnect.IntegrationTests/LankaConnect.IntegrationTests.csproj"
    )
    
    # Output as JSON for easier parsing
    cat << EOF
{
    "projects": {
        "Domain": {
            "path": "${projects[Domain]}",
            "dependencies": [],
            "testPath": "${test_projects[Domain]}"
        },
        "Application": {
            "path": "${projects[Application]}",
            "dependencies": ["Domain"],
            "testPath": "${test_projects[Application]}"
        },
        "Infrastructure": {
            "path": "${projects[Infrastructure]}",
            "dependencies": ["Domain", "Application"],
            "testPath": "${test_projects[Infrastructure]}"
        },
        "API": {
            "path": "${projects[API]}",
            "dependencies": ["Domain", "Application", "Infrastructure"],
            "testPath": "${test_projects[API]}"
        }
    }
}
EOF
}

# Test project changes
test_project_changes() {
    write_log "Detecting changed projects for incremental build" "INFO"
    
    local last_build_file="$PROJECT_ROOT/.incremental-build-cache"
    local last_build_time="1970-01-01 00:00:00"
    
    if [[ -f "$last_build_file" ]]; then
        last_build_time=$(cat "$last_build_file")
        write_log "Last successful build: $last_build_time" "INFO"
    else
        write_log "No previous build cache found - building all projects" "INFO"
        echo "Domain Application Infrastructure API"
        return 0
    fi
    
    local changed_projects=()
    local project_dirs=(
        "src/LankaConnect.Domain"
        "src/LankaConnect.Application"
        "src/LankaConnect.Infrastructure"
        "src/LankaConnect.API"
    )
    local project_names=("Domain" "Application" "Infrastructure" "API")
    
    for i in "${!project_dirs[@]}"; do
        local project_dir="$PROJECT_ROOT/${project_dirs[$i]}"
        local project_name="${project_names[$i]}"
        
        if [[ -d "$project_dir" ]]; then
            # Check if any files in the project directory have been modified since last build
            local has_changes=false
            while IFS= read -r -d '' file; do
                if [[ "$file" -nt "$last_build_file" ]]; then
                    has_changes=true
                    break
                fi
            done < <(find "$project_dir" -name "*.cs" -o -name "*.csproj" -print0)
            
            if [[ "$has_changes" == "true" ]]; then
                changed_projects+=("$project_name")
                write_log "Project $project_name has changes since last build" "INFO"
            fi
        fi
    done
    
    if [[ ${#changed_projects[@]} -eq 0 ]]; then
        write_log "No projects have changes - incremental build not needed" "SUCCESS"
    fi
    
    echo "${changed_projects[@]}"
}

# Get build order based on dependencies
get_build_order() {
    local changed_projects=($1)
    
    # Simple dependency order (could be enhanced with topological sort)
    local all_projects=("Domain" "Application" "Infrastructure" "API")
    local build_order=()
    
    # Add changed projects and their dependents in dependency order
    for project in "${all_projects[@]}"; do
        for changed in "${changed_projects[@]}"; do
            if [[ "$project" == "$changed" ]]; then
                build_order+=("$project")
                break
            fi
        done
    done
    
    # Add dependent projects if their dependencies changed
    local needs_rebuild=("${build_order[@]}")
    for project in "${all_projects[@]}"; do
        local should_add=false
        case "$project" in
            "Application")
                for dep in "${needs_rebuild[@]}"; do
                    if [[ "$dep" == "Domain" ]]; then
                        should_add=true
                        break
                    fi
                done
                ;;
            "Infrastructure")
                for dep in "${needs_rebuild[@]}"; do
                    if [[ "$dep" == "Domain" || "$dep" == "Application" ]]; then
                        should_add=true
                        break
                    fi
                done
                ;;
            "API")
                for dep in "${needs_rebuild[@]}"; do
                    if [[ "$dep" == "Domain" || "$dep" == "Application" || "$dep" == "Infrastructure" ]]; then
                        should_add=true
                        break
                    fi
                done
                ;;
        esac
        
        if [[ "$should_add" == "true" ]]; then
            # Add if not already in build order
            local already_added=false
            for existing in "${build_order[@]}"; do
                if [[ "$existing" == "$project" ]]; then
                    already_added=true
                    break
                fi
            done
            if [[ "$already_added" == "false" ]]; then
                build_order+=("$project")
            fi
        fi
    done
    
    write_log "Build order: ${build_order[*]}" "INFO"
    echo "${build_order[@]}"
}

# Build individual project
invoke_project_build() {
    local project_name="$1"
    local project_path="$2"
    
    local full_project_path="$PROJECT_ROOT/$project_path"
    
    if [[ ! -f "$full_project_path" ]]; then
        write_log "Project file not found: $full_project_path" "ERROR"
        return 1
    fi
    
    write_log "Building project: $project_name" "INFO"
    
    local build_args=("build" "$full_project_path" "--configuration" "$CONFIGURATION" "--no-restore")
    
    if [[ "$PARALLEL" == "true" && "$MAX_CPU_COUNT" -gt 0 ]]; then
        build_args+=("--maxcpucount:$MAX_CPU_COUNT")
    fi
    
    if [[ "$VERBOSE" == "true" ]]; then
        build_args+=("--verbosity" "detailed")
    fi
    
    local start_time=$(date +%s.%N)
    local build_output
    local build_success=false
    
    if build_output=$(dotnet "${build_args[@]}" 2>&1); then
        build_success=true
    fi
    
    local end_time=$(date +%s.%N)
    local duration=$(echo "$end_time - $start_time" | bc -l)
    
    if [[ "$build_success" == "true" ]]; then
        write_log "✅ Project $project_name built successfully in ${duration}s" "SUCCESS"
    else
        write_log "❌ Project $project_name build failed" "ERROR"
        BUILD_ERRORS+=("Project $project_name: $build_output")
    fi
    
    # Parse build output for warnings
    local warnings
    warnings=$(echo "$build_output" | grep -i "warning" || true)
    if [[ -n "$warnings" ]]; then
        BUILD_WARNINGS+=("$warnings")
        local warning_count
        warning_count=$(echo "$warnings" | wc -l)
        write_log "⚠️ Project $project_name has $warning_count warnings" "WARN"
    fi
    
    return $([ "$build_success" == "true" ] && echo 0 || echo 1)
}

# Run project tests
invoke_project_tests() {
    local project_name="$1"
    local test_project_path="$2"
    
    if [[ "$SKIP_TESTS" == "true" ]]; then
        write_log "Skipping tests for $project_name" "INFO"
        return 0
    fi
    
    local full_test_path="$PROJECT_ROOT/$test_project_path"
    
    if [[ ! -f "$full_test_path" ]]; then
        write_log "Test project not found: $full_test_path - skipping tests" "WARN"
        return 0
    fi
    
    write_log "Running tests for project: $project_name" "INFO"
    
    local test_dir
    test_dir=$(dirname "$full_test_path")
    
    if invoke_test_execution "$test_dir" "*" "$CONFIGURATION" "false"; then
        write_log "✅ Tests passed for $project_name" "SUCCESS"
        return 0
    else
        write_log "❌ Tests failed for $project_name" "ERROR"
        BUILD_ERRORS+=("Test failure in $project_name")
        return 1
    fi
}

# Cultural validation
invoke_cultural_validation() {
    local project_name="$1"
    
    if [[ "$SKIP_CULTURAL_VALIDATION" == "true" ]]; then
        write_log "Skipping cultural validation for $project_name" "INFO"
        return 0
    fi
    
    write_log "Running cultural intelligence validation for $project_name" "INFO"
    
    local cultural_features_count
    cultural_features_count=$(test_cultural_features "$PROJECT_ROOT")
    
    local validations=()
    local project_dir=""
    
    case "$project_name" in
        "Domain") project_dir="$PROJECT_ROOT/src/LankaConnect.Domain" ;;
        "Application") project_dir="$PROJECT_ROOT/src/LankaConnect.Application" ;;
        "Infrastructure") project_dir="$PROJECT_ROOT/src/LankaConnect.Infrastructure" ;;
        "API") project_dir="$PROJECT_ROOT/src/LankaConnect.API" ;;
    esac
    
    if [[ -d "$project_dir" ]]; then
        # Check for cultural intelligence patterns
        local patterns=("CulturalContext" "Language.*Sri.*Lanka\|Sinhala\|Tamil" "Region.*Province\|District" "Cultural.*Intelligence\|Diaspora")
        
        for pattern in "${patterns[@]}"; do
            if find "$project_dir" -name "*.cs" -exec grep -l "$pattern" {} \; | head -1 > /dev/null; then
                validations+=("Found pattern: $pattern")
            fi
        done
        
        # Check for compliance issues (simplified)
        local compliance_issues=0
        while IFS= read -r -d '' file; do
            if [[ "$file" != *"Test"* && "$file" != *"Spec"* ]]; then
                # Check for hardcoded English-only strings
                local hardcoded_strings
                hardcoded_strings=$(grep -o '"[A-Za-z ]\{10,\}"' "$file" 2>/dev/null || true)
                if [[ -n "$hardcoded_strings" ]]; then
                    if ! echo "$hardcoded_strings" | grep -qi "exception\|error\|log\|debug\|test"; then
                        ((compliance_issues++))
                    fi
                fi
            fi
        done < <(find "$project_dir" -name "*.cs" -print0)
        
        if [[ $compliance_issues -gt 0 ]]; then
            write_log "⚠️ Found $compliance_issues potential cultural compliance issues in $project_name" "WARN"
        else
            write_log "✅ No cultural compliance issues found in $project_name" "SUCCESS"
        fi
    fi
    
    return 0
}

# Update build cache
update_build_cache() {
    local cache_file="$PROJECT_ROOT/.incremental-build-cache"
    local end_time=$(date +%s)
    BUILD_METRICS["total_duration"]=$((end_time - BUILD_METRICS["start_time"]))
    
    # Only update cache if build was successful
    if [[ ${#BUILD_ERRORS[@]} -eq 0 ]]; then
        date '+%Y-%m-%d %H:%M:%S' > "$cache_file"
        write_log "Build cache updated successfully" "INFO"
    else
        write_log "Build cache not updated due to build failures" "WARN"
    fi
}

# Create build report
create_build_report() {
    local total_projects="${1:-0}"
    local successful_projects="${2:-0}"
    local failed_projects="${3:-0}"
    
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local success=$([[ ${#BUILD_ERRORS[@]} -eq 0 ]] && echo "true" || echo "false")
    
    local report_data
    report_data=$(cat << EOF
{
    "buildMetrics": {
        "startTime": "${BUILD_METRICS[start_time]}",
        "totalDuration": "${BUILD_METRICS[total_duration]}",
        "success": $success
    },
    "configuration": "$CONFIGURATION",
    "incrementalBuild": $([[ "$CLEAN_BUILD" == "false" ]] && echo "true" || echo "false"),
    "timestamp": "$timestamp",
    "summary": {
        "totalProjects": $total_projects,
        "successfulProjects": $successful_projects,
        "failedProjects": $failed_projects,
        "totalErrors": ${#BUILD_ERRORS[@]},
        "totalWarnings": ${#BUILD_WARNINGS[@]}
    },
    "errors": [$(printf '"%s",' "${BUILD_ERRORS[@]}" | sed 's/,$//')]
}
EOF
    )
    
    local report_path="incremental-build-$(date '+%Y%m%d-%H%M%S').json"
    new_automation_report "$report_data" "$report_path"
    
    echo "$report_data"
}

# Write build summary
write_build_summary() {
    local total_projects="$1"
    local successful_projects="$2"
    local failed_projects="$3"
    
    echo
    echo -e "${CYAN}=== INCREMENTAL BUILD SUMMARY ===${NC}"
    echo -e "${CYAN}Configuration: $CONFIGURATION${NC}"
    echo -e "${CYAN}Total Duration: ${BUILD_METRICS[total_duration]}s${NC}"
    echo -e "${CYAN}Projects Built: $total_projects${NC}"
    echo -e "${GREEN}Successful: $successful_projects${NC}"
    echo -e "${RED}Failed: $failed_projects${NC}"
    echo -e "${RED}Errors: ${#BUILD_ERRORS[@]}${NC}"
    echo -e "${YELLOW}Warnings: ${#BUILD_WARNINGS[@]}${NC}"
    echo -e "${CYAN}=================================${NC}"
    echo
}

# Main execution
main() {
    # Parse arguments
    parse_arguments "$@"
    
    write_log "🏗️ Incremental Build Validation Started" "INFO"
    
    # Initialize
    if ! initialize_incremental_build; then
        write_log "Incremental build initialization failed" "ERROR"
        exit 1
    fi
    
    # Get project information
    local projects_json
    projects_json=$(get_project_dependencies)
    
    # Clean build if requested
    local changed_projects
    if [[ "$CLEAN_BUILD" == "true" ]]; then
        write_log "Performing clean build" "INFO"
        dotnet clean "$PROJECT_ROOT" --configuration "$CONFIGURATION" > /dev/null
        changed_projects="Domain Application Infrastructure API"
    else
        # Detect changes for incremental build
        changed_projects=$(test_project_changes)
        
        if [[ -z "$changed_projects" ]]; then
            write_log "No changes detected - build not required" "SUCCESS"
            exit 0
        fi
    fi
    
    # Restore packages
    write_log "Restoring NuGet packages" "INFO"
    if ! dotnet restore "$PROJECT_ROOT" --verbosity minimal > /dev/null; then
        write_log "Package restore failed" "ERROR"
        exit 1
    fi
    
    # Determine build order
    local build_order
    build_order=$(get_build_order "$changed_projects")
    
    # Build projects in dependency order
    local build_success=true
    local total_projects=0
    local successful_projects=0
    local failed_projects=0
    
    # Define project paths (should be extracted from projects_json in real implementation)
    declare -A project_paths=(
        ["Domain"]="src/LankaConnect.Domain/LankaConnect.Domain.csproj"
        ["Application"]="src/LankaConnect.Application/LankaConnect.Application.csproj"
        ["Infrastructure"]="src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj"
        ["API"]="src/LankaConnect.API/LankaConnect.API.csproj"
    )
    
    declare -A test_paths=(
        ["Domain"]="tests/LankaConnect.Domain.Tests/LankaConnect.Domain.Tests.csproj"
        ["Application"]="tests/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj"
        ["Infrastructure"]="tests/LankaConnect.Infrastructure.Tests/LankaConnect.Infrastructure.Tests.csproj"
        ["API"]="tests/LankaConnect.IntegrationTests/LankaConnect.IntegrationTests.csproj"
    )
    
    for project_name in $build_order; do
        ((total_projects++))
        
        # Build project
        if invoke_project_build "$project_name" "${project_paths[$project_name]}"; then
            ((successful_projects++))
        else
            ((failed_projects++))
            build_success=false
            if [[ "$CONTINUOUS_INTEGRATION" != "true" ]]; then
                write_log "Stopping build due to failure in $project_name" "ERROR"
                break
            fi
        fi
        
        # Run tests
        if ! invoke_project_tests "$project_name" "${test_paths[$project_name]}"; then
            build_success=false
            if [[ "$CONTINUOUS_INTEGRATION" != "true" ]]; then
                write_log "Stopping build due to test failure in $project_name" "ERROR"
                break
            fi
        fi
        
        # Cultural validation
        invoke_cultural_validation "$project_name"
    done
    
    # Update build cache and generate report
    update_build_cache
    local report
    report=$(create_build_report "$total_projects" "$successful_projects" "$failed_projects")
    
    # Write summary
    write_build_summary "$total_projects" "$successful_projects" "$failed_projects"
    
    if [[ "$build_success" == "true" ]]; then
        write_log "🏗️ Incremental build completed successfully" "SUCCESS"
        exit 0
    else
        write_log "🏗️ Incremental build completed with errors" "ERROR"
        exit 1
    fi
}

# Handle script termination
cleanup() {
    local exit_code=$?
    if [[ $exit_code -ne 0 ]]; then
        write_log "Incremental build automation failed with exit code $exit_code" "ERROR"
        if [[ "$VERBOSE" == "true" ]]; then
            write_log "Check logs for detailed error information" "ERROR"
        fi
    fi
}

# Set trap for cleanup
trap cleanup EXIT

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi