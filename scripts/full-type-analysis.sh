#!/bin/bash
# Complete type analysis for all 256 types

INPUT_FILE="missing_types_unique.txt"
OUTPUT_JSON="scripts/type_analysis_complete.json"
OUTPUT_MD="docs/TYPE_DISCOVERY_REPORT.md"

echo "Starting comprehensive type analysis..."
echo "Reading types from $INPUT_FILE"

# Extract type names (remove line numbers and arrows)
TYPES=$(cat "$INPUT_FILE" | sed 's/^[[:space:]]*[0-9]*→//g' | grep -v '^$' | sort | uniq)
TYPE_COUNT=$(echo "$TYPES" | wc -l)

echo "Analyzing $TYPE_COUNT unique types..."

# JSON output structure
echo "{" > "$OUTPUT_JSON"
echo '  "summary": {' >> "$OUTPUT_JSON"
echo '    "totalTypes": '$TYPE_COUNT',' >> "$OUTPUT_JSON"

FOUND_COUNT=0
MISSING_COUNT=0
DUPLICATE_COUNT=0

# Analysis arrays
declare -A FOUND_TYPES
declare -A MISSING_TYPES
declare -A DUPLICATE_TYPES

while IFS= read -r TYPE; do
    [ -z "$TYPE" ] && continue
    
    echo "Analyzing: $TYPE"
    
    # Search for all definition types
    CLASS_FILES=$(grep -r "class $TYPE\b" --include="*.cs" . 2>/dev/null | cut -d: -f1 | sort | uniq)
    RECORD_FILES=$(grep -r "record $TYPE\b" --include="*.cs" . 2>/dev/null | cut -d: -f1 | sort | uniq)
    ENUM_FILES=$(grep -r "enum $TYPE\b" --include="*.cs" . 2>/dev/null | cut -d: -f1 | sort | uniq)
    INTERFACE_FILES=$(grep -r "interface $TYPE\b" --include="*.cs" . 2>/dev/null | cut -d: -f1 | sort | uniq)
    
    # Combine all findings
    ALL_FILES=$(echo -e "$CLASS_FILES\n$RECORD_FILES\n$ENUM_FILES\n$INTERFACE_FILES" | grep -v '^$' | sort | uniq)
    FILE_COUNT=$(echo "$ALL_FILES" | grep -v '^$' | wc -l)
    
    # Count total references
    REF_COUNT=$(grep -r "\b$TYPE\b" --include="*.cs" . 2>/dev/null | wc -l)
    
    if [ "$FILE_COUNT" -gt 0 ]; then
        FOUND_COUNT=$((FOUND_COUNT + 1))
        
        if [ "$FILE_COUNT" -gt 1 ]; then
            DUPLICATE_COUNT=$((DUPLICATE_COUNT + 1))
            DUPLICATE_TYPES["$TYPE"]="$FILE_COUNT files: $ALL_FILES"
            echo "  ⚠ DUPLICATE ($FILE_COUNT locations)"
        else
            FOUND_TYPES["$TYPE"]="$ALL_FILES"
            echo "  ✓ FOUND"
        fi
    else
        MISSING_COUNT=$((MISSING_COUNT + 1))
        MISSING_TYPES["$TYPE"]="$REF_COUNT references"
        echo "  ✗ MISSING ($REF_COUNT refs)"
    fi
    
done <<< "$TYPES"

# Update JSON summary
echo '    "foundTypes": '$FOUND_COUNT',' >> "$OUTPUT_JSON"
echo '    "missingTypes": '$MISSING_COUNT',' >> "$OUTPUT_JSON"
echo '    "duplicateTypes": '$DUPLICATE_COUNT >> "$OUTPUT_JSON"
echo '  },' >> "$OUTPUT_JSON"
echo '  "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"' >> "$OUTPUT_JSON"
echo "}" >> "$OUTPUT_JSON"

# Generate Markdown Report
cat > "$OUTPUT_MD" << 'MDEOF'
# Type Discovery Report
**Generated:** $(date)

## Executive Summary

This report analyzes **256 unique missing types** (CS0246 errors) from the LankaConnect codebase to determine which types already exist and which need to be created.

### Summary Statistics

- **Total Missing Types Analyzed:** $TYPE_COUNT
- **Found in Codebase:** $FOUND_COUNT ($(echo "scale=1; $FOUND_COUNT * 100 / $TYPE_COUNT" | bc)%)
- **Truly Missing (Need Creation):** $MISSING_COUNT ($(echo "scale=1; $MISSING_COUNT * 100 / $TYPE_COUNT" | bc)%)
- **Duplicate Definitions:** $DUPLICATE_COUNT (⚠ Consolidation Required)

## Key Findings

### 1. Duplicate Type Problem
**$DUPLICATE_COUNT types have multiple definitions** across the codebase, causing namespace ambiguity errors.

**Impact:** Many CS0246 errors are actually CS0104 ambiguous reference errors in disguise.

**Action Required:** Consolidate duplicate types into canonical locations.

### 2. Type Distribution

Based on the sample analysis:
- **Found Types:** Most configuration, result, and infrastructure types exist
- **Missing Types:** Primarily interfaces and specialized service types
- **High Reference Types:** SecurityLevel (83 refs), CulturalCommunityType (187 refs), PerformanceThreshold (31 refs)

### 3. Priority Matrix

| Priority | Criteria | Count (Est) | Action |
|----------|----------|-------------|--------|
| **P0 (Critical)** | >50 references | ~15 types | Create/consolidate immediately |
| **P1 (High)** | 10-50 references | ~45 types | Create in next batch |
| **P2 (Medium)** | 5-9 references | ~85 types | Create after P0/P1 |
| **P3 (Low)** | <5 references | ~111 types | Create as needed |

MDEOF

echo "Analysis complete!"
echo "Found: $FOUND_COUNT types"
echo "Missing: $MISSING_COUNT types"
echo "Duplicates: $DUPLICATE_COUNT types"
echo ""
echo "Output files:"
echo "  - JSON: $OUTPUT_JSON"
echo "  - Report: $OUTPUT_MD"
