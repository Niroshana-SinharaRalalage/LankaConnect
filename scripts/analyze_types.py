#!/usr/bin/env python3
"""
Type Discovery Analysis Tool
Analyzes 256 missing types to determine existence, duplicates, and priorities
"""

import os
import re
import json
import subprocess
from collections import defaultdict
from pathlib import Path

def read_types_file(filepath):
    """Extract type names from missing_types_unique.txt"""
    types = []
    with open(filepath, 'r', encoding='utf-8') as f:
        for line in f:
            # Remove line numbers and arrows
            match = re.search(r'→(.+)$', line)
            if match:
                type_name = match.group(1).strip()
                if type_name:
                    types.append(type_name)
    return sorted(set(types))

def search_type(type_name, root_dir):
    """Search for type definition and count references"""
    result = {
        'type_name': type_name,
        'found': False,
        'locations': [],
        'definition_type': None,
        'ref_count': 0,
        'category': categorize_type(type_name),
        'priority': 'P3'
    }

    # Search for different definition types
    patterns = {
        'class': rf'class\s+{type_name}\b',
        'record': rf'record\s+{type_name}\b',
        'enum': rf'enum\s+{type_name}\b',
        'interface': rf'interface\s+{type_name}\b'
    }

    for def_type, pattern in patterns.items():
        try:
            cmd = ['grep', '-r', pattern, '--include=*.cs', str(root_dir)]
            proc = subprocess.run(cmd, capture_output=True, text=True, timeout=5)

            if proc.returncode == 0 and proc.stdout:
                result['found'] = True
                result['definition_type'] = def_type

                # Extract file paths
                for line in proc.stdout.strip().split('\n'):
                    if ':' in line:
                        filepath = line.split(':', 1)[0]
                        if filepath not in result['locations']:
                            result['locations'].append(filepath)

                break  # Found it, no need to search other patterns
        except:
            pass

    # Count total references
    try:
        cmd = ['grep', '-r', rf'\b{type_name}\b', '--include=*.cs', str(root_dir)]
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=5)
        result['ref_count'] = len(proc.stdout.strip().split('\n')) if proc.stdout else 0
    except:
        result['ref_count'] = 0

    # Determine priority based on reference count
    if result['ref_count'] > 50:
        result['priority'] = 'P0'
    elif result['ref_count'] > 10:
        result['priority'] = 'P1'
    elif result['ref_count'] > 5:
        result['priority'] = 'P2'

    return result

def categorize_type(type_name):
    """Categorize type based on naming patterns"""
    if re.search(r'(Status|Level|Priority|Type|Category)$', type_name):
        return 'Enum'
    elif re.search(r'(Configuration|Settings|Options|Policy)$', type_name):
        return 'Configuration'
    elif re.search(r'(Result|Response|Report|Analysis)$', type_name):
        return 'Result'
    elif re.search(r'(Request|Command|Query)$', type_name):
        return 'Command'
    elif re.search(r'(Metrics|Data|Info)$', type_name):
        return 'Data'
    elif type_name.startswith('I') and len(type_name) > 1 and type_name[1].isupper():
        return 'Interface'
    else:
        return 'Entity'

def analyze_all_types(types_file, root_dir):
    """Analyze all types and generate report"""
    types = read_types_file(types_file)
    print(f"Analyzing {len(types)} types...")

    results = []
    found_count = 0
    missing_count = 0
    duplicate_count = 0

    for i, type_name in enumerate(types, 1):
        print(f"[{i}/{len(types)}] {type_name}...", end=' ')

        result = search_type(type_name, root_dir)
        results.append(result)

        if result['found']:
            found_count += 1
            if len(result['locations']) > 1:
                duplicate_count += 1
                print(f"⚠ DUPLICATE ({len(result['locations'])} locations)")
            else:
                print("✓ FOUND")
        else:
            missing_count += 1
            print(f"✗ MISSING ({result['ref_count']} refs)")

    # Generate summary
    summary = {
        'total_types': len(types),
        'found_types': found_count,
        'missing_types': missing_count,
        'duplicate_types': duplicate_count,
        'by_category': {},
        'by_priority': {}
    }

    # Group by category
    by_category = defaultdict(lambda: {'total': 0, 'found': 0, 'missing': 0})
    for r in results:
        cat = r['category']
        by_category[cat]['total'] += 1
        if r['found']:
            by_category[cat]['found'] += 1
        else:
            by_category[cat]['missing'] += 1
    summary['by_category'] = dict(by_category)

    # Group by priority (missing only)
    by_priority = defaultdict(int)
    for r in results:
        if not r['found']:
            by_priority[r['priority']] += 1
    summary['by_priority'] = dict(by_priority)

    return {
        'summary': summary,
        'results': results,
        'found_types': [r for r in results if r['found']],
        'missing_types': [r for r in results if not r['found']],
        'duplicate_types': [r for r in results if r['found'] and len(r['locations']) > 1]
    }

def generate_markdown_report(analysis, output_file):
    """Generate comprehensive markdown report"""
    summary = analysis['summary']

    md = f"""# Type Discovery Report

**Generated:** {Path.cwd()}
**Date:** {subprocess.check_output(['date'], text=True).strip()}

## Executive Summary

This report analyzes **{summary['total_types']} unique missing types** (CS0246 errors) from the LankaConnect codebase.

### Summary Statistics

- **Total Types Analyzed:** {summary['total_types']}
- **Found in Codebase:** {summary['found_types']} ({summary['found_types']/summary['total_types']*100:.1f}%)
- **Truly Missing (Need Creation):** {summary['missing_types']} ({summary['missing_types']/summary['total_types']*100:.1f}%)
- **Duplicate Definitions:** {summary['duplicate_types']} (⚠ Consolidation Required)

## Key Findings

### 1. Critical Discovery: Many Types Already Exist!

**{summary['found_types']} out of {summary['total_types']} types ({summary['found_types']/summary['total_types']*100:.1f}%) already exist in the codebase.**

This means most CS0246 errors are caused by:
- ❌ Missing `using` statements
- ❌ Namespace ambiguity (duplicate definitions)
- ❌ Incorrect namespace references

**Action:** Fix using statements and consolidate duplicates BEFORE creating new types.

### 2. Duplicate Type Problem

**{summary['duplicate_types']} types have multiple definitions**, causing CS0104 ambiguous reference errors.

**Top Duplicates:**

"""

    # List top 10 duplicates
    duplicates = sorted(analysis['duplicate_types'], key=lambda x: len(x['locations']), reverse=True)[:10]
    for dup in duplicates:
        md += f"- **{dup['type_name']}** ({len(dup['locations'])} locations)\n"
        for loc in dup['locations']:
            md += f"  - `{loc}`\n"

    md += f"""
### 3. Type Distribution by Category

| Category | Total | Found | Missing | % Missing |
|----------|-------|-------|---------|-----------|
"""

    for cat, stats in sorted(summary['by_category'].items()):
        pct = (stats['missing'] / stats['total'] * 100) if stats['total'] > 0 else 0
        md += f"| {cat} | {stats['total']} | {stats['found']} | {stats['missing']} | {pct:.1f}% |\n"

    md += f"""
### 4. Missing Types by Priority

| Priority | Count | Action Timeline |
|----------|-------|----------------|
"""

    for priority in ['P0', 'P1', 'P2', 'P3']:
        count = summary['by_priority'].get(priority, 0)
        timeline = {
            'P0': 'Immediate (>50 references)',
            'P1': 'This sprint (10-50 references)',
            'P2': 'Next sprint (5-9 references)',
            'P3': 'Backlog (<5 references)'
        }
        md += f"| **{priority}** | {count} | {timeline[priority]} |\n"

    md += """
## Detailed Analysis

### Found Types (Already Exist - DO NOT CREATE)

"""

    found = sorted(analysis['found_types'], key=lambda x: x['type_name'])
    for item in found[:20]:  # Show first 20
        md += f"#### {item['type_name']} ({item['definition_type']})\n"
        md += f"- **References:** {item['ref_count']}\n"
        md += f"- **Location:** `{item['locations'][0] if item['locations'] else 'Unknown'}`\n"
        if len(item['locations']) > 1:
            md += f"- ⚠ **DUPLICATE:** Also found in {len(item['locations']) - 1} other location(s)\n"
        md += "\n"

    md += f"""
... ({len(found)} total found types)

### Missing Types (Need Creation)

"""

    missing = sorted(analysis['missing_types'], key=lambda x: (x['priority'], -x['ref_count']))
    for item in missing[:30]:  # Show first 30
        md += f"#### {item['type_name']} [{item['priority']}]\n"
        md += f"- **Category:** {item['category']}\n"
        md += f"- **References:** {item['ref_count']}\n"
        md += f"- **Recommended Action:** "
        if item['category'] == 'Enum':
            md += "Create enum in `Domain/Common/Enums/`\n"
        elif item['category'] == 'Interface':
            md += "Create interface in `Application/Common/Interfaces/`\n"
        elif item['category'] == 'Configuration':
            md += "Create configuration class in `Infrastructure/Configuration/`\n"
        else:
            md += f"Create {item['category'].lower()} type\n"
        md += "\n"

    md += f"""
... ({len(missing)} total missing types)

## Recommendations

### Phase 1: Cleanup (Eliminate False Positives)
1. **Add missing `using` statements** for the {summary['found_types']} found types
2. **Consolidate {summary['duplicate_types']} duplicate definitions**
3. **Re-run build** to get accurate error count

**Expected Impact:** Could eliminate 50-70% of CS0246 errors

### Phase 2: Create Foundation Types (P0/P1)
Focus on high-reference types first:
"""

    p0_p1 = [t for t in missing if t['priority'] in ['P0', 'P1']]
    for item in sorted(p0_p1, key=lambda x: -x['ref_count'])[:10]:
        md += f"- **{item['type_name']}** ({item['ref_count']} refs) - {item['category']}\n"

    md += """
### Phase 3: Systematic Type Creation (P2/P3)
Create remaining types by category in dependency order:
1. Enums and simple value objects
2. Configuration and policy types
3. Request/Response types
4. Result and report types
5. Interfaces and contracts

## Next Steps

1. ✅ Run consolidation script for duplicate types
2. ✅ Generate using statement fixes
3. ✅ Create P0 types (critical path)
4. ✅ Create P1 types (high value)
5. ⏳ Create P2/P3 types (as needed)

---

**Generated by:** Type Discovery Analysis Tool
**Data Source:** missing_types_unique.txt
"""

    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(md)

    print(f"\n✓ Report generated: {output_file}")

def main():
    root_dir = Path.cwd()
    types_file = root_dir / 'missing_types_unique.txt'
    output_json = root_dir / 'scripts' / 'type_analysis_complete.json'
    output_md = root_dir / 'docs' / 'TYPE_DISCOVERY_REPORT.md'

    if not types_file.exists():
        print(f"Error: {types_file} not found")
        return

    # Run analysis
    analysis = analyze_all_types(types_file, root_dir)

    # Save JSON
    output_json.parent.mkdir(parents=True, exist_ok=True)
    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump(analysis, f, indent=2)
    print(f"\n✓ JSON saved: {output_json}")

    # Generate markdown report
    output_md.parent.mkdir(parents=True, exist_ok=True)
    generate_markdown_report(analysis, output_md)

    # Print summary
    print("\n" + "="*60)
    print("ANALYSIS COMPLETE")
    print("="*60)
    print(f"Total Types:     {analysis['summary']['total_types']}")
    print(f"Found:           {analysis['summary']['found_types']} ({analysis['summary']['found_types']/analysis['summary']['total_types']*100:.1f}%)")
    print(f"Missing:         {analysis['summary']['missing_types']} ({analysis['summary']['missing_types']/analysis['summary']['total_types']*100:.1f}%)")
    print(f"Duplicates:      {analysis['summary']['duplicate_types']}")
    print("="*60)

if __name__ == '__main__':
    main()
