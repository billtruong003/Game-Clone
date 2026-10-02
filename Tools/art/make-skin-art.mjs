// Sprites for the interactive premium skins (Docs/SHADER_LAB.md): `node make-skin-art.mjs` (in Tools/art).
//
// Everything is drawn here as SVG (house style: flat fills, #1E2240 ink outlines) and exported to
// Assets/_Game/Art/Skins. The skin shaders sample these through texture slots on their material templates
// (Assets/_Game/Skins), so any PNG can be swapped for a hand-made one with the same layout.
//
// Tinted parts follow the sheet rule: bodies that take the piece colour are WHITE / light grey ("base" rows),
// fixed-colour details and outlines sit in a separate untinted row ("details").
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(HERE, '../../Assets/_Game/Art/Skins');
fs.mkdirSync(OUT, { recursive: true });

const INK = '#1E2240';
const svg = (w, h, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
const g = (x, y, body, extra = '') => `<g transform="translate(${x} ${y})" ${extra}>${body}</g>`;
const circle = (cx, cy, r, fill, sw = 0, stroke = INK) => `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}"/>`;
const ellipse = (cx, cy, rx, ry, fill, sw = 0, stroke = INK, extra = '') =>
  `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const rect = (x, y, w, h, r, fill, sw = 0, stroke = INK) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}"/>`;
const path_ = (d, fill, sw = 0, stroke = INK, extra = '') => `<path d="${d}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" stroke-linejoin="round" stroke-linecap="round" ${extra}/>`;
const line = (d, sw, stroke = INK) => path_(d, 'none', sw, stroke);

async function save(name, w, h, body) {
  const file = path.join(OUT, name + '.png');
  await sharp(Buffer.from(svg(w, h, body))).png().toFile(file);
  console.log('wrote', path.relative(path.resolve(HERE, '../..'), file));
}

// seeded random so the art is stable between runs
let seed = 7;
const rnd = () => ((seed = (seed * 16807) % 2147483647) / 2147483647);

// ------------------------------------------------------------------ ants: 4 walk frames, facing right ----------
// 128 x 80 per frame. Tripod gait: legs 0/2 of one side swing with leg 1 of the other.
function ant(frame) {
  const body = '#3A2A30', hi = '#7A5A62';
  const swing = [1, 0.35, -1, -0.35][frame];
  let legs = '';
  [[56, -1], [66, 0], [76, 1]].forEach(([x, k], i) => {
    for (const side of [-1, 1]) {
      const phase = (i % 2 === 0 ? 1 : -1) * side * swing;
      const kneeX = x + k * 10 + phase * 10, kneeY = 40 + side * 20;
      const footX = x + k * 22 + phase * 22, footY = 40 + side * 34;
      legs += line(`M${x},40 L${kneeX},${kneeY} L${footX},${footY}`, 4.5, body);
    }
  });
  const wave = [0, 2, 0, -2][frame];
  const antennae = line(`M98,34 Q108,${20 + wave} 122,${14 + wave}`, 3.5, body) + line(`M98,46 Q108,${60 - wave} 122,${66 - wave}`, 3.5, body);
  const parts =
    ellipse(34, 40, 25, 18, body, 3) + ellipse(28, 33, 11, 5, hi, 0) +          // abdomen + highlight
    ellipse(54, 40, 6, 5, body, 3) +                                           // petiole
    ellipse(68, 40, 15, 10, body, 3) + ellipse(66, 36, 7, 3, hi, 0) +           // thorax
    circle(93, 40, 13, body, 3) + circle(97, 34, 3.2, '#FFFFFF') + circle(97, 46, 3.2, '#FFFFFF') +
    line('M104,36 L110,38 M104,44 L110,42', 3, body);                          // mandibles
  return legs + antennae + parts;
}
await save('ant_walk', 512, 80, [0, 1, 2, 3].map(f => g(f * 128, 0, ant(f))).join(''));

// ------------------------------------------------------------------ train: engine + car, facing right ----------
// 256 x 128 cells; columns: engine, car; row 0 = base (white, tinted), row 1 = details (untinted).
function engineBase() {
  return path_('M10,16 H196 Q246,16 246,64 Q246,112 196,112 H10 Z', '#FFFFFF') +
    path_('M14,24 H194 Q236,24 236,64 Q236,104 194,104 H14 Z', '#EEF0F6') +   // roof panel
    rect(14, 52, 222, 24, 12, '#FFFFFF');                                         // roof ridge light
}
function engineDetails() {
  return path_('M10,16 H196 Q246,16 246,64 Q246,112 196,112 H10 Z', 'none', 7) +
    rect(18, 30, 46, 68, 10, '#2B3550', 4) + rect(24, 36, 14, 56, 6, '#56658F') +   // cab roof window (the lamp sits here)
    ellipse(240, 64, 7, 14, '#FFE27A', 3) +                                        // headlight
    rect(0, 54, 12, 20, 4, '#3A3F55', 3);                                           // coupler
}
function carBase() {
  return rect(12, 16, 232, 96, 18, '#FFFFFF') + rect(22, 26, 212, 76, 12, '#EEF0F6') + rect(22, 52, 212, 24, 12, '#FFFFFF');
}
function carDetails() {
  let win = '';
  for (let i = 0; i < 5; i++) { win += rect(32 + i * 42, 18, 30, 9, 4, '#2B3550', 2) + rect(32 + i * 42, 101, 30, 9, 4, '#2B3550', 2); }
  return rect(12, 16, 232, 96, 18, 'none', 7) + win + line('M128,28 V100', 4, '#C9CCD8') +
    rect(0, 54, 14, 20, 4, '#3A3F55', 3) + rect(242, 54, 14, 20, 4, '#3A3F55', 3);
}
await save('train', 512, 256, g(0, 0, engineBase()) + g(256, 0, carBase()) + g(0, 128, engineDetails()) + g(256, 128, carDetails()));

// ------------------------------------------------------------------ snow globe scenes ----------
// 256 x 256 each, transparent: snowman, cabin, pines. The snow mound fills the bottom.
const snow = '#FFFFFF', snowShade = '#DCE6F5';
const mound = path_('M-10,256 V200 Q60,170 128,184 Q200,196 266,176 V256 Z', snow, 5) + path_('M20,214 Q70,200 110,208', 'none', 4, snowShade);
function pine(x, y, s) {
  const t = (w, h, yy) => path_(`M${x},${yy - h} L${x + w},${yy} L${x - w},${yy} Z`, '#2E7D5B', 5);
  return rect(x - 7 * s, y - 4, 14 * s, 22 * s, 3, '#7A4E2D', 4) +
    t(46 * s, 60 * s, y) + t(38 * s, 54 * s, y - 34 * s) + t(28 * s, 46 * s, y - 64 * s) +
    path_(`M${x - 20 * s},${y - 70 * s} Q${x},${y - 78 * s} ${x + 18 * s},${y - 66 * s}`, 'none', 5, snow) +
    path_(`M${x - 30 * s},${y - 36 * s} Q${x},${y - 46 * s} ${x + 30 * s},${y - 34 * s}`, 'none', 5, snow);
}
const scenePines = pine(150, 196, 1.15) + pine(72, 200, 0.75) + pine(212, 196, 0.6) + mound;
const sceneCabin =
  rect(58, 120, 140, 82, 6, '#B5533C', 5) + rect(70, 132, 116, 6, 2, '#9A4532') + rect(70, 156, 116, 6, 2, '#9A4532') +
  path_('M40,126 L128,62 L216,126 Z', '#5A3A2E', 5) + path_('M44,118 L128,58 L212,118 L204,126 L128,72 L52,126 Z', snow, 4) +
  rect(160, 54, 22, 40, 3, '#7A4E2D', 4) + rect(156, 48, 30, 10, 3, snow, 4) +
  rect(76, 146, 36, 32, 4, '#FFD66B', 5) + line('M94,146 V178 M76,162 H112', 4) +
  rect(142, 146, 34, 56, 4, '#6B3A2A', 5) + circle(168, 176, 3, '#FFD66B') + mound;
const sceneSnowman =
  circle(128, 162, 44, snow, 5) + circle(128, 96, 32, snow, 5) +
  circle(116, 90, 4.5, INK) + circle(140, 90, 4.5, INK) + path_('M128,99 L156,104 L128,107 Z', '#FF8A3D', 3) +
  path_('M98,120 Q128,134 158,120 L160,132 Q128,144 96,132 Z', '#E5484D', 4) + path_('M148,128 L162,160 L148,162 L140,132 Z', '#E5484D', 4) +
  circle(128, 150, 4, INK) + circle(128, 168, 4, INK) +
  rect(104, 50, 48, 34, 4, '#2B2D42', 5) + rect(94, 80, 68, 10, 4, '#2B2D42', 5) +
  line('M86,156 L52,130 M168,156 L204,128', 5, '#7A4E2D') + mound;
await save('snowglobe_scenes', 768, 256, g(0, 0, sceneSnowman) + g(256, 0, sceneCabin) + g(512, 0, scenePines));

// ------------------------------------------------------------------ aquarium: fish (3 x 2 frames) + tank bed ----------
// fish cells 128 x 96, facing right; frame 1 = tail flicked
function fish(kind, frame) {
  const tail = frame ? 'M34,48 L6,26 L14,48 L6,70 Z' : 'M34,48 L8,32 L12,48 L8,64 Z';
  if (kind === 0) { // clownfish
    return path_(tail, '#FF8A2A', 4) + ellipse(72, 48, 44, 28, '#FF8A2A', 4) +
      path_('M60,21 Q54,48 60,75', 'none', 9, '#FFFFFF') + path_('M88,23 Q82,48 88,73', 'none', 8, '#FFFFFF') +
      path_('M60,21 Q54,48 60,75 M88,23 Q82,48 88,73', 'none', 2, INK) +
      circle(100, 42, 6, '#FFFFFF', 3) + circle(102, 42, 3, INK) + path_('M66,20 Q76,8 86,22', '#FF8A2A', 4);
  }
  if (kind === 1) { // blue tang
    return path_(tail, '#FFD23F', 4) + ellipse(72, 48, 42, 30, '#3E7BFA', 4) +
      path_('M42,40 Q72,22 96,36 Q74,40 60,56 Q50,52 42,40 Z', '#1E2A6B') +
      circle(100, 42, 6, '#FFFFFF', 3) + circle(102, 42, 3, INK) + path_('M56,20 Q74,6 92,22', '#3E7BFA', 4);
  }
  return path_(tail, '#FFE066', 4) + circle(72, 48, 32, '#FFE066', 4) + // puffer
    [[60, 30], [80, 26], [54, 56], [86, 60], [70, 70]].map(([x, y]) => circle(x, y, 3.5, '#D9A520')).join('') +
    circle(92, 40, 7, '#FFFFFF', 3) + circle(94, 40, 3.5, INK) + ellipse(100, 56, 5, 3.5, '#E5484D', 2);
}
let fishSheet = '';
for (let k = 0; k < 3; k++) for (let f = 0; f < 2; f++) fishSheet += g(f * 128, k * 96, fish(k, f));
await save('aquarium_fish', 256, 288, fishSheet);

seed = 11;
let weeds = '';
[[30, 1.0, '#3BA676'], [70, 0.7, '#5CC88E'], [196, 1.1, '#2F8F64'], [226, 0.75, '#5CC88E']].forEach(([x, s, c]) => {
  weeds += path_(`M${x},120 Q${x - 18 * s},${100 - 30 * s} ${x},${120 - 70 * s} Q${x + 14 * s},${120 - 90 * s} ${x + 4 * s},${120 - 110 * s} ` +
    `Q${x + 22 * s},${120 - 80 * s} ${x + 10 * s},${120 - 60 * s} Q${x + 24 * s},${100 - 26 * s} ${x + 12 * s},120 Z`, c, 4);
});
let pebbles = '';
for (let i = 0; i < 9; i++) pebbles += ellipse(16 + i * 28 + rnd() * 8, 116 + rnd() * 6, 12 + rnd() * 6, 8 + rnd() * 3, ['#E9DCC0', '#C9B48E', '#F4EBDA', '#9FB7C9'][i % 4], 3);
const bed = path_('M0,128 V108 Q64,98 128,104 Q192,110 256,100 V128 Z', '#F2DFB3', 4) + weeds + pebbles +
  path_('M150,112 Q160,96 172,112 Z', '#FF9FB0', 3) + line('M155,111 L161,101 L167,111', 2);
await save('aquarium_bed', 256, 128, bed);

// ------------------------------------------------------------------ night city: far skyline + mid buildings + window ids ----------
// Tile horizontally (1024 wide). Window ids: each window a different grey (its light-up threshold), alpha = window.
seed = 23;
let far = '';
for (let x = 0; x < 1024;) {
  const w = 40 + rnd() * 60, h = 70 + rnd() * 150;
  far += rect(x, 256 - h, w + 1, h, 0, '#2A3566');
  if (rnd() > 0.7) far += rect(x + w / 2 - 2, 256 - h - 24, 4, 24, 0, '#2A3566');
  x += w;
}
await save('city_far', 1024, 256, far);

seed = 41;
let mid = '', wins = '';
const walls = ['#3A3F6B', '#2F3560', '#463E70', '#35406A', '#4A3A62'];
for (let x = 6; x < 1018;) {
  const w = Math.min(1018 - x, 90 + Math.floor(rnd() * 90)), h = 200 + Math.floor(rnd() * 280), top = 512 - h;
  const col = walls[Math.floor(rnd() * walls.length)];
  mid += rect(x, top, w, h + 10, 4, col, 5);
  if (rnd() > 0.5) mid += rect(x + 12, top - 22, 26, 22, 3, col, 4) + line(`M${x + 25},${top - 22} V${top - 34}`, 4);   // water tank
  if (rnd() > 0.6) mid += line(`M${x + w - 16},${top} V${top - 40} M${x + w - 24},${top - 30} H${x + w - 8}`, 4);        // antenna
  const cols = Math.max(2, Math.floor((w - 16) / 26)), gap = (w - cols * 16) / (cols + 1);
  for (let yy = top + 18; yy < 500; yy += 30) {
    for (let c = 0; c < cols; c++) {
      const wx = x + gap + c * (16 + gap);
      mid += rect(wx, yy, 16, 20, 2, '#1A1F3A');
      const v = 8 + Math.floor(rnd() * 240);
      wins += rect(wx, yy, 16, 20, 2, `rgb(${v},${v},${v})`);
    }
  }
  x += w + 4 + Math.floor(rnd() * 10);
}
await save('city_mid', 1024, 512, mid);
await save('city_windows', 1024, 512, wins);

// ------------------------------------------------------------------ tape roll (the Tape arrow's cap) ----------
// 256 x 256 each: base (white tape ring, tinted) and details (cardboard core, outline, shine).
const rollBase = circle(128, 128, 118, '#FFFFFF') + circle(128, 128, 104, '#EEF0F6') + circle(128, 128, 92, '#FFFFFF');
const rollDetails = circle(128, 128, 118, 'none', 8) +
  [100, 86].map(r => circle(128, 128, r, 'none', 2, '#B9BFD0')).join('') +
  circle(128, 128, 66, '#E3C493', 7) + circle(128, 128, 54, '#F1DDB5') +
  path_('M60,64 A92,92 0 0 1 128,36', 'none', 10, 'rgba(255,255,255,0.85)');
await save('tape_roll', 512, 256, g(0, 0, rollBase) + g(256, 0, rollDetails));
console.log('done');
