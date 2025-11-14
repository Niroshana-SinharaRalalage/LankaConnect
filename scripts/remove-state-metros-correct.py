#!/usr/bin/env python3
"""
Correctly remove ONLY state-level metro areas from MetroAreaSeeder.cs
by identifying blocks with isStateLevelArea: true
"""

import re

file_path = 'src/LankaConnect.Infrastructure/Data/Seeders/MetroAreaSeeder.cs'

# Read the file
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Split content into lines for processing
lines = content.split('\n')
new_lines = []
skip_lines = False
brace_count = 0
collecting_block = []
block_has_state_level_true = False

i = 0
while i < len(lines):
    line = lines[i]

    # Check if this is the start of a MetroArea.Create block
    if 'MetroArea.Create(' in line:
        # Start collecting this block
        collecting_block = [line]
        brace_count = line.count('(') - line.count(')')
        block_has_state_level_true = False
        i += 1

        # Collect the entire block until we find the closing ),
        while i < len(lines):
            line = lines[i]
            collecting_block.append(line)

            # Check if this line contains isStateLevelArea: true
            if 'isStateLevelArea:' in line and 'true' in line:
                block_has_state_level_true = True

            # Check if block ends with ),
            if line.strip().endswith('),'):
                # Block is complete
                if block_has_state_level_true:
                    # Skip this block - it's a state-level metro
                    print(f"Removing state-level metro block")
                    # Check for blank line after and skip it too
                    if i + 1 < len(lines) and lines[i + 1].strip() == '':
                        i += 1
                else:
                    # Keep this block - it's a city metro
                    new_lines.extend(collecting_block)
                collecting_block = []
                break

            i += 1
    else:
        # Not part of a MetroArea.Create block, keep the line
        new_lines.append(line)

    i += 1

# Write back
with open(file_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(new_lines))

print(f"\nSuccessfully removed state-level metro area entries")
print(f"  Original lines: {len(lines)}")
print(f"  New lines: {len(new_lines)}")
print(f"  Removed: {len(lines) - len(new_lines)} lines")
