/**
 * Generates web/public/lanka-seyla.png — the 110×110 square logo used by the
 * LankaSeyla entry card on the umbrella landing page.
 *
 * Why this script exists: the supplied brand artwork is a 4.03:1 wordmark
 * (1268×315 after trimming its black padding). Letterboxed into a 110×110 square
 * it renders 14px tall at the card's 55×55 display size — illegible. So the
 * square asset is built as a stacked lockup: "Lanka" (white) over "Seyla" (gold),
 * cut from the real letterforms, sharing one scale factor so the original type
 * proportions survive.
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

// Near-black to match the wordmark's own background.
const BG = { r: 10, g: 10, b: 10, alpha: 1 };

// Geometry of the trimmed wordmark (1268×315). The main line sits above the
// "by LankaConnect" sub-line, which is dropped — it is unreadable at this size.
const MAIN_LINE_HEIGHT = 245;
const LANKA_WIDTH = 700;

const OUT_SIZE = 110;
const INNER = 408; // width of the wider word inside the lockup canvas
const PAD = 26;
const GAP = 6;

async function main() {
  const trimmed = await sharp(SRC).trim({ threshold: 20 }).png().toBuffer();
  const meta = await sharp(trimmed).metadata();

  const lankaRaw = await sharp(trimmed)
    .extract({ left: 0, top: 0, width: LANKA_WIDTH, height: MAIN_LINE_HEIGHT })
    .png().toBuffer();
  const seylaRaw = await sharp(trimmed)
    .extract({ left: LANKA_WIDTH, top: 0, width: meta.width - LANKA_WIDTH, height: MAIN_LINE_HEIGHT })
    .png().toBuffer();

  // One scale factor for both words so their relative type size is preserved.
  const scale = INNER / LANKA_WIDTH;
  const lk = await sharp(lankaRaw)
    .resize({ width: Math.round(LANKA_WIDTH * scale) })
    .png().toBuffer({ resolveWithObject: true });
  const sy = await sharp(seylaRaw)
    .resize({ width: Math.round((meta.width - LANKA_WIDTH) * scale) })
    .png().toBuffer({ resolveWithObject: true });

  const canvasW = INNER + PAD * 2;
  const canvasH = PAD * 2 + lk.info.height + GAP + sy.info.height;

  const stacked = await sharp({
    create: { width: canvasW, height: canvasH, channels: 4, background: BG },
  })
    .composite([
      { input: lk.data, left: Math.round((canvasW - lk.info.width) / 2), top: PAD },
      {
        input: sy.data,
        left: Math.round((canvasW - sy.info.width) / 2),
        top: PAD + lk.info.height + GAP,
      },
    ])
    .png().toBuffer();

  const side = Math.max(canvasW, canvasH);
  await sharp(stacked)
    .resize(side, side, { fit: 'contain', background: BG })
    .resize(OUT_SIZE, OUT_SIZE)
    .png()
    .toFile(DEST);

  console.log(`source   ${meta.width}x${meta.height} (trimmed)`);
  console.log(`lockup   ${canvasW}x${canvasH} -> square ${side}`);
  console.log(`written  ${DEST} (${OUT_SIZE}x${OUT_SIZE})`);
}

main().catch(err => {
  console.error('Failed to generate lanka-seyla.png:', err);
  process.exit(1);
});
