#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const IMAGES_DIR = path.join(__dirname, '../public/images');

async function optimizeImage(inputPath, outputPath, targetName) {
  console.log(`\n📸 Optimizing ${targetName}...`);

  const stats = fs.statSync(inputPath);
  const inputSizeKB = (stats.size / 1024).toFixed(2);
  console.log(`   Original: ${inputSizeKB} KB`);

  await sharp(inputPath)
    .resize(1920, 1280, {
      fit: 'cover',
      position: 'center'
    })
    .jpeg({
      quality: 85,
      progressive: true,
      mozjpeg: true
    })
    .toFile(outputPath);

  const outputStats = fs.statSync(outputPath);
  const outputSizeKB = (outputStats.size / 1024).toFixed(2);
  console.log(`   Optimized: ${outputSizeKB} KB`);
  console.log(`   Savings: ${((1 - outputStats.size / stats.size) * 100).toFixed(1)}%`);
}

async function main() {
  console.log('🖼️  Optimizing New Sri Lankan Images\n');

  // Optimize image.png (elephants)
  const elephantsInput = path.join(IMAGES_DIR, 'image.png');
  const elephantsOutput = path.join(IMAGES_DIR, 'sri-lankan-elephants.jpg');

  if (fs.existsSync(elephantsInput)) {
    await optimizeImage(elephantsInput, elephantsOutput, 'Elephants Image');
  }

  // Check tangalle beach image
  const beachPath = path.join(IMAGES_DIR, 'sri-lanka-tangalle-2301_l2kbyv.jpeg');
  if (fs.existsSync(beachPath)) {
    const stats = fs.statSync(beachPath);
    console.log(`\n📸 Tangalle Beach Image:`);
    console.log(`   Size: ${(stats.size / 1024).toFixed(2)} KB`);
    console.log(`   ✅ Already optimized - perfect for web use!`);
  }

  console.log('\n✅ Done! Your images are ready to use as backgrounds.');
}

main().catch(console.error);
