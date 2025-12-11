#!/usr/bin/env python3
"""
Remove state-level metro areas from MetroAreaSeeder.cs
"""

import re

# Read the file
with open('src/LankaConnect.Infrastructure/Data/Seeders/MetroAreaSeeder.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Pattern to match MetroArea.Create blocks with isStateLevelArea: true
# This matches from MetroArea.Create( to the closing ),
pattern = r'MetroArea\.Create\([^)]*?isStateLevelArea:\s*true[^)]*?\),\s*\n'

# Count matches before removal
matches = re.findall(pattern, content, re.DOTALL)
print(f"Found {len(matches)} state-level metro area entries")

# Remove all matches
new_content = re.sub(pattern, '', content, flags=re.DOTALL)

# Write back
with open('src/LankaConnect.Infrastructure/Data/Seeders/MetroAreaSeeder.cs', 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"✓ Removed all {len(matches)} state-level metro area entries")
print(f"  Original size: {len(content)} chars")
print(f"  New size: {len(new_content)} chars")
