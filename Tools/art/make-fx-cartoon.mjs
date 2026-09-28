// Cartoon VFX drawn as vector (flat fills, soft outline, no noise/grain) so every effect matches the sticker style.
// `node make-fx-cartoon.mjs` → Tools/art/fx-cartoon/<name>.png (white-based sprites are tinted in Unity).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(HERE, 'fx-cartoon');
fs.mkdirSync(OUT, { recursive: true });

const INK = '#1E2240';
const svg = (w, h, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
const circle = (cx, cy, r, fill, extra = '') => `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${fill}" ${extra}/>`;
const pts = (cx, cy, rOut, rIn, n, rot = -90) => Array.from({ length: n * 2 }, (_, i) => {
  const r = i % 2 ? rIn : rOut;
  const a = ((rot + (i * 180) / n) * Math.PI) / 180;
  return `${(cx + r * Math.cos(a)).toFixed(1)},${(cy + r * Math.sin(a)).toFixed(1)}`;
}).join(' ');

const FX = {};

// Puff: overlapping round blobs, white with a soft shade crescent (reads as cartoon smoke). 4 frames: pop → grow → drift → fade.
[[0.55, 1], [0.85, 1], [1.0, 0.8], [1.1, 0.45]].forEach(([s, a], i) => {
  const blobs = [[128, 150, 58], [82, 140, 42], [174, 140, 44], [104, 100, 44], [152, 98, 40], [128, 76, 34]];
  const body = blobs.map(([x, y, r]) => circle(128 + (x - 128) * s, 128 + (y - 128) * s, r * s, '#FFFFFF')).join('') +
    blobs.slice(0, 3).map(([x, y, r]) => circle(128 + (x - 128) * s + r * s * 0.25, 128 + (y - 128) * s + r * s * 0.3, r * s * 0.6, '#DCE1F2')).join('');
  FX[`puff_${i}`] = svg(256, 256, `<g opacity="${a}">${body}</g>`);
});

// Burst: spiky star explosion with a white core (hit / merge pop).
FX.burst = svg(256, 256,
  `<polygon points="${pts(128, 128, 120, 62, 10)}" fill="#FFD23F" stroke="${INK}" stroke-width="8" stroke-linejoin="round"/>` +
  `<polygon points="${pts(128, 128, 76, 40, 10, -72)}" fill="#FFF8EC"/>`);
FX.burst_white = svg(256, 256, `<polygon points="${pts(128, 128, 120, 62, 10)}" fill="#FFFFFF"/>`);

// Pow: comic impact star with speed ticks around it.
FX.pow = svg(256, 256,
  `<polygon points="${pts(128, 128, 110, 70, 12, -80)}" fill="#FF5A5F" stroke="${INK}" stroke-width="8" stroke-linejoin="round"/>` +
  `<polygon points="${pts(128, 128, 70, 44, 12, -80)}" fill="#FFD23F"/>`);

// Sparkle: 4-point twinkle, rounded (the classic cartoon glint).
FX.sparkle = svg(128, 128, `<path d="M64,6 Q70,58 122,64 Q70,70 64,122 Q58,70 6,64 Q58,58 64,6 Z" fill="#FFFFFF"/>`);
FX.sparkle_outline = svg(128, 128, `<path d="M64,10 Q70,58 118,64 Q70,70 64,118 Q58,70 10,64 Q58,58 64,10 Z" fill="#FFD23F" stroke="${INK}" stroke-width="6" stroke-linejoin="round"/>`);

// Shock ring: thick flat ring (scales out and thins in Unity).
FX.ring = svg(256, 256, `<circle cx="128" cy="128" r="104" fill="none" stroke="#FFFFFF" stroke-width="22"/>`);

// Speed lines: radial dashes for big moments.
FX.speed_lines = svg(512, 512, Array.from({ length: 24 }, (_, i) => {
  const a = (i / 24) * Math.PI * 2, r1 = 150 + (i % 3) * 20, r2 = 250;
  return `<path d="M${256 + r1 * Math.cos(a)},${256 + r1 * Math.sin(a)} L${256 + r2 * Math.cos(a)},${256 + r2 * Math.sin(a)}" stroke="#FFFFFF" stroke-width="${10 - (i % 3) * 3}" stroke-linecap="round"/>`;
}).join(''));

// Confetti pieces (white, tinted per particle).
FX.confetti_rect = svg(64, 64, `<rect x="18" y="8" width="28" height="48" rx="6" fill="#FFFFFF"/>`);
FX.confetti_circle = svg(64, 64, circle(32, 32, 20, '#FFFFFF'));
FX.confetti_tri = svg(64, 64, `<polygon points="32,8 58,54 6,54" fill="#FFFFFF" stroke="#FFFFFF" stroke-width="6" stroke-linejoin="round"/>`);
FX.confetti_squiggle = svg(64, 64, `<path d="M10,40 Q20,16 32,32 T54,24" fill="none" stroke="#FFFFFF" stroke-width="10" stroke-linecap="round"/>`);

// Droplet splash (fruit / jelly merges) and a round shard (block pops).
FX.drop = svg(64, 64, `<path d="M32,4 Q52,32 48,42 A16,16 0 0 1 16,42 Q12,32 32,4 Z" fill="#FFFFFF"/>` + circle(26, 40, 5, '#DCE1F2'));
FX.shard = svg(64, 64, `<polygon points="10,20 40,6 58,34 30,58" fill="#FFFFFF" stroke="#FFFFFF" stroke-width="4" stroke-linejoin="round"/>`);

// Heart pop (love reactions, likes).
FX.heart = svg(128, 128, `<path d="M64,112 C8,76 10,36 38,30 C52,27 60,36 64,44 C68,36 76,27 90,30 C118,36 120,76 64,112 Z" fill="#FF5A5F" stroke="${INK}" stroke-width="7" stroke-linejoin="round"/>`);

// Soft glow disc for additive highlights: smooth radial falloff, no noise.
FX.glow = svg(256, 256, `<defs><radialGradient id="g"><stop offset="0" stop-color="#fff" stop-opacity="1"/><stop offset="0.45" stop-color="#fff" stop-opacity="0.55"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient></defs>` + circle(128, 128, 126, 'url(#g)'));

// Light rays behind a reward (rotates slowly).
FX.rays = svg(512, 512, Array.from({ length: 12 }, (_, i) => {
  const a0 = (i / 12) * Math.PI * 2, a1 = a0 + Math.PI / 24;
  return `<path d="M256,256 L${256 + 260 * Math.cos(a0)},${256 + 260 * Math.sin(a0)} L${256 + 260 * Math.cos(a1)},${256 + 260 * Math.sin(a1)} Z" fill="#FFFFFF" opacity="0.5"/>`;
}).join(''));

for (const [name, body] of Object.entries(FX)) await sharp(Buffer.from(body)).png().toFile(path.join(OUT, `${name}.png`));
console.log(`${Object.keys(FX).length} cartoon fx → ${OUT}`);
