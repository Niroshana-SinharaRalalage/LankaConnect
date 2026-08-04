/**
 * Generates the LankaSeyla logo assets.
 *
 *   1. A transparent-background master, written back to the brand Logo Files
 *      folder as LankaSeyla-transparent.png. Same pixel dimensions as the
 *      supplied JPEG (1600x887) — the ONLY change is that the black background
 *      becomes transparent. No cropping, scaling, recolouring or reflowing.
 *
 *   2. web/public/lanka-seyla.png — that master trimmed of its now-transparent
 *      padding and scaled to a fixed 110px height. Its natural width rides along
 *      (~443px), so the letterforms and the "by LankaConnect" sub-line keep their
 *      real proportions. The card does not need to mirror the LankaEvents
 *      button's square-icon slot.
 *
 * How the transparency is derived: the artwork is light text on a flat black
 * field, so a pixel's max channel is a direct measure of ink coverage. Measured
 * on the source, the background occupies 0-15 and the solid letterforms plateau
 * at >=200, with only antialiased edges in between. Ramping alpha across that gap
 * leaves the glyphs fully opaque and the field fully clear.
 *
 * RGB is copied through untouched rather than un-premultiplied, so the brand gold
 * stays exactly #D7A959 instead of being brightened toward #FFC869.
 *
 * Committed rather than left as a one-off so the assets are reproducible instead
 * of unexplained binaries. See
 * docs/superpowers/specs/2026-08-04-lankaseyla-landing-entry-design.md
 *
 * Usage:  node scripts/generate-lanka-seyla-logo.js [path/to/LankaSeyla.jpeg]
 */

const path = require('path');
const sharp = require(path.join(__dirname, '..', 'web', 'node_modules', 'sharp'));

const LOGO_DIR = 'C:/Niroshan/LankaConnect Marketplace/Logo Files';
const SRC = process.argv[2] || path.join(LOGO_DIR, 'LankaSeyla.jpeg');
const MASTER = path.join(LOGO_DIR, 'LankaSeyla-transparent.png');
const DEST = path.join(__dirname, '..', 'web', 'public', 'lanka-seyla.png');

/** Alpha ramp bounds, in max-channel units. Below ALPHA_LO is background. */
const ALPHA_LO = 16;
const ALPHA_HI = 200;

/** Fixed output height for the web asset; width follows the wordmark's aspect. */
const OUT_HEIGHT = 110;

async function main() {
  // ── 1. Transparent master ───────────────────────────────────────────────────
  const { data, info } = await sharp(SRC).raw().toBuffer({ resolveWithObject: true });
  const px = info.width * info.height;
  const rgba = Buffer.alloc(px * 4);

  for (let i = 0; i < px; i++) {
    const s = i * info.channels;
    const r = data[s], g = data[s + 1], b = data[s + 2];
    const coverage = Math.max(r, g, b);

    let alpha;
    if (coverage <= ALPHA_LO) alpha = 0;
    else if (coverage >= ALPHA_HI) alpha = 255;
    else alpha = Math.round(((coverage - ALPHA_LO) / (ALPHA_HI - ALPHA_LO)) * 255);

    const d = i * 4;
    rgba[d] = r;
    rgba[d + 1] = g;
    rgba[d + 2] = b;
    rgba[d + 3] = alpha;
  }

  await sharp(rgba, { raw: { width: info.width, height: info.height, channels: 4 } })
    .png()
    .toFile(MASTER);

  // ── 2. Web asset, derived from the master ───────────────────────────────────
  const trimmed = await sharp(MASTER).trim().png().toBuffer({ resolveWithObject: true });
  const out = await sharp(trimmed.data).resize({ height: OUT_HEIGHT }).png().toFile(DEST);

  console.log(`source   ${info.width}x${info.height}`);
  console.log(`master   ${MASTER} (${info.width}x${info.height}, transparent)`);
  console.log(`trimmed  ${trimmed.info.width}x${trimmed.info.height}`);
  console.log(`written  ${DEST} (${out.width}x${out.height})`);
  console.log(`aspect   ${(out.width / out.height).toFixed(2)}:1 — card renders at half height`);
}

main().catch(err => {
  console.error('Failed to generate LankaSeyla logo assets:', err);
  process.exit(1);
});
