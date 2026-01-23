import re

# Read the file
filepath = r'c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs'
with open(filepath, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Process line by line
output_lines = []
i = 0
while i < len(lines):
    line = lines[i]
    output_lines.append(line)

    # Check if this is a line with duplicate NOW()
    stripped = line.strip()
    if stripped == 'NOW()':
        # Check if next line is also NOW()
        if i + 1 < len(lines) and lines[i + 1].strip() == 'NOW()':
            # Skip the next NOW() line
            i += 2
            continue

    i += 1

# Write back
with open(filepath, 'w', encoding='utf-8') as f:
    f.writelines(output_lines)

print("Removed duplicate NOW() calls")

# Now swap the HTML and text values
# For each INSERT, the pattern is:
# Line N: subject (short string ending with 'LankaConnect',)
# Line N+1: Currently HTML (very long)
# Line N+2: Currently text (short)
# We need to swap N+1 and N+2

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Find each INSERT block and swap values 7 and 8 (HTML and text templates)
# Pattern: subject line, then HTML line, then text line
def swap_templates(match):
    subject_line = match.group(1)
    html_line = match.group(2)
    text_line = match.group(3)
    rest = match.group(4)

    # Swap: text should come before HTML
    return subject_line + text_line + html_line + rest

# Match pattern: subject, HTML (starts with <!DOCTYPE), text (short), then true,
pattern = r"('(?:Reset Your Password|Your Password Has Been Changed|Welcome to LankaConnect!|Your RSVP Confirmation - \{\{EventTitle\}\}|Congratulations! You're Now an Event Organizer) - LankaConnect',\s*\n)(\s*'<!DOCTYPE[^']*(?:''[^']*)*',\s*\n)(\s*'[^']*(?:''[^']*)*',\s*\n)(\s*true,)"

content = re.sub(pattern, swap_templates, content, flags=re.DOTALL)

# Write back
with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print("Swapped template values - text now before HTML")
