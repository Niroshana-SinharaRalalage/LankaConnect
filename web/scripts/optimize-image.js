#!/usr/bin/env node

/**
 * Image Optimization Script
 * Resizes and optimizes the Sri Lankan background image for web use
 */

const fs = require('fs');
const path = require('path');

const INPUT_PATH = path.join(__dirname, '../public/images/sri-lankan-background.jpg');
const OUTPUT_PATH = path.join(__dirname, '../public/images/sri-lankan-background-optimized.jpg');
const BACKUP_PATH = path.join(__dirname, '../public/images/sri-lankan-background-original.jpg');

console.log('🖼️  Image Optimization Script');
console.log('==============================\n');

// Check if file exists
if (!fs.existsSync(INPUT_PATH)) {
  console.error('❌ Error: Source image not found at:', INPUT_PATH);
  process.exit(1);
}

// Get file size
const stats = fs.statSync(INPUT_PATH);
const fileSizeInMB = (stats.size / (1024 * 1024)).toFixed(2);
console.log(`📊 Original file size: ${fileSizeInMB} MB`);

// Try to use Sharp if available
let sharp;
try {
  sharp = require('sharp');
  console.log('✅ Using Sharp for image optimization\n');
} catch (err) {
  console.log('⚠️  Sharp not found. Installing sharp...\n');
  const { execSync } = require('child_process');
  try {
    execSync('npm install --save-dev sharp', { stdio: 'inherit', cwd: path.join(__dirname, '..') });
    sharp = require('sharp');
    console.log('\n✅ Sharp installed successfully\n');
  } catch (installErr) {
    console.error('❌ Failed to install Sharp:', installErr.message);
    console.log('\n📝 Manual installation required:');
    console.log('   Run: npm install --save-dev sharp');
    process.exit(1);
  }
}

// Backup original file
console.log('💾 Creating backup of original image...');
fs.copyFileSync(INPUT_PATH, BACKUP_PATH);
console.log(`✅ Backup saved to: ${BACKUP_PATH}\n`);

// Optimize image
console.log('🔧 Optimizing image...');
console.log('   - Target resolution: 1920x1280 (maintaining aspect ratio)');
console.log('   - Target format: JPEG');
console.log('   - Target quality: 85%');
console.log('   - Target size: < 500KB\n');

sharp(INPUT_PATH)
  .resize(1920, 1280, {
    fit: 'cover',
    position: 'center'
  })
  .jpeg({
    quality: 85,
    progressive: true,
    mozjpeg: true
  })
  .toFile(OUTPUT_PATH)
  .then(info => {
    const outputSizeInKB = (info.size / 1024).toFixed(2);
    const outputSizeInMB = (info.size / (1024 * 1024)).toFixed(2);

    console.log('✅ Optimization complete!');
    console.log(`   - Output size: ${outputSizeInKB} KB (${outputSizeInMB} MB)`);
    console.log(`   - Dimensions: ${info.width}x${info.height}`);
    console.log(`   - Format: ${info.format}`);
    console.log(`   - Compression ratio: ${((1 - info.size / stats.size) * 100).toFixed(1)}%\n`);

    // Replace original with optimized
    console.log('🔄 Replacing original with optimized version...');
    fs.unlinkSync(INPUT_PATH);
    fs.renameSync(OUTPUT_PATH, INPUT_PATH);
    console.log('✅ Done!\n');

    console.log('📁 Files:');
    console.log(`   - Optimized image: ${INPUT_PATH}`);
    console.log(`   - Original backup: ${BACKUP_PATH}`);
    console.log('\n🎉 Image optimization complete! The background image is now ready for use.');

    if (info.size > 500 * 1024) {
      console.log('\n⚠️  Note: Image is still larger than 500KB. Consider reducing quality further if needed.');
    }
  })
  .catch(err => {
    console.error('❌ Optimization failed:', err.message);
    process.exit(1);
  });
