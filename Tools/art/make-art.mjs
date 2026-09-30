// Placeholder art for the Casual Game project: `npm run build` (in Tools/art).
//
// Every sprite is drawn as SVG, rendered to PNG, then shelf-packed into a few sprite sheets under
// Assets/_Game/Art/Sheets. Each sheet gets a sidecar <sheet>.json (rects, 9-slice borders, pivots) that
// the Unity importer (SheetSlicer) turns into sliced sprites automatically — no manual Sprite Editor work.
//
// Reuse rules that keep the sheets small:
//  - bodies (circle / block) and arrow tiles are WHITE and tinted in Unity; outline + highlight live on a
//    separate untinted "line" sprite drawn on top.
//  - faces are a Texture2DArray built by make-faces.mjs (faces.mjs is the one source, shared with the mockup).
//  - buttons, panels, bars, the jar and the board frame are 9-sliced.
//
// Final art: drop a same-named PNG into Tools/art/override/<sheet>/<name>.png (e.g. from ChatGPT) and rebuild.
// Overrides are scaled to the placeholder's size, so sheet layout and Unity slicing stay identical.
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(HERE, '../../Assets/_Game/Art/Sheets');
const OVERRIDE = path.join(HERE, 'override');
const PREVIEW = path.join(HERE, 'preview');

const C = {
  ink: '#1E2240', paper: '#FFF8EC', white: '#FFFFFF',
  red: '#FF5A5F', orange: '#FF9F1C', yellow: '#FFD23F', green: '#3DDC97', blue: '#4EA8DE', purple: '#9B5DE5', pink: '#F15BB5',
  gray: '#AEB3C8', mouth: '#7A2436', tongue: '#FF7A93', tear: '#6FC3F7', blush: '#FF8FA3',
};

// ---------- svg helpers ----------
const svg = (w, h, body, vb = `0 0 ${w} ${h}`) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="${vb}">${body}</svg>`;
const circle = (cx, cy, r, fill, stroke = 'none', sw = 0, extra = '') =>
  `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const ellipse = (cx, cy, rx, ry, fill, stroke = 'none', sw = 0, extra = '') =>
  `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const line = (d, col = C.ink, sw = 9, extra = '') =>
  `<path d="${d}" fill="none" stroke="${col}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
const shape = (d, fill, stroke = C.ink, sw = 6, extra = '') =>
  `<path d="${d}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" stroke-linejoin="round" ${extra}/>`;
const rot = (deg, cx, cy) => `transform="rotate(${deg} ${cx} ${cy})"`;

function starPts(cx, cy, rOut, rIn, n = 5, rotDeg = 0) {
  const pts = [];
  for (let i = 0; i < n * 2; i++) {
    const r = i % 2 ? rIn : rOut;
    const a = ((rotDeg - 90) * Math.PI) / 180 + (i * Math.PI) / n;
    pts.push(`${(cx + r * Math.cos(a)).toFixed(1)},${(cy + r * Math.sin(a)).toFixed(1)}`);
  }
  return pts.join(' ');
}
const heartD = (cx, cy, s) =>
  `M${cx},${cy + s * 0.9} C${cx - s * 1.5},${cy - s * 0.1} ${cx - s * 0.9},${cy - s * 1.2} ${cx},${cy - s * 0.4} ` +
  `C${cx + s * 0.9},${cy - s * 1.2} ${cx + s * 1.5},${cy - s * 0.1} ${cx},${cy + s * 0.9} Z`;
function spiralD(cx, cy, r, rotDeg, turns = 2.2) {
  const pts = [];
  for (let t = 0; t <= 1.0001; t += 0.02) {
    const a = t * turns * Math.PI * 2 + (rotDeg * Math.PI) / 180;
    pts.push(`${(cx + r * t * Math.cos(a)).toFixed(1)},${(cy + r * t * Math.sin(a)).toFixed(1)}`);
  }
  return `M${pts.join(' L')}`;
}

// ---------- sprite definitions per sheet ----------
const sprites = { shapes: [], ui: [], fx: [] };
const add = (sheet, name, w, h, body, opts = {}) => sprites[sheet].push({ name, w, h, body, ...opts });

// Faces are no longer sprites: make-faces.mjs builds the Texture2DArray atlas (Assets/_Game/Art/Faces/faces.png).

// Bodies: white fill (tinted) + untinted line layer.
add('shapes', 'circle_fill', 256, 256, circle(128, 128, 122, C.white));
add('shapes', 'circle_line', 256, 256, circle(128, 128, 116, 'none', C.ink, 12) + ellipse(84, 72, 30, 18, C.white, 'none', 0, `opacity="0.55" ${rot(-35, 84, 72)}`));
add('shapes', 'block_fill', 256, 256, `<rect x="6" y="6" width="244" height="244" rx="52" fill="${C.white}"/>`);
add('shapes', 'block_line', 256, 256, `<rect x="11" y="11" width="234" height="234" rx="48" fill="none" stroke="${C.ink}" stroke-width="10"/>` +
  `<rect x="40" y="26" width="84" height="20" rx="10" fill="${C.white}" opacity="0.55"/>`);
// Arrow Out tiles (white, tinted per color), authored pointing up / entering from below.
const SW = 26;
const aStroke = (cap = 'butt') => `fill="none" stroke="${C.white}" stroke-width="${SW}" stroke-linecap="${cap}" stroke-linejoin="round"`;
add('shapes', 'arrow_head', 128, 128, `<path d="M64,128 L64,34" ${aStroke()}/><polyline points="28,68 64,30 100,68" ${aStroke('round')}/>`);
add('shapes', 'arrow_body', 128, 128, `<path d="M64,0 L64,128" ${aStroke()}/>`);
add('shapes', 'arrow_corner', 128, 128, `<path d="M64,128 A64,64 0 0 1 128,64" ${aStroke()}/>`);
add('shapes', 'arrow_tail', 128, 128, `<path d="M64,0 L64,64" ${aStroke()}/>` + circle(64, 64, SW * 0.85, C.white));
add('shapes', 'arrow_single', 128, 128, `<path d="M64,108 L64,36" ${aStroke('round')}/><polyline points="32,64 64,30 96,64" ${aStroke('round')}/>`);
add('shapes', 'grid_dot', 32, 32, circle(16, 16, 7, C.white));
add('shapes', 'dot', 64, 64, circle(32, 32, 28, C.white));
// Jar (Eye Merge) and board frame (Eye Blast): 9-sliced.
add('shapes', 'jar_back', 256, 256, `<rect x="8" y="8" width="240" height="240" rx="70" fill="${C.white}"/>`, { border: [90, 90, 90, 90] });
add('shapes', 'jar_line', 256, 256, `<rect x="14" y="14" width="228" height="228" rx="64" fill="none" stroke="${C.ink}" stroke-width="16"/>`, { border: [90, 90, 90, 90] });
add('shapes', 'danger_dash', 64, 16, `<rect x="4" y="2" width="40" height="12" rx="6" fill="${C.white}"/>`);
add('shapes', 'frame', 192, 192, `<rect x="8" y="8" width="176" height="176" rx="48" fill="${C.white}" stroke="${C.ink}" stroke-width="12"/>`, { border: [64, 64, 64, 64] });
add('shapes', 'round_rect', 96, 96, `<rect x="0" y="0" width="96" height="96" rx="28" fill="${C.white}"/>`, { border: [36, 36, 36, 36] });

// ---------- UI ----------
const pill = (fill) => `<rect x="7" y="7" width="226" height="98" rx="49" fill="${fill}" stroke="${C.ink}" stroke-width="9"/>` +
  `<rect x="36" y="20" width="80" height="16" rx="8" fill="${C.white}" opacity="0.5"/>` + `<rect x="16" y="78" width="208" height="12" rx="6" fill="${C.ink}" opacity="0.12"/>`;
for (const [n, col] of Object.entries({ green: C.green, blue: C.blue, yellow: C.yellow, gray: C.gray, red: C.red, white: C.paper }))
  add('ui', `btn_${n}`, 240, 112, pill(col), { border: [60, 50, 60, 50] });
const roundBtn = (fill) => circle(72, 72, 64, fill, C.ink, 9) + ellipse(50, 42, 20, 10, C.white, 'none', 0, `opacity="0.5" ${rot(-30, 50, 42)}`);
for (const [n, col] of Object.entries({ green: C.green, blue: C.blue, yellow: C.yellow, gray: C.gray, white: C.paper }))
  add('ui', `round_${n}`, 144, 144, roundBtn(col));
add('ui', 'panel', 240, 240, `<rect x="7" y="7" width="226" height="226" rx="56" fill="${C.paper}" stroke="${C.ink}" stroke-width="12"/>` +
  `<rect x="26" y="26" width="188" height="188" rx="40" fill="none" stroke="${C.ink}" stroke-width="3" opacity="0.12"/>`, { border: [80, 80, 80, 80] });
add('ui', 'ribbon', 360, 120, shape('M10,30 L50,20 L50,100 L10,110 L28,70 Z', '#E08A1E', C.ink, 7) + shape('M350,30 L310,20 L310,100 L350,110 L332,70 Z', '#E08A1E', C.ink, 7) +
  `<rect x="40" y="10" width="280" height="90" rx="16" fill="${C.yellow}" stroke="${C.ink}" stroke-width="8"/>`, { border: [100, 40, 100, 40] });
add('ui', 'bar_bg', 96, 48, `<rect x="4" y="4" width="88" height="40" rx="20" fill="${C.ink}" opacity="0.25"/>`, { border: [24, 20, 24, 20] });
add('ui', 'bar_fill', 96, 48, `<rect x="4" y="4" width="88" height="40" rx="20" fill="${C.white}"/>`, { border: [24, 20, 24, 20] });
const tile = (fill, dash = false) => `<rect x="7" y="7" width="146" height="146" rx="34" fill="${fill}" stroke="${C.ink}" stroke-width="8" ${dash ? 'stroke-dasharray="14 10"' : ''}/>` +
  `<rect x="22" y="18" width="52" height="12" rx="6" fill="${C.white}" opacity="0.45"/>`;
add('ui', 'tile_open', 160, 160, tile(C.paper));
add('ui', 'tile_current', 160, 160, tile(C.yellow));
add('ui', 'tile_boss', 160, 160, tile('#FFB4A8'));
add('ui', 'tile_locked', 160, 160, tile('#D8DBE6'));
add('ui', 'toggle_on', 160, 88, `<rect x="6" y="6" width="148" height="76" rx="38" fill="${C.green}" stroke="${C.ink}" stroke-width="8"/>` + circle(114, 44, 28, C.white, C.ink, 6));
add('ui', 'toggle_off', 160, 88, `<rect x="6" y="6" width="148" height="76" rx="38" fill="${C.gray}" stroke="${C.ink}" stroke-width="8"/>` + circle(46, 44, 28, C.white, C.ink, 6));
add('ui', 'hand', 160, 160, shape('M62,150 L62,70 Q62,54 76,54 Q90,54 90,70 L90,98 L90,40 Q90,22 106,22 Q122,22 122,40 L122,100 Q140,96 146,110 L146,128 Q146,150 124,150 Z', C.paper, C.ink, 8) +
  line('M106,40 L106,96', C.ink, 5, 'opacity="0.35"'));

// Icons: navy glyphs on transparent, 96px, drawn on top of round buttons or next to text.
const I = (name, body) => add('ui', `icon_${name}`, 96, 96, body);
const ic = (d, sw = 10) => line(d, C.ink, sw);
I('play', shape('M34,22 L76,48 L34,74 Z', C.ink, C.ink, 8));
I('pause', `<rect x="28" y="22" width="14" height="52" rx="5" fill="${C.ink}"/><rect x="54" y="22" width="14" height="52" rx="5" fill="${C.ink}"/>`);
I('home', shape('M20,48 L48,22 L76,48 L76,76 L20,76 Z', 'none', C.ink, 9) + `<rect x="40" y="54" width="16" height="22" fill="${C.ink}"/>`);
I('settings', circle(48, 48, 18, 'none', C.ink, 10) + [0, 45, 90, 135, 180, 225, 270, 315].map((a) =>
  `<rect x="43" y="14" width="10" height="16" rx="3" fill="${C.ink}" ${rot(a, 48, 48)}/>`).join(''));
I('sound_on', shape('M18,38 L32,38 L50,22 L50,74 L32,58 L18,58 Z', C.ink, C.ink, 6) + ic('M62,34 Q72,48 62,62', 8) + ic('M70,24 Q88,48 70,72', 8));
I('sound_off', shape('M18,38 L32,38 L50,22 L50,74 L32,58 L18,58 Z', C.ink, C.ink, 6) + ic('M62,36 L82,60', 8) + ic('M82,36 L62,60', 8));
I('music', ic('M36,70 L36,26 L72,18 L72,62', 8) + circle(28, 70, 11, C.ink) + circle(64, 62, 11, C.ink));
I('vibrate', `<rect x="32" y="16" width="32" height="64" rx="8" fill="none" stroke="${C.ink}" stroke-width="8"/>` + ic('M18,36 L18,60', 7) + ic('M78,36 L78,60', 7));
I('trophy', shape('M30,18 L66,18 L64,46 Q48,62 32,46 Z', C.ink, C.ink, 6) + ic('M30,24 Q14,26 22,40 Q26,46 32,46', 6) + ic('M66,24 Q82,26 74,40 Q70,46 64,46', 6) +
  `<rect x="43" y="56" width="10" height="12" fill="${C.ink}"/><rect x="30" y="68" width="36" height="10" rx="4" fill="${C.ink}"/>`);
I('lock', `<rect x="24" y="42" width="48" height="38" rx="8" fill="${C.ink}"/>` + ic('M34,42 L34,32 Q34,18 48,18 Q62,18 62,32 L62,42', 9));
I('close', ic('M28,28 L68,68', 11) + ic('M68,28 L28,68', 11));
I('hint', circle(48, 40, 22, 'none', C.ink, 9) + `<rect x="38" y="64" width="20" height="8" rx="3" fill="${C.ink}"/><rect x="40" y="76" width="16" height="6" rx="3" fill="${C.ink}"/>` + ic('M48,30 L48,44', 7));
I('restart', ic('M70,40 A24,24 0 1 0 72,56', 10) + shape('M62,26 L78,40 L60,48 Z', C.ink, C.ink, 5));
I('back', ic('M58,22 L32,48 L58,74', 12));
I('next', ic('M38,22 L64,48 L38,74', 12));
I('levels', [0, 1, 2].flatMap((r) => [0, 1, 2].map((c) => `<rect x="${20 + c * 20}" y="${20 + r * 20}" width="15" height="15" rx="4" fill="${C.ink}"/>`)).join(''));
I('star', `<polygon points="${starPts(48, 50, 38, 17)}" fill="${C.yellow}" stroke="${C.ink}" stroke-width="7" stroke-linejoin="round"/>`);
I('star_empty', `<polygon points="${starPts(48, 50, 38, 17)}" fill="#D8DBE6" stroke="${C.ink}" stroke-width="7" stroke-linejoin="round" opacity="0.9"/>`);
I('heart', shape(heartD(48, 50, 30), '#E76F51', 'none', 0));
I('heart_empty', shape(heartD(48, 50, 30), 'none', '#D5CEC2', 7));
I('ad', `<rect x="14" y="24" width="68" height="50" rx="12" fill="${C.ink}"/>` + shape('M40,36 L60,49 L40,62 Z', C.white, C.white, 4));
I('noads', `<rect x="14" y="24" width="68" height="50" rx="12" fill="${C.ink}"/>` + shape('M40,36 L60,49 L40,62 Z', C.white, C.white, 4) + ic('M16,84 L80,14', 10) + line('M16,84 L80,14', C.red, 6));
I('check', ic('M24,50 L42,68 L74,30', 12));
I('infinity', ic('M48,48 C36,30 16,34 16,48 C16,62 36,66 48,48 C60,30 80,34 80,48 C80,62 60,66 48,48', 9));
I('calendar', `<rect x="18" y="24" width="60" height="52" rx="8" fill="none" stroke="${C.ink}" stroke-width="8"/><rect x="18" y="24" width="60" height="16" fill="${C.ink}"/>` +
  ic('M34,16 L34,30', 7) + ic('M62,16 L62,30', 7));
I('gift', `<rect x="18" y="40" width="60" height="38" rx="6" fill="${C.ink}"/><rect x="14" y="30" width="68" height="14" rx="5" fill="${C.ink}"/>` +
  `<rect x="44" y="30" width="8" height="48" fill="${C.yellow}"/>` + ic('M48,30 Q34,10 30,26 Q30,32 48,30', 6) + ic('M48,30 Q62,10 66,26 Q66,32 48,30', 6));

// ---------- FX (white, tinted by particle color) ----------
add('fx', 'fx_spark', 64, 64, shape('M32,2 Q36,28 62,32 Q36,36 32,62 Q28,36 2,32 Q28,28 32,2 Z', C.white, 'none', 0));
add('fx', 'fx_star', 64, 64, `<polygon points="${starPts(32, 34, 30, 13)}" fill="${C.white}"/>`);
add('fx', 'fx_circle', 64, 64, circle(32, 32, 28, C.white));
add('fx', 'fx_ring', 128, 128, circle(64, 64, 54, 'none', C.white, 12));
add('fx', 'fx_confetti', 32, 48, `<rect x="4" y="4" width="24" height="40" rx="5" fill="${C.white}"/>`);
add('fx', 'fx_puff', 96, 96, circle(34, 56, 26, C.white) + circle(58, 40, 28, C.white) + circle(66, 62, 22, C.white));
add('fx', 'fx_drop', 48, 48, shape('M24,4 Q40,26 36,34 A12,12 0 0 1 12,34 Q8,26 24,4 Z', C.white, 'none', 0));

// ---------- render + pack ----------
async function renderSprite(sheet, s) {
  const ov = path.join(OVERRIDE, sheet, `${s.name}.png`);
  if (fs.existsSync(ov)) {
    return sharp(ov).resize(s.w, s.h, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer();
  }
  return sharp(Buffer.from(svg(s.w, s.h, s.body, s.viewBox))).png().toBuffer();
}

// Shelf packer: tallest first, rows left→right, 2px padding. Power-of-two-ish width.
function pack(list, width, PAD = 2) {
  const order = [...list].sort((a, b) => b.h - a.h || b.w - a.w);
  let x = PAD, y = PAD, rowH = 0;
  for (const s of order) {
    if (x + s.w + PAD > width) {
      x = PAD;
      y += rowH + PAD;
      rowH = 0;
    }
    s.x = x;
    s.y = y;
    x += s.w + PAD;
    rowH = Math.max(rowH, s.h);
  }
  // Power of two so the sheet compresses on every mobile format (PVRTC needs POT; ETC2/ASTC are happy too).
  return 2 ** Math.ceil(Math.log2(y + rowH + PAD));
}

async function buildSheet(sheet, width, meta = {}, pad = 2) {
  const list = sprites[sheet];
  const height = pack(list, width, pad);
  const layers = [];
  for (const s of list) layers.push({ input: await renderSprite(sheet, s), left: s.x, top: s.y });
  fs.mkdirSync(OUT, { recursive: true });
  const file = path.join(OUT, `${sheet}.png`);
  await sharp({ create: { width, height, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } }).composite(layers).png().toFile(file);
  const json = {
    sheet,
    width,
    height,
    pixelsPerUnit: 100,
    sprites: list.map((s) => ({ name: s.name, x: s.x, y: s.y, w: s.w, h: s.h, border: s.border ?? [0, 0, 0, 0] })),
    ...meta,
  };
  fs.writeFileSync(path.join(OUT, `${sheet}.json`), JSON.stringify(json, null, 1));
  fs.mkdirSync(PREVIEW, { recursive: true });
  await sharp(file).flatten({ background: '#9aa0b8' }).resize({ width: Math.min(width, 1024) }).toFile(path.join(PREVIEW, `${sheet}.png`));
  console.log(`${sheet}.png  ${width}x${height}  ${list.length} sprites`);
}

await buildSheet('shapes', 1024);
await buildSheet('ui', 1024);
await buildSheet('fx', 256);
