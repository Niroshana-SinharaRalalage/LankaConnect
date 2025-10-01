#!/bin/bash
# Batch type search using Grep patterns
OUTPUT_FILE="scripts/type_search_batch_results.txt"
> "$OUTPUT_FILE"

# Sample 20 types to test the approach
TYPES=(
"AccessPatternAnalysis"
"AlertConfiguration"
"BackupConfiguration"
"CulturalAffinityCalculation"
"DataBreachIncident"
"FailoverConfiguration"
"GDPRComplianceResult"
"IncidentSeverity"
"MonitoringConfiguration"
"PerformanceThreshold"
"SecurityLevel"
"UserSession"
"ICrossCulturalDiscoveryService"
"CulturalCommunityType"
"AlertSuppressionPolicy"
"ComplianceStandard"
"DisasterRecoveryProcedure"
"RegionalComplianceStatus"
"ScalingMetrics"
"ThreatIntelligence"
)

for TYPE in "${TYPES[@]}"; do
    echo "=== $TYPE ===" >> "$OUTPUT_FILE"
    
    # Class search
    echo "CLASS:" >> "$OUTPUT_FILE"
    grep -r "class $TYPE\b" --include="*.cs" . 2>/dev/null | head -3 >> "$OUTPUT_FILE" || echo "None" >> "$OUTPUT_FILE"
    
    # Record search
    echo "RECORD:" >> "$OUTPUT_FILE"
    grep -r "record $TYPE\b" --include="*.cs" . 2>/dev/null | head -3 >> "$OUTPUT_FILE" || echo "None" >> "$OUTPUT_FILE"
    
    # Enum search
    echo "ENUM:" >> "$OUTPUT_FILE"
    grep -r "enum $TYPE\b" --include="*.cs" . 2>/dev/null | head -3 >> "$OUTPUT_FILE" || echo "None" >> "$OUTPUT_FILE"
    
    # Interface search
    echo "INTERFACE:" >> "$OUTPUT_FILE"
    grep -r "interface $TYPE\b" --include="*.cs" . 2>/dev/null | head -3 >> "$OUTPUT_FILE" || echo "None" >> "$OUTPUT_FILE"
    
    # Count occurrences
    COUNT=$(grep -r "\b$TYPE\b" --include="*.cs" . 2>/dev/null | wc -l)
    echo "REFERENCES: $COUNT" >> "$OUTPUT_FILE"
    echo "" >> "$OUTPUT_FILE"
done

echo "Search complete. Results in $OUTPUT_FILE"
