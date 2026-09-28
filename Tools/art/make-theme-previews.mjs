// Placeholder previews of the 10 Eye Merge skins (3 sample tiers each), flat vector, for mockups and the shop.
// `node make-theme-previews.mjs` → Tools/art/theme-previews/<theme>_<i>.png. Final art comes from ChatGPT later.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(HERE, 'theme-previews');
fs.mkdirSync(OUT, { recursive: true });
const INK = '#1E2240';
const svg = (body) => `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">${body}</svg>`;
const ball = (fill, extra = '') => `<circle cx="128" cy="128" r="112" fill="${fill}" stroke="${INK}" stroke-width="12"/>${extra}` +
  `<ellipse cx="86" cy="76" rx="30" ry="16" fill="#fff" opacity="0.55" transform="rotate(-35 86 76)"/>`;
const eyes = (dy = 0) => [96, 160].map((x) => `<ellipse cx="${x}" cy="${118 + dy}" rx="18" ry="22" fill="#fff" stroke="${INK}" stroke-width="6"/><circle cx="${x + 3}" cy="${123 + dy}" r="9" fill="${INK}"/>`).join('') +
  `<path d="M104,${160 + dy} Q128,${178 + dy} 152,${160 + dy}" fill="none" stroke="${INK}" stroke-width="8" stroke-linecap="round"/>`;

const T = {
  fruit: [
    ball('#E63946', `<path d="M128,20 Q140,-4 162,6" fill="none" stroke="#3A5A40" stroke-width="10" stroke-linecap="round"/><ellipse cx="150" cy="22" rx="22" ry="11" fill="#3DDC97" stroke="${INK}" stroke-width="6"/>` + eyes()),
    ball('#FF9F1C', Array.from({ length: 8 }, (_, i) => `<circle cx="${128 + 70 * Math.cos(i * 0.785)}" cy="${128 + 70 * Math.sin(i * 0.785)}" r="5" fill="#E07A00"/>`).join('') + eyes()),
    ball('#3DDC97', Array.from({ length: 7 }, (_, i) => `<path d="M${40 + i * 30},30 Q${30 + i * 30},128 ${40 + i * 30},226" fill="none" stroke="#1F8A5B" stroke-width="10"/>`).join('') + eyes()),
  ],
  billiards: [1, 5, 8].map((n, i) => ball(['#FFD23F', '#FF7A00', '#1E2240'][i],
    (n === 8 ? '' : `<rect x="16" y="92" width="224" height="72" fill="#FFF8EC" opacity="${i === 1 ? 1 : 0}"/>`) +
    `<circle cx="128" cy="128" r="46" fill="#FFF8EC" stroke="${INK}" stroke-width="5"/><text x="128" y="148" font-family="Arial Black,Arial" font-weight="900" font-size="58" text-anchor="middle" fill="${INK}">${n}</text>`)),
  planets: [
    ball('#B8BED4', `<circle cx="96" cy="150" r="18" fill="#9AA0B8"/><circle cx="160" cy="96" r="12" fill="#9AA0B8"/><circle cx="150" cy="170" r="10" fill="#9AA0B8"/>`),
    ball('#4EA8DE', `<path d="M40,110 Q90,80 140,120 T220,110" fill="none" stroke="#3DDC97" stroke-width="22" stroke-linecap="round"/><path d="M60,170 Q110,150 170,176" fill="none" stroke="#3DDC97" stroke-width="18" stroke-linecap="round"/>`),
    `<ellipse cx="128" cy="140" rx="126" ry="36" fill="none" stroke="#E9A23B" stroke-width="14" transform="rotate(-15 128 140)"/>` + ball('#F4C06A', `<path d="M24,110 L232,110" stroke="#E9A23B" stroke-width="14"/><path d="M28,150 L228,150" stroke="#D98B2B" stroke-width="10"/>`),
  ],
  candy: [
    ball('#F15BB5', `<path d="M60,70 L196,186 M40,120 L136,210 M120,46 L216,136" stroke="#fff" stroke-width="18" stroke-linecap="round"/>`),
    `<circle cx="128" cy="128" r="112" fill="#E9A23B" stroke="${INK}" stroke-width="12"/><circle cx="128" cy="128" r="40" fill="#2B2F55" stroke="${INK}" stroke-width="10"/>` +
      `<path d="M40,100 Q128,40 216,100 L200,140 Q128,90 56,140 Z" fill="#F15BB5"/>` + Array.from({ length: 9 }, (_, i) => `<rect x="${60 + i * 16}" y="${70 + (i % 3) * 14}" width="10" height="4" rx="2" fill="${['#FFD23F', '#4EA8DE', '#3DDC97'][i % 3]}"/>`).join(''),
    ball('#9B5DE5', `<path d="M128,16 L128,240" stroke="#fff" stroke-width="16"/><circle cx="128" cy="128" r="56" fill="none" stroke="#fff" stroke-width="14"/>`),
  ],
  pets: [
    `<circle cx="60" cy="56" r="34" fill="#C9A27A" stroke="${INK}" stroke-width="10"/><circle cx="196" cy="56" r="34" fill="#C9A27A" stroke="${INK}" stroke-width="10"/>` + ball('#E0B98E', eyes(6)),
    `<path d="M40,90 L60,20 L110,60 Z M216,90 L196,20 L146,60 Z" fill="#9AA0B8" stroke="${INK}" stroke-width="10" stroke-linejoin="round"/>` + ball('#B8BED4', eyes(6)),
    `<circle cx="58" cy="60" r="36" fill="#1E2240"/><circle cx="198" cy="60" r="36" fill="#1E2240"/>` + ball('#FFF8EC', `<ellipse cx="94" cy="122" rx="30" ry="36" fill="#1E2240"/><ellipse cx="162" cy="122" rx="30" ry="36" fill="#1E2240"/>` + eyes(0)),
  ],
  sports: [
    ball('#FFF8EC', `<path d="M60,40 Q128,128 60,216" fill="none" stroke="#FF5A5F" stroke-width="8"/><path d="M196,40 Q128,128 196,216" fill="none" stroke="#FF5A5F" stroke-width="8"/>`),
    ball('#C8F05A', `<path d="M40,60 Q128,128 40,196" fill="none" stroke="#fff" stroke-width="10"/><path d="M216,60 Q128,128 216,196" fill="none" stroke="#fff" stroke-width="10"/>`),
    ball('#FF8A3D', `<path d="M16,128 L240,128 M128,16 L128,240" stroke="${INK}" stroke-width="8"/><path d="M52,40 Q110,128 52,216 M204,40 Q146,128 204,216" fill="none" stroke="${INK}" stroke-width="8"/>`),
  ],
  gems: [
    ['#7FD1F7', '#4EA8DE'], ['#3DDC97', '#1F8A5B'], ['#FF5A5F', '#C7353A'],
  ].map(([a, b]) => `<polygon points="128,16 228,96 128,240 28,96" fill="${a}" stroke="${INK}" stroke-width="12" stroke-linejoin="round"/>` +
    `<polygon points="128,16 176,96 128,240 80,96" fill="${b}" opacity="0.6"/><path d="M28,96 L228,96" stroke="${INK}" stroke-width="8"/><polygon points="100,40 120,40 100,80" fill="#fff" opacity="0.7"/>`),
  neon: ['#F15BB5', '#4EA8DE', '#3DDC97'].map((c) =>
    `<circle cx="128" cy="128" r="118" fill="${c}" opacity="0.25"/><circle cx="128" cy="128" r="100" fill="#15173A" stroke="${c}" stroke-width="16"/>` +
    `<circle cx="128" cy="128" r="70" fill="none" stroke="${c}" stroke-width="6" opacity="0.6"/>` + eyes().replaceAll(INK, c).replaceAll('fill="#fff"', 'fill="#15173A"')),
};

let n = 0;
for (const [theme, list] of Object.entries(T)) {
  for (let i = 0; i < list.length; i++) {
    await sharp(Buffer.from(svg(list[i]))).png().toFile(path.join(OUT, `${theme}_${i}.png`));
    n++;
  }
}
console.log(`${n} theme previews → ${OUT}`);
