/**
 * Script to remove state-level metro areas from MetroAreaSeeder.cs
 * Removes all entries with isStateLevelArea: true
 */

const fs = require('fs');
const path = require('path');

const seederPath = path.join(__dirname, '../src/LankaConnect.Infrastructure/Data/Seeders/MetroAreaSeeder.cs');

// Read the file
const content = fs.readFileSync(seederPath, 'utf8');

// Split by Metro area Create calls
const lines = content.split('\n');
let newLines = [];
let inStateLevelBlock = false;
let blockDepth = 0;
let skipBlock = false;

for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Detect start of MetroArea.Create block
    if (line.trim().startsWith('MetroArea.Create(')) {
        // Look ahead to check if this is a state-level area
        let isStateLevel = false;
        for (let j = i; j < Math.min(i + 15, lines.length); j++) {
            if (lines[j].includes('isStateLevelArea: true')) {
                isStateLevel = true;
                break;
            }
            if (lines[j].includes('),')) {
                break; // End of this Create block
            }
        }

        if (isStateLevel) {
            skipBlock = true;
            inStateLevelBlock = true;
            blockDepth = 1;
            continue; // Skip this line
        }
    }

    // If we're in a block to skip
    if (skipBlock) {
        // Count parentheses to detect end of block
        const openParens = (line.match(/\(/g) || []).length;
        const closeParens = (line.match(/\)/g) || []).length;
        blockDepth += openParens - closeParens;

        // Check if block ends with ),
        if (line.trim().endsWith('),')) {
            skipBlock = false;
            inStateLevelBlock = false;
            blockDepth = 0;

            // Also skip the next blank line if present
            if (i + 1 < lines.length && lines[i + 1].trim() === '') {
                i++; // Skip blank line
            }
            continue;
        }
        continue; // Skip this line
    }

    newLines.push(line);
}

// Write back
fs.writeFileSync(seederPath, newLines.join('\n'), 'utf8');

console.log('✓ Removed all state-level metro area entries from MetroAreaSeeder.cs');
console.log(`  Original lines: ${lines.length}`);
console.log(`  New lines: ${newLines.length}`);
console.log(`  Removed: ${lines.length - newLines.length} lines`);
