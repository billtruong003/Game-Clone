// App icons + Play feature graphics for the three games: `node make-icons.mjs` (in Tools/art).
//
// Same drawing language as make-art.mjs (flat colors, ink outline, emoji faces). Icons are full-bleed squares with
// no text (Play masks them to its own shape); feature graphics are 1024x500 with the game name.
//   Assets/_Game/Art/Icons/<Game>.png        1024x1024, used by the Build Switcher profile
//   Store/graphics/<game>-icon-512.png        Play Console hi-res icon
//   Store/graphics/<game>-feature.png         Play Console feature graphic
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ICONS = path.resolve(HERE, '../../Assets/_Game/Art/Icons');
const STORE = path.resolve(HERE, '../../Store/graphics');

const C = {
  ink: '#1E2240', paper: '#FFF8EC', white: '#FFFFFF', navy: '#2B2F55', navyDeep: '#232748', cream: '#F5F1EA', grid: '#D9D2C5',
  red: '#FF5A5F', orange: '#FF9F1C', yellow: '#FFD23F', green: '#3DDC97', blue: '#4EA8DE', purple: '#9B5DE5', pink: '#F15BB5',
  teal: '#2A9D8F', plum: '#8E5BB5', amber: '#E9A23B', slate: '#2E3A59', mouth: '#7A2436', tongue: '#FF7A93', blush: '#FF8FA3',
};

const svg = (w, h, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
const circle = (cx, cy, r, fill, stroke = 'none', sw = 0, extra = '') =>
  `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const ellipse = (cx, cy, rx, ry, fill, stroke = 'none', sw = 0, extra = '') =>
  `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;
const line = (d, col = C.ink, sw = 9, extra = '') =>
  `<path d="${d}" fill="none" stroke="${col}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
const shape = (d, fill, stroke = C.ink, sw = 6, extra = '') =>
  `<path d="${d}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" stroke-linejoin="round" ${extra}/>`;
const rrect = (x, y, w, h, r, fill, stroke = C.ink, sw = 0, extra = '') =>
  `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" stroke="${stroke}" stroke-width="${sw}" ${extra}/>`;

// ---------- emoji faces, authored in a 256 box (same geometry as make-art.mjs) ----------
const EX = 42, EY = 108, MY = 158, L = 128 - EX, R = 128 + EX;
const eye = (x, y, look = 0, lookY = 0) =>
  ellipse(x, y, 23, 27, C.white, C.ink, 6) + circle(x + look * 9, y + 5 + lookY * 7, 12, C.ink) + circle(x + look * 9 - 4, y - 1 + lookY * 7, 4, C.white);
const eyeClosed = (x, y) => line(`M${x - 20},${y + 6} Q${x},${y - 16} ${x + 20},${y + 6}`);
const smile = (w, depth, y = MY) => line(`M${128 - w},${y} Q128,${y + depth} ${128 + w},${y}`);
const openMouth = (w, h, y = MY - 6) =>
  shape(`M${128 - w},${y} Q128,${y + h * 2} ${128 + w},${y} Z`, C.mouth, C.ink, 7) + ellipse(128, y + h * 0.75, w * 0.45, h * 0.3, C.tongue);
const blush = () => ellipse(58, 142, 15, 9, C.blush, 'none', 0, 'opacity="0.75"') + ellipse(198, 142, 15, 9, C.blush, 'none', 0, 'opacity="0.75"');
const FACES = {
  happy: () => eye(L, EY, 0.3) + eye(R, EY, 0.3) + smile(34, 30) + blush(),
  laugh: () => eyeClosed(L, EY) + eyeClosed(R, EY) + openMouth(42, 34) + blush(),
  wow: () => eye(L, EY - 4, 0, -0.4) + eye(R, EY - 4, 0, -0.4) + ellipse(128, MY + 8, 13, 17, C.mouth, C.ink, 7),
  grin: () => eye(L, EY, -0.4) + eye(R, EY, -0.4) + openMouth(38, 26) + blush(),
};
// face centered at (cx, cy) filling a box of `size`
const face = (kind, cx, cy, size) => `<g transform="translate(${cx - size / 2} ${cy - size / 2}) scale(${size / 256})">${FACES[kind]()}</g>`;

// A glossy ball / block like the in-game sprites: fill + ink outline + soft highlight.
const ball = (cx, cy, r, fill, faceKind) =>
  circle(cx, cy, r, fill, C.ink, r * 0.07) +
  ellipse(cx - r * 0.38, cy - r * 0.5, r * 0.28, r * 0.13, C.white, 'none', 0, `opacity="0.55" transform="rotate(-28 ${cx - r * 0.38} ${cy - r * 0.5})"`) +
  face(faceKind, cx, cy + r * 0.04, r * 1.55);
const block = (x, y, s, fill, faceKind) =>
  rrect(x, y, s, s, s * 0.22, fill, C.ink, s * 0.045) + rrect(x + s * 0.14, y + s * 0.09, s * 0.34, s * 0.08, s * 0.04, C.white, 'none', 0, 'opacity="0.55"') +
  (faceKind ? face(faceKind, x + s / 2, y + s * 0.52, s * 0.95) : '');

// ---------- icon art (1024) ----------
function arrowOutArt(S = 1024) {
  const u = S / 1024;
  let b = rrect(0, 0, S, S, 0, C.cream);
  for (let r = 0; r < 5; r++) for (let c = 0; c < 5; c++) b += circle((212 + c * 150) * u, (212 + r * 150) * u, 12 * u, C.grid);
  const W = 104 * u, O = 26 * u;
  // an arrow as in the game: tail dot, body, corners, chevron head; drawn as ink outline + color on top
  const arrow = (pts, dir, col) => {
    const d = 'M' + pts.map(([x, y]) => `${x * u},${y * u}`).join(' L');
    const [hx, hy] = pts[pts.length - 1];
    const k = 70;
    const chevron = dir === 'right'
      ? `M${(hx - k) * u},${(hy - k) * u} L${hx * u},${hy * u} L${(hx - k) * u},${(hy + k) * u}`
      : `M${(hx - k) * u},${(hy + k) * u} L${hx * u},${hy * u} L${(hx + k) * u},${(hy + k) * u}`;
    const [tx, ty] = pts[0];
    return line(d, C.ink, W + O * 2) + line(chevron, C.ink, W + O * 2) + circle(tx * u, ty * u, W * 0.62 + O, C.ink) +
      line(d, col, W) + line(chevron, col, W) + circle(tx * u, ty * u, W * 0.62, col);
  };
  // slate arrow still on the board, teal arrow flying out to the right with its ink trail and speed lines
  b += arrow([[212, 812], [212, 512], [420, 512]], 'right', C.slate);
  b += line(`M${362 * u},${812 * u} L${512 * u},${812 * u}`, C.teal, W * 0.5, 'opacity="0.22"');
  b += arrow([[512, 812], [512, 362], [800, 362]], 'right', C.teal);
  b += line(`M${860 * u},${300 * u} L${910 * u},${300 * u}`, C.amber, 30 * u) + line(`M${870 * u},${362 * u} L${930 * u},${362 * u}`, C.teal, 30 * u) +
    line(`M${860 * u},${424 * u} L${910 * u},${424 * u}`, C.plum, 30 * u);
  return b;
}

function eyeBlastArt(S = 1024) {
  const u = S / 1024;
  let b = rrect(0, 0, S, S, 0, C.navy);
  // board cells behind
  for (let r = 0; r < 4; r++) for (let c = 0; c < 4; c++) b += rrect((140 + c * 190) * u, (140 + r * 190) * u, 170 * u, 170 * u, 36 * u, C.navyDeep);
  const s = 184 * u, o = (x) => (140 + x * 190) * u - 7 * u;
  b += block(o(0), o(2), s, C.blue) + block(o(0), o(3), s, C.blue) + block(o(1), o(3), s, C.blue, 'grin');
  b += block(o(3), o(0), s, C.pink) + block(o(3), o(1), s, C.pink, 'wow');
  b += block(o(1), o(1), s * 1.2, C.yellow, 'laugh');
  b += block(o(2), o(2), s, C.green, 'happy');
  return b;
}

function eyeMergeArt(S = 1024) {
  const u = S / 1024;
  let b = rrect(0, 0, S, S, 0, C.navy);
  // glass jar: flat toon glass, dark outline, one hard highlight band
  const jx = 150 * u, jy = 230 * u, jw = 724 * u, jh = 700 * u, jr = 150 * u;
  b += rrect(jx, jy, jw, jh, jr, '#DFF3FF', 'none', 0, 'opacity="0.16"');
  b += ball(660 * u, 780 * u, 150 * u, C.green, 'happy');
  b += ball(330 * u, 760 * u, 175 * u, C.orange, 'grin');
  b += ball(520 * u, 540 * u, 205 * u, C.yellow, 'laugh');
  b += rrect(jx, jy, jw, jh, jr, 'none', C.ink, 34 * u);
  b += rrect(jx + 50 * u, jy + 90 * u, 36 * u, 250 * u, 18 * u, C.white, 'none', 0, 'opacity="0.35"');
  b += rrect(jx - 30 * u, jy - 64 * u, jw + 60 * u, 80 * u, 40 * u, C.paper, C.ink, 26 * u); // rim
  // falling ball above the jar
  b += ball(740 * u, 140 * u, 72 * u, C.pink, 'wow');
  return b;
}

const GAMES = [
  { id: 'ArrowOut', slug: 'arrow-out', name: 'Arrow Out', tag: 'Clear the arrows', art: arrowOutArt, bg: C.cream, text: C.ink },
  { id: 'EyeBlast', slug: 'eye-blast', name: 'Eye Blast', tag: 'Cute block puzzle', art: eyeBlastArt, bg: C.navy, text: C.white },
  { id: 'EyeMerge', slug: 'eye-merge', name: 'Eye Merge', tag: 'Drop, merge, smile', art: eyeMergeArt, bg: C.navy, text: C.white },
];

// Feature graphic 1024x500: the icon art on the right, name + tagline on the left. Text is set in the system
// Segoe UI Black; Play overlays nothing on feature graphics, so the name is readable as is.
function feature(g) {
  const art = `<g transform="translate(560 -20) scale(0.53)">${g.art(1024)}</g>`;
  const fade = `<defs><linearGradient id="f" x1="0" x2="1"><stop offset="0.45" stop-color="${g.bg}"/><stop offset="0.62" stop-color="${g.bg}" stop-opacity="0"/></linearGradient></defs>`;
  const text = `<text x="70" y="250" font-family="Segoe UI Black" font-size="110" fill="${g.text}">${g.name}</text>` +
    `<text x="74" y="330" font-family="Segoe UI" font-weight="700" font-size="46" fill="${g.text}" opacity="0.7">${g.tag}</text>`;
  return svg(1024, 500, fade + rrect(0, 0, 1024, 500, 0, g.bg) + art + rrect(0, 0, 1024, 500, 0, 'url(#f)') + text);
}

fs.mkdirSync(ICONS, { recursive: true });
fs.mkdirSync(STORE, { recursive: true });
for (const g of GAMES) {
  const icon = Buffer.from(svg(1024, 1024, g.art(1024)));
  await sharp(icon).png().toFile(path.join(ICONS, `${g.id}.png`));
  await sharp(icon).resize(512, 512).png().toFile(path.join(STORE, `${g.slug}-icon-512.png`));
  await sharp(Buffer.from(feature(g))).png().toFile(path.join(STORE, `${g.slug}-feature.png`));
  console.log('icon', g.id);
}
