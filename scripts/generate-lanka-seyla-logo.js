/**
 * Generates web/public/lanka-seyla.png — the LankaSeyla wordmark used by the
 * entry card on the umbrella landing page.
 *
 * The wordmark is used AS SUPPLIED: the only transform is trimming the flat black
 * padding and scaling to a fixed 110px height. Its natural width rides along
 * (~443px at that height), so the letterforms and the "by LankaConnect" sub-line
 * keep their real proportions. The card does not need to mirror the LankaEvents
 * button's square-icon slot.
 *
 * The black background is kept deliberately — LankaSeyla's own site header sets
 * the wordmark in a black box, so it reads as brand treatment, not as an artifact.
 *
 * Committed rather than left as a one-off so the asset is reproducible instead of
 * an unexplained binary. See
 * docs/superpowers/specs/2026-08-04-lankaseyla-landing-entry-design.md
 *
 * Usage:  node scripts/generate-lanka-seyla-logo.js [path/to/LankaSeyla.jpeg]
 */

const path = require('path');
const sharp = require(path.join(__dirname, '..', 'web', 'node_modules', 'sharp'));

const SRC = process.argv[2] || 'C:/Niroshan/LankaConnect Marketplace/Logo Files/LankaSeyla.jpeg';
const DEST = path.join(__dirname, '..', 'web', 'public', 'lanka-seyla.png');

/** Fixed output height; width follows from the wordmark's own aspect ratio. */
const OUT_HEIGHT = 110;

async function main() {
  const trimmed = await sharp(SRC).trim({ threshold: 20 }).png().toBuffer();
  const meta = await sharp(trimmed).metadata();

  const out = await sharp(trimmed)
    .resize({ height: OUT_HEIGHT })
    .png()
    .toFile(DEST);

  console.log(`source   ${meta.width}x${meta.height} (trimmed)`);
  console.log(`written  ${DEST} (${out.width}x${out.height})`);
  console.log(`aspect   ${(out.width / out.height).toFixed(2)}:1 — card renders at half height`);
}

main().catch(err => {
  console.error('Failed to generate lanka-seyla.png:', err);
  process.exit(1);
});
