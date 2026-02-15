#!/usr/bin/env python3
"""
Phase 6A.112/113: Automated Email Template Modification Script

This script:
1. Reads exported templates from JSON files
2. Adds "View Signup Forms" button to 14 templates
3. Removes "feel free" text from 8 templates
4. Saves modified HTML to Template_Correction/staging-phase6a113/
"""

import json
import re
import os
from pathlib import Path

# Button HTML to insert
SIGNUP_FORMS_BUTTON_HTML = '''
{{#HasSignupForms}}
<p style="text-align: center; margin: 20px 0;">
    <a href="{{SignupFormsUrl}}" style="color: #FF6600; text-decoration: underline;">View Signup Forms</a>
</p>
{{/HasSignupForms}}
'''

def remove_feel_free_paragraph(html):
    """
    Remove paragraphs containing 'feel free' text.
    Handles multiple patterns:
    - <p>If you have questions, feel free to reply to this email.</p>
    - <p>Feel free to reach out if you have any questions.</p>
    """
    # Pattern 1: Paragraph with "feel free"
    html = re.sub(
        r'<p[^>]*>\s*If you have (?:any )?questions,?\s*feel free to .*?</p>\s*',
        '',
        html,
        flags=re.IGNORECASE | re.DOTALL
    )

    # Pattern 2: Alternative phrasing
    html = re.sub(
        r'<p[^>]*>\s*Feel free to .*?</p>\s*',
        '',
        html,
        flags=re.IGNORECASE | re.DOTALL
    )

    # Pattern 3: Divs with "feel free"
    html = re.sub(
        r'<div[^>]*>\s*(?:If you have (?:any )?questions,?\s*)?feel free to .*?</div>\s*',
        '',
        html,
        flags=re.IGNORECASE | re.DOTALL
    )

    return html

def add_signup_forms_button(html):
    """
    Add View Signup Forms button after View Sign-Up Lists button.
    Placement: After {{/HasSignUpLists}} and before {{#HasOrganizerContact}}
    """
    # Pattern: Find {{/HasSignUpLists}} and insert button after it
    pattern = r'({{/HasSignUpLists}})\s*'

    if re.search(pattern, html):
        # Insert after HasSignUpLists
        html = re.sub(
            pattern,
            r'\1' + SIGNUP_FORMS_BUTTON_HTML + '\n',
            html,
            count=1
        )
    else:
        # Fallback: Insert before {{#HasOrganizerContact}} if no signup lists
        pattern_organizer = r'(\s*{{#HasOrganizerContact}})'
        if re.search(pattern_organizer, html):
            html = re.sub(
                pattern_organizer,
                SIGNUP_FORMS_BUTTON_HTML + r'\1',
                html,
                count=1
            )

    return html

def process_template(template_name, html_body, need_button, need_feel_free_removal):
    """
    Process a single template:
    - Add button if needed
    - Remove feel free text if needed
    """
    modified_html = html_body
    changes_made = []

    if need_button:
        modified_html = add_signup_forms_button(modified_html)
        changes_made.append("Added 'View Signup Forms' button")

    if need_feel_free_removal:
        before_len = len(modified_html)
        modified_html = remove_feel_free_paragraph(modified_html)
        after_len = len(modified_html)

        if before_len != after_len:
            changes_made.append(f"Removed 'feel free' text ({before_len - after_len} chars)")
        else:
            changes_made.append("[WARN]  'feel free' text NOT FOUND (manual check needed)")

    return modified_html, changes_made

def main():
    print("=" * 80)
    print("Phase 6A.112/113: Email Template Modification Script")
    print("=" * 80)
    print()

    # Load exported templates
    signup_forms_file = Path("phase6a112_signup_forms_button_templates.json")
    feel_free_file = Path("phase6a112_feel_free_templates.json")

    if not signup_forms_file.exists():
        print(f"[ERROR] Error: {signup_forms_file} not found!")
        print("   Run: dotnet run (in scripts/ExportEmailTemplates)")
        return

    if not feel_free_file.exists():
        print(f"[ERROR] Error: {feel_free_file} not found!")
        print("   Run: dotnet run (in scripts/ExportEmailTemplates)")
        return

    with open(signup_forms_file, 'r', encoding='utf-8') as f:
        signup_forms_templates = json.load(f)

    with open(feel_free_file, 'r', encoding='utf-8') as f:
        feel_free_templates = json.load(f)

    # Build sets for quick lookup
    need_button = {t['name'] for t in signup_forms_templates}
    need_feel_free_removal = {t['name'] for t in feel_free_templates}

    print(f"Templates needing button: {len(need_button)}")
    print(f"Templates needing 'feel free' removal: {len(need_feel_free_removal)}")
    print(f"Templates needing BOTH: {len(need_button & need_feel_free_removal)}")
    print()

    # Create output directory
    output_dir = Path("../Template_Correction/staging-phase6a113")
    output_dir.mkdir(parents=True, exist_ok=True)

    # Process all unique templates
    all_templates = {t['name']: t for t in signup_forms_templates}
    all_templates.update({t['name']: t for t in feel_free_templates})

    modified_count = 0

    for template_name, template_data in sorted(all_templates.items()):
        needs_button = template_name in need_button
        needs_removal = template_name in need_feel_free_removal

        if not needs_button and not needs_removal:
            continue

        print(f"[*] Processing: {template_name}")

        modified_html, changes = process_template(
            template_name,
            template_data['html_body'],
            needs_button,
            needs_removal
        )

        for change in changes:
            print(f"   [+] {change}")

        # Save modified template
        output_file = output_dir / f"{template_name}-modified.html"
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(modified_html)

        print(f"   [>] Saved to: {output_file.name}")
        print()

        modified_count += 1

    print("=" * 80)
    print(f"[OK] Modified {modified_count} templates")
    print(f"[DIR] Output directory: {output_dir.absolute()}")
    print()
    print("Next steps:")
    print("1. Review modified templates in Template_Correction/staging-phase6a113/")
    print("2. Run: python create_migration_phase6a113.py")
    print("3. Test migration locally")
    print("4. Deploy to staging")

if __name__ == "__main__":
    main()
