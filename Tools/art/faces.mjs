// Deadpan face set — the single source for the mockup (Face.dc.html) and the game (Texture2DArray, 9 slices).
//
// Style rules (keep them when adding a face):
//  - ink only (#1E2240) on transparent, plus one accent colour for tears / sweat; no whites, no blush, no highlights
//  - strokes are brush-inked: tapered ends, slightly uneven width, never a perfect geometric line
//  - eyes are small and far apart; the joke is the blank stare, not big cute eyes
//  - everything sits inside a 256 box, centred a bit high, so the body shows around it
//
// Slice order is the game's FaceId enum: Stare, Smug, Meh, Grin, Blink, Shock, Panic, Cry, Dizzy.

export const INK = '#1E2240';
export const ACCENT = '#6FC3F7';
export const FACES = ['stare', 'smug', 'meh', 'grin', 'blink', 'shock', 'panic', 'cry', 'dizzy'];

// ---------- brush helpers ----------
// Line weight: features must still read on a 28 px tray block, so strokes are heavy for their size.
const WEIGHT = 1.2;
const f = n => Math.round(n * 10) / 10;

// deterministic wobble so a face renders the same every build
function hash(s) {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) h = Math.imul(h ^ s.charCodeAt(i), 16777619);
  return () => ((h = Math.imul(h ^ (h >>> 15), 2246822507) ^ Math.imul(h ^ (h >>> 13), 3266489909)) >>> 0) / 4294967296;
}

// quadratic / cubic sample
function sample(pts, n = 24) {
  const out = [];
  for (let i = 0; i <= n; i++) {
    const t = i / n;
    if (pts.length === 2) out.push([pts[0][0] + (pts[1][0] - pts[0][0]) * t, pts[0][1] + (pts[1][1] - pts[0][1]) * t]);
    else if (pts.length === 3) {
      const u = 1 - t;
      out.push([u * u * pts[0][0] + 2 * u * t * pts[1][0] + t * t * pts[2][0], u * u * pts[0][1] + 2 * u * t * pts[1][1] + t * t * pts[2][1]]);
    } else {
      const u = 1 - t;
      out.push([
        u * u * u * pts[0][0] + 3 * u * u * t * pts[1][0] + 3 * u * t * t * pts[2][0] + t * t * t * pts[3][0],
        u * u * u * pts[0][1] + 3 * u * u * t * pts[1][1] + 3 * u * t * t * pts[2][1] + t * t * t * pts[3][1],
      ]);
    }
  }
  return out;
}

/** A tapered brush stroke along a 2–4 point Bezier. w = max width; taper = how thin the ends get (0..1). */
export function brush(pts, w0, { taper = 0.55, seed = 'b', col = INK, bias = 0 } = {}) {
  const w = w0 * WEIGHT;
  const rnd = hash(seed + pts.flat().join(','));
  const p = sample(pts, 28);
  const L = [], R = [];
  const wob = rnd() * 0.12 + 0.94;
  for (let i = 0; i < p.length; i++) {
    const a = p[Math.max(0, i - 1)], b = p[Math.min(p.length - 1, i + 1)];
    let nx = -(b[1] - a[1]), ny = b[0] - a[0];
    const len = Math.hypot(nx, ny) || 1;
    nx /= len; ny /= len;
    const t = i / (p.length - 1);
    // fat in the middle (shifted by bias), thin at both ends, a little uneven
    const tt = Math.min(1, Math.max(0, t + bias * (1 - t) * t));
    const body = Math.sin(Math.PI * tt);
    const hw = (w / 2) * ((1 - taper) + taper * Math.pow(body, 0.6)) * (wob + (rnd() - 0.5) * 0.08);
    L.push([p[i][0] + nx * hw, p[i][1] + ny * hw]);
    R.push([p[i][0] - nx * hw, p[i][1] - ny * hw]);
  }
  const ring = L.concat(R.reverse());
  return `<path d="M${ring.map(q => `${f(q[0])} ${f(q[1])}`).join(' L')} Z" fill="${col}"/>`;
}

/** Slightly lopsided ink dot. */
export function dot(cx, cy, r0, { seed = 'd', col = INK, squash = 0.88, tilt = -12 } = {}) {
  const r = r0 * WEIGHT;
  const rnd = hash(seed + cx + ',' + cy);
  const rx = r * (1 + (rnd() - 0.5) * 0.08), ry = r * squash * (1 + (rnd() - 0.5) * 0.08);
  return `<ellipse cx="${f(cx)}" cy="${f(cy)}" rx="${f(rx)}" ry="${f(ry)}" transform="rotate(${tilt} ${f(cx)} ${f(cy)})" fill="${col}"/>`;
}

/** Filled blob from a closed cubic-ish outline (mouth openings, tears). */
const blob = (d, col = INK) => `<path d="${d}" fill="${col}"/>`;

// eye positions shared by most faces — wide apart and a little high
const LX = 88, RX = 170, EY = 108;

// ---------- the nine faces ----------
const FACE_BODY = {
  // idle: the blank stare. Mouth short and a hair off-centre.
  stare: () =>
    dot(LX, EY, 12, { seed: 's1' }) + dot(RX, EY + 2, 11.5, { seed: 's2' }) +
    brush([[112, 162], [134, 160], [152, 163]], 11, { seed: 'sm', taper: 0.7 }),

  // lids pulled down flat over the pupils, one-sided smirk
  smug: () =>
    brush([[62, 104], [88, 101], [114, 104]], 9, { seed: 'l1', taper: 0.75 }) +
    brush([[144, 102], [170, 99], [196, 102]], 9, { seed: 'l2', taper: 0.75 }) +
    blob(`M${LX - 12} 106 Q${LX} 124 ${LX + 12} 106 Z`) + blob(`M${RX - 12} 104 Q${RX} 122 ${RX + 12} 104 Z`) +
    brush([[108, 166], [140, 170], [162, 150]], 11, { seed: 'sm2', taper: 0.7, bias: 0.4 }),

  // unimpressed: one eye squinting flat, the other a dot; tiny wavy mouth
  meh: () =>
    brush([[70, 110], [88, 108], [106, 111]], 10, { seed: 'm1', taper: 0.7 }) +
    dot(RX, EY, 11.5, { seed: 'm2' }) +
    brush([[168, 82], [182, 76], [196, 80]], 7, { seed: 'm3', taper: 0.8 }) +
    brush([[110, 164], [122, 158], [134, 164], [150, 160]], 9, { seed: 'm4', taper: 0.7 }),

  // content: the same stare, the mouth finally tips up. Still underplayed.
  grin: () =>
    brush([[74, 112], [88, 100], [102, 112]], 10, { seed: 'g1', taper: 0.7 }) +
    brush([[156, 112], [170, 100], [184, 112]], 10, { seed: 'g2', taper: 0.7 }) +
    brush([[100, 150], [128, 176], [158, 150]], 12, { seed: 'g3', taper: 0.65 }),

  // blink frame of the stare
  blink: () =>
    brush([[74, 110], [88, 113], [102, 110]], 8, { seed: 'k1', taper: 0.8 }) +
    brush([[156, 112], [170, 115], [184, 112]], 8, { seed: 'k2', taper: 0.8 }) +
    brush([[112, 162], [134, 160], [152, 163]], 11, { seed: 'sm', taper: 0.7 }),

  // falling: pupils shrink, eyes drift apart, small open mouth, one sweat drop
  shock: () =>
    dot(LX - 6, EY - 4, 7.5, { seed: 'h1', squash: 1 }) + dot(RX + 6, EY - 2, 7, { seed: 'h2', squash: 1 }) +
    blob('M122 150 C112 150 110 170 118 178 C126 186 140 182 141 168 C142 154 132 150 122 150 Z') +
    blob('M204 74 C200 86 196 92 198 98 C200 104 210 104 212 98 C214 92 208 84 204 74 Z', ACCENT),

  // falling fast: shaking eyes, big wobbly scream, two sweat drops
  panic: () =>
    dot(LX - 4, EY - 6, 8, { seed: 'p1', squash: 1 }) + dot(RX + 4, EY - 6, 8, { seed: 'p2', squash: 1 }) +
    brush([[LX - 26, EY - 6], [LX - 22, EY - 2], [LX - 26, EY + 4]], 4, { seed: 'p3', taper: 0.9 }) +
    brush([[RX + 26, EY - 6], [RX + 22, EY - 2], [RX + 26, EY + 4]], 4, { seed: 'p4', taper: 0.9 }) +
    blob('M104 146 C108 138 150 136 156 146 C162 158 154 196 130 198 C106 200 98 160 104 146 Z') +
    blob('M50 70 C46 82 42 88 44 94 C46 100 56 100 58 94 C60 88 54 80 50 70 Z', ACCENT) +
    blob('M210 66 C206 78 202 84 204 90 C206 96 216 96 218 90 C220 84 214 76 210 66 Z', ACCENT),

  // lost: dots with a tear running down each, mouth sagging
  cry: () =>
    dot(LX, EY, 11, { seed: 'c1' }) + dot(RX, EY + 2, 10.5, { seed: 'c2' }) +
    brush([[LX + 2, EY + 16], [LX, EY + 40], [LX + 4, EY + 62]], 10, { seed: 'c3', taper: 0.5, col: ACCENT }) +
    brush([[RX - 2, EY + 18], [RX + 1, EY + 38], [RX - 2, EY + 56]], 9, { seed: 'c4', taper: 0.5, col: ACCENT }) +
    brush([[108, 170], [128, 156], [152, 170]], 11, { seed: 'c5', taper: 0.65 }),

  // hit hard / game over: crossed eyes, loose squiggle mouth
  dizzy: () =>
    brush([[LX - 13, EY - 13], [LX + 13, EY + 13]], 9, { seed: 'z1', taper: 0.6 }) +
    brush([[LX + 13, EY - 13], [LX - 13, EY + 13]], 9, { seed: 'z2', taper: 0.6 }) +
    brush([[RX - 13, EY - 11], [RX + 13, EY + 15]], 9, { seed: 'z3', taper: 0.6 }) +
    brush([[RX + 13, EY - 11], [RX - 13, EY + 15]], 9, { seed: 'z4', taper: 0.6 }) +
    brush([[104, 164], [118, 154], [130, 166], [150, 158]], 10, { seed: 'z5', taper: 0.7 }),
};

/** Inner SVG (256 box) of one face. */
export function faceInner(name) {
  const fn = FACE_BODY[name];
  if (!fn) throw new Error('unknown face ' + name);
  // features are drawn around (128,130); scale them out so they fill the box
  return `<g transform="translate(128 128) scale(1.32) translate(-128 -134)">${fn()}</g>`;
}

/** Stand-alone SVG of one face at the given pixel size. */
export function faceSvg(name, size = 256) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 256 256">${faceInner(name)}</svg>`;
}
