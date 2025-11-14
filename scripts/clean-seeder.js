/**
 * Remove all state-level metro areas from MetroAreaSeeder.cs
 * Removes all MetroArea.Create() blocks where isStateLevelArea: true
 */

const fs = require('fs');
const path = require('path');

const seederPath = path.join(__dirname, '../src/LankaConnect.Infrastructure/Data/Seeders/MetroAreaSeeder.cs');

// Read the file
const content = fs.readFileSync(seederPath, 'utf8');

// Pattern to match MetroArea.Create blocks with isStateLevelArea: true
// This matches from MetroArea.Create( to the closing ),
// The [\s\S] matches any character including newlines
const pattern = /MetroArea\.Create\([\s\S]*?isStateLevelArea:\s*true[\s\S]*?\),\s*\n/g;

// Count matches before removal
const matches = content.match(pattern);
console.log(`Found ${matches ? matches.length : 0} state-level metro area entries`);

if (matches) {
    console.log('\nRemoving:');
    matches.forEach((match, i) => {
        const nameMatch = match.match(/name:\s*"([^"]+)"/);
        if (nameMatch) {
            console.log(`  ${i + 1}. ${nameMatch[1]}`);
        }
    });
}

// Remove all matches
const newContent = content.replace(pattern, '');

// Write back
fs.writeFileSync(seederPath, newContent, 'utf8');

console.log(`\n✓ Removed ${matches ? matches.length : 0} state-level metro area entries`);
console.log(`  Original size: ${content.length} chars`);
console.log(`  New size: ${newContent.length} chars`);
console.log(`  Removed: ${content.length - newContent.length} chars`);
