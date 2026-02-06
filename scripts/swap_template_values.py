import re

# Read the migration file
with open(r'c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Pattern to match each INSERT statement for the 5 new templates
# We need to swap the 7th value (HTML) with the 8th value (text)
# Format: subject, html_template, text_template, is_active, created_at

def swap_values(match):
    """Swap the 7th and 8th values in the SELECT statement"""
    full_match = match.group(0)

    # Extract the part between subject and is_active
    # This contains: subject, value7 (currently HTML), value8 (currently text)

    # Find the subject line (ends with ',)
    subject_match = re.search(r"'[^']+',\s*\n", full_match)
    if not subject_match:
        return full_match

    subject_end = subject_match.end()

    # Everything after subject until is_active line
    after_subject = full_match[subject_end:]

    # Find the two string values (they start with ' and can contain escaped quotes '')
    # Value 7 (HTML - very long) and Value 8 (text - shorter)

    # Match first value (HTML)
    value7_match = re.match(r"(\s*'(?:[^']|'')*',)\s*\n", after_subject)
    if not value7_match:
        return full_match

    value7 = value7_match.group(1)
    value7_end = value7_match.end()

    # Match second value (text)
    after_value7 = after_subject[value7_end:]
    value8_match = re.match(r"(\s*'(?:[^']|'')*',)\s*\n", after_value7)
    if not value8_match:
        return full_match

    value8 = value8_match.group(1)
    value8_end = value8_match.end()

    # Rebuild with swapped values
    before = full_match[:subject_end]
    after = after_subject[value7_end + value8_end:]

    return before + value8 + "\n" + value7 + "\n" + after

# Process each of the 5 INSERT statements
# Match pattern: from subject_template line to is_active line
pattern = r"('(?:Reset Your Password|Your Password Has Been Changed|Welcome to LankaConnect!|Your RSVP Confirmation - \{\{EventTitle\}\}|Congratulations! You''re Now an Event Organizer) - LankaConnect',\s*\n\s*'[^']*(?:''[^']*)*',\s*\n\s*'[^']*(?:''[^']*)*',\s*\n\s*true,)"

content = re.sub(pattern, swap_values, content, flags=re.MULTILINE | re.DOTALL)

# Write back
with open(r'c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("Template values swapped successfully")
