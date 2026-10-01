// Brand art for the three games, drawn with the game's own language (deadpan faces from faces.mjs, flat colour,
// ink outline, Baloo 2): app icon, adaptive-icon layers, title logo, Play feature graphic and splash logo.
//   node make-brand.mjs            → Tools/art/brand/html/*.html + rendered PNGs in Tools/art/brand/out/
//   node make-brand.mjs --install  → also copies them to Assets/_Game/Art/Brand and Store/graphics
// Rendering goes through headless Chrome (real font, real SVG), one page per image at its exact pixel size.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { faceInner, INK } from './faces.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(HERE, 'brand');
const HTML = path.join(OUT, 'html');
const PNG = path.join(OUT, 'out');
const FONT = pathToFileURL(path.resolve(HERE, '../../Assets/_Game/Art/Fonts/Baloo2-Bold.ttf')).href;
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
fs.mkdirSync(HTML, { recursive: true });
fs.mkdirSync(PNG, { recursive: true });

const CREAM = '#FFF8EC';
const f = n => Math.round(n * 10) / 10;
const lum = h => { const [r, g, b] = [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16) / 255); return 0.2126 * r + 0.7152 * g + 0.0722 * b; };
const face = (expr, cx, cy, size, ink = INK) =>
  `<g transform="translate(${f(cx - size / 2)} ${f(cy - size / 2)}) scale(${f(size / 256 * 1000) / 1000})">${faceInner(expr).split(INK).join(ink)}</g>`;

// ---------- characters, same construction as the game sprites ----------
function ball(cx, cy, r, color, expr) {
  const bw = Math.max(3, r * 0.07);
  return `<circle cx="${cx}" cy="${cy}" r="${f(r - bw / 2)}" fill="${color}" stroke="${INK}" stroke-width="${f(bw)}"/>` +
    `<ellipse cx="${f(cx - r * 0.34)}" cy="${f(cy - r * 0.6)}" rx="${f(r * 0.22)}" ry="${f(r * 0.1)}" fill="#FFFFFF" opacity="0.5" transform="rotate(-30 ${f(cx - r * 0.34)} ${f(cy - r * 0.6)})"/>` +
    face(expr, cx, cy - r * 0.06, r * 1.32, lum(color) < 0.36 ? CREAM : INK);
}
function block(x, y, s, color, expr, rot = 0) {
  const bw = s * 0.045;
  return `<g transform="rotate(${rot} ${f(x + s / 2)} ${f(y + s / 2)})">` +
    `<rect x="${f(x + bw / 2)}" y="${f(y + bw / 2)}" width="${f(s - bw)}" height="${f(s - bw)}" rx="${f(s * 0.2)}" fill="${color}" stroke="${INK}" stroke-width="${f(bw)}"/>` +
    `<rect x="${f(x + s * 0.13)}" y="${f(y + s * 0.1)}" width="${f(s * 0.24)}" height="${f(s * 0.07)}" rx="${f(s * 0.035)}" fill="#FFFFFF" opacity="0.5"/>` +
    `<rect x="${f(x + bw)}" y="${f(y + s * 0.86)}" width="${f(s - 2 * bw)}" height="${f(s * 0.08)}" rx="${f(s * 0.04)}" fill="#1E2240" opacity="0.12"/>` +
    face(expr, x + s / 2, y + s * 0.47, s * 0.68) + '</g>';
}
// Game arrow: thick round stroke, chevron head, round tail cap carrying the face (no outline, as in the game).
function arrow(pts, dir, color, expr, w) {
  const [hx, hy] = pts[pts.length - 1];
  const k = w * 1.05;
  const chev = dir === 'L' ? `${hx + k},${hy - k * 1.1} ${hx - 4},${hy} ${hx + k},${hy + k * 1.1}`
    : dir === 'R' ? `${hx - k},${hy - k * 1.1} ${hx + 4},${hy} ${hx - k},${hy + k * 1.1}`
    : dir === 'D' ? `${hx - k * 1.1},${hy - k} ${hx},${hy + 4} ${hx + k * 1.1},${hy - k}`
    : `${hx - k * 1.1},${hy + k} ${hx},${hy - 4} ${hx + k * 1.1},${hy + k}`;
  const [tx, ty] = pts[0];
  const r = w * 1.28;
  return `<g fill="none" stroke="${color}" stroke-width="${w}" stroke-linejoin="round" stroke-linecap="round"><polyline points="${pts.map(p => p.join(',')).join(' ')}"/><polyline points="${chev}"/></g>` +
    `<circle cx="${tx}" cy="${ty}" r="${r}" fill="${color}"/>` + face(expr, tx, ty - r * 0.04, r * 1.55, lum(color) < 0.36 ? CREAM : INK);
}
// Meh Merge open stage: shelf top at y, pillars from top to the shelf at x0 / x1 (outer edges), darker floor below.
function stage(x0, x1, y, top, adaptive) {
  const t = 36, sw = 14;
  const pill = (x, yy, w, h, col) => `<rect x="${x}" y="${yy}" width="${w}" height="${h}" rx="${t / 2}" fill="${col}" stroke="${INK}" stroke-width="${sw}"/>`;
  return (adaptive ? '' : `<rect x="0" y="${y + t / 2}" width="1024" height="${1024 - y}" fill="#271E47"/>`) +
    pill(x0, top, t, y - top + t / 2, '#4A3D80') + pill(x1 - t, top, t, y - top + t / 2, '#4A3D80') +
    pill(x0 - 8, y, x1 - x0 + 16, t, '#5B4D96');
}

// ---------- the three scenes (1024 space) ----------
const GAMES = {
  'bruh-arrows': {
    title: 'BRUH ARROWS', sub: 'Tap. Yeet. Bruh.', bg: '#F4EFE8', text: INK, fill: '#35B09F', first: '#D6334A',
    scene(adaptive = false) {
      let s = '';
      if (!adaptive) for (let x = 64; x < 1024; x += 128) for (let y = 64; y < 1024; y += 128) s += `<circle cx="${x}" cy="${y}" r="7" fill="#DCD4C6"/>`;
      s += arrow([[704, 684], [300, 684]], 'L', '#2E3A59', 'smug', 104);
      s += arrow([[316, 356], [746, 356]], 'R', '#35B09F', 'grin', 104);
      return s;
    },
  },
  'nah-blocks': {
    title: 'NAH BLOCKS', sub: "Line 'em up. They say nah.", bg: '#2B2F55', text: '#FFFFFF', fill: '#FFD23F',
    scene() {
      const s0 = 280, g = 16, x0 = 492 - s0 - g / 2, y0 = 580 - s0 - g / 2;
      return block(x0, y0, s0, '#4EA8DE', 'shock') + block(x0 + s0 + g, y0, s0, '#3DDC97', 'grin') +
        block(x0, y0 + s0 + g, s0, '#FF5A5F', 'dizzy') + block(x0 + s0 + g, y0 + s0 + g, s0, '#FFD23F', 'smug') +
        block(720, 110, 170, '#F15BB5', 'panic', 14);
    },
  },
  'meh-merge': {
    title: 'MEH MERGE', sub: 'Two mehs make a bigger meh.', bg: '#2F2552', text: '#FFFFFF', fill: '#FFD23F',
    // open stage, as in the game: a shelf the pile sits on and two pillars, nothing drawn over the balls
    scene(adaptive = false) {
      const st = stage(178, 846, 846, 430, adaptive);
      return st + ball(392, 672, 174, '#FFD23F', 'smug') + ball(682, 732, 114, '#4EA8DE', 'cry') + ball(642, 534, 84, '#3DDC97', 'dizzy') +
        ball(512, 196, 72, '#F15BB5', 'shock');
    },
  },
};

// ---------- pages ----------
const page = (w, h, body, transparent = false) => `<!doctype html><html><head><meta charset="utf-8"><style>
@font-face { font-family: Baloo; src: url('${FONT}'); }
html, body { margin: 0; width: ${w}px; height: ${h}px; overflow: hidden; background: ${transparent ? 'transparent' : '#000'}; }
.logo { font-family: Baloo, sans-serif; font-weight: 700; line-height: 0.95; letter-spacing: 1px; text-align: center;
  paint-order: stroke fill; -webkit-text-stroke: var(--sw) ${INK}; }
</style></head><body>${body}</body></html>`;
const svgWrap = (w, h, inner, extra = '') => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"${extra}>${inner}</svg>`;

// The wordmark: two lines, chunky, ink stroke + a hard ink shadow (the game's button text style).
function logoHtml(g, width, size) {
  const words = g.title.split(' ');
  const line = (t, col) => `<div class="logo" style="--sw: ${f(size * 0.13)}px; font-size: ${size}px; color: ${col}; text-shadow: 0 ${f(size * 0.08)}px 0 ${INK};">${t}</div>`;
  return `<div style="width: ${width}px; display: flex; flex-direction: column; align-items: center; gap: ${f(size * 0.02)}px">${line(words[0], g.first ?? '#FFFFFF')}${line(words[1], g.fill)}</div>`;
}

const jobs = [];
for (const [id, g] of Object.entries(GAMES)) {
  const full = svgWrap(1024, 1024, `<rect width="1024" height="1024" fill="${g.bg}"/>${g.scene()}`);
  jobs.push([`${id}-icon`, 1024, 1024, page(1024, 1024, full)]);
  // adaptive icon: the launcher masks to ~66 % of the canvas, so the scene sits in the middle 66 %
  jobs.push([`${id}-adaptive-bg`, 1024, 1024, page(1024, 1024, svgWrap(1024, 1024, `<rect width="1024" height="1024" fill="${g.bg}"/>` +
    (id === 'bruh-arrows' ? Array.from({ length: 64 }, (_, i) => `<circle cx="${64 + (i % 8) * 128}" cy="${64 + Math.floor(i / 8) * 128}" r="7" fill="#DCD4C6"/>`).join('') : '')))]);
  jobs.push([`${id}-adaptive-fg`, 1024, 1024, page(1024, 1024, svgWrap(1024, 1024, `<g transform="translate(512 512) scale(0.64) translate(-512 -512)">${g.scene(true)}</g>`), true), true]);
  jobs.push([`${id}-logo`, 1600, 600, page(1600, 600, `<div style="height: 600px; display: flex; align-items: center; justify-content: center">${logoHtml(g, 1600, 230)}</div>`, true), true]);
  // feature graphic: logo left, the icon scene right, flat game colour
  const sceneSvg = svgWrap(500, 500, `<g transform="scale(${500 / 1024})">${g.scene(true)}</g>`);
  jobs.push([`${id}-feature`, 1024, 500, page(1024, 500, `<div style="width: 1024px; height: 500px; background: ${g.bg}; display: flex; align-items: center">
<div style="width: 540px; display: flex; flex-direction: column; align-items: center; gap: 10px">${logoHtml(g, 540, 104)}
<div style="font-family: Baloo, sans-serif; font-weight: 700; font-size: 34px; color: ${g.text === INK ? '#6A6F8E' : '#C9C3E8'}">${g.sub}</div></div>
<div style="width: 460px; height: 460px">${sceneSvg.replace('width="500" height="500"', 'width="460" height="460"')}</div></div>`)]);
}

for (const [name, w, h, html, transparent] of jobs) {
  const file = path.join(HTML, `${name}.html`);
  fs.writeFileSync(file, html);
  const out = path.join(PNG, `${name}.png`);
  // a fresh profile per image: Chrome keeps the previous one locked for a moment after exiting
  for (let attempt = 1; ; attempt++) {
    const prof = path.join(OUT, 'chromeprof', `${name}-${Date.now()}`);
    try {
      execFileSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', `--user-data-dir=${prof}`, `--window-size=${w},${h}`,
        '--force-device-scale-factor=1', ...(transparent ? ['--default-background-color=00000000'] : []), '--virtual-time-budget=4000',
        `--screenshot=${out}`, pathToFileURL(file).href], { stdio: 'ignore', timeout: 60000 });
      break;
    } catch (e) {
      if (attempt >= 3) throw e;
    }
  }
  console.log('rendered', name);
}

if (process.argv.includes('--install')) {
  const ASSET = path.resolve(HERE, '../../Assets/_Game/Art/Brand');
  const STORE = path.resolve(HERE, '../../Store/graphics');
  fs.mkdirSync(ASSET, { recursive: true });
  for (const [name] of jobs) if (!name.endsWith('-feature')) fs.copyFileSync(path.join(PNG, `${name}.png`), path.join(ASSET, `${name}.png`)); // the feature graphic is store-only
  for (const id of Object.keys(GAMES)) {
    fs.copyFileSync(path.join(PNG, `${id}-icon.png`), path.join(STORE, `${id}-icon-1024.png`));
    fs.copyFileSync(path.join(PNG, `${id}-feature.png`), path.join(STORE, `${id}-feature.png`));
  }
  console.log('installed');
}
