// Extra face packs for the shop (Sleepy, Grumpy, Derp). Same rules and brush helpers as faces.mjs: ink only plus the
// one accent colour, small eyes far apart, deadpan humour. Each pack has the same 9 slots as the FaceId enum
// (stare, smug, meh, grin, blink, shock, panic, cry, dizzy) so a pack is just another Texture2DArray.
import { brush, dot, INK, ACCENT, FACES } from './faces.mjs';

const blob = (d, col = INK) => `<path d="${d}" fill="${col}"/>`;
const LX = 88, RX = 170, EY = 108;

// ---------- shared pieces ----------
const flatMouth = (s = 'fm') => brush([[112, 162], [134, 160], [152, 163]], 11, { seed: s, taper: 0.7 });
const smirk = (s = 'sk') => brush([[108, 166], [140, 170], [162, 150]], 11, { seed: s, taper: 0.7, bias: 0.4 });
const smile = (s = 'sl') => brush([[100, 150], [128, 176], [158, 150]], 12, { seed: s, taper: 0.65 });
const frown = (s = 'fr') => brush([[106, 172], [130, 156], [154, 172]], 11, { seed: s, taper: 0.65 });
const oMouth = () => blob('M122 150 C112 150 110 170 118 178 C126 186 140 182 141 168 C142 154 132 150 122 150 Z');
const scream = () => blob('M104 146 C108 138 150 136 156 146 C162 158 154 196 130 198 C106 200 98 160 104 146 Z');
const sweat = (x, y) => blob(`M${x} ${y} C${x - 4} ${y + 12} ${x - 8} ${y + 18} ${x - 6} ${y + 24} C${x - 4} ${y + 30} ${x + 6} ${y + 30} ${x + 8} ${y + 24} C${x + 10} ${y + 18} ${x + 4} ${y + 10} ${x} ${y} Z`, ACCENT);
const tears = () =>
  brush([[LX + 2, EY + 16], [LX, EY + 40], [LX + 4, EY + 62]], 10, { seed: 't1', taper: 0.5, col: ACCENT }) +
  brush([[RX - 2, EY + 18], [RX + 1, EY + 38], [RX - 2, EY + 56]], 9, { seed: 't2', taper: 0.5, col: ACCENT });
const closed = (s, dy = 0) =>
  brush([[72, EY + dy], [88, EY + 3 + dy], [104, EY + dy]], 8, { seed: s + 'a', taper: 0.8 }) +
  brush([[154, EY + 2 + dy], [170, EY + 5 + dy], [186, EY + 2 + dy]], 8, { seed: s + 'b', taper: 0.8 });
const xEyes = s =>
  brush([[LX - 13, EY - 13], [LX + 13, EY + 13]], 9, { seed: s + '1', taper: 0.6 }) +
  brush([[LX + 13, EY - 13], [LX - 13, EY + 13]], 9, { seed: s + '2', taper: 0.6 }) +
  brush([[RX - 13, EY - 11], [RX + 13, EY + 15]], 9, { seed: s + '3', taper: 0.6 }) +
  brush([[RX + 13, EY - 11], [RX - 13, EY + 15]], 9, { seed: s + '4', taper: 0.6 });
const squiggle = s => brush([[104, 164], [118, 154], [130, 166], [150, 158]], 10, { seed: s, taper: 0.7 });

// Sleepy: a heavy lid always cuts the eye in half; the pupil hangs under it.
const lidEye = (x, y, s) =>
  brush([[x - 19, y - 1], [x, y - 3], [x + 19, y]], 11, { seed: s + 'l', taper: 0.55 }) +
  blob(`M${x - 13} ${y + 1} Q${x} ${y + 21} ${x + 13} ${y + 1} Z`) +
  brush([[x - 12, y + 24], [x, y + 28], [x + 12, y + 24]], 4, { seed: s + 'g', taper: 0.8 });
const lids = s => lidEye(LX, EY, s + 'L') + lidEye(RX, EY + 2, s + 'R');
const zz = (x, y, k = 1) =>
  brush([[x, y], [x + 14 * k, y], [x, y + 14 * k], [x + 14 * k, y + 14 * k]], 7, { seed: 'z' + x + y, taper: 0.5 });

const SLEEPY = {
  stare: () => lids('s') + brush([[116, 164], [132, 163], [146, 165]], 9, { seed: 'ss', taper: 0.7 }),
  smug: () => lids('m') + smirk('sm'),
  meh: () => lidEye(LX, EY, 'mh') + brush([[154, EY + 3], [170, EY + 5], [186, EY + 3]], 8, { seed: 'mhc', taper: 0.8 }) +
    brush([[110, 164], [122, 158], [134, 164], [150, 160]], 9, { seed: 'mhm', taper: 0.7 }),
  grin: () => closed('g', 2) + smile('gs'),
  blink: () => closed('b', 4) + flatMouth('bm') + zz(176, 58, 1.4) + zz(204, 34, 1),
  // snapped awake: round eyes with the lids still hanging above them
  shock: () => brush([[70, EY - 22], [88, EY - 25], [106, EY - 21]], 7, { seed: 'hl', taper: 0.8 }) +
    brush([[152, EY - 20], [170, EY - 23], [188, EY - 19]], 7, { seed: 'hr', taper: 0.8 }) +
    dot(LX, EY, 8, { seed: 'h1', squash: 1 }) + dot(RX, EY + 2, 8, { seed: 'h2', squash: 1 }) + oMouth() + sweat(206, 70),
  panic: () => dot(LX - 4, EY - 4, 9, { seed: 'p1', squash: 1 }) + dot(RX + 4, EY - 4, 9, { seed: 'p2', squash: 1 }) +
    brush([[LX - 14, EY + 18], [LX, EY + 24], [LX + 14, EY + 18]], 5, { seed: 'pb1', taper: 0.8 }) +
    brush([[RX - 14, EY + 18], [RX, EY + 24], [RX + 14, EY + 18]], 5, { seed: 'pb2', taper: 0.8 }) + scream() + sweat(48, 66),
  cry: () => closed('c', 2) + tears() + frown('cf'),
  // out cold: flat lids, open drooling mouth
  dizzy: () => closed('d', 2) + blob('M116 156 C112 170 124 180 134 176 C144 172 146 158 138 154 C130 150 120 150 116 156 Z') +
    brush([[140, 176], [142, 190], [140, 204]], 7, { seed: 'dr', taper: 0.4, col: ACCENT }),
};

// Grumpy: two heavy brows pulled down to the nose on every face.
const brows = (s, k = 1) =>
  brush([[60, 80 - 6 * k], [82, 84], [106, 92 + 4 * k]], 11, { seed: s + 'L', taper: 0.55 }) +
  brush([[152, 94 + 4 * k], [176, 86], [198, 82 - 6 * k]], 11, { seed: s + 'R', taper: 0.55 });
// worried brows (inner ends up) for shock, panic, cry
const upBrows = s =>
  brush([[62, 90], [84, 84], [104, 72]], 10, { seed: s + 'L', taper: 0.55 }) +
  brush([[154, 72], [174, 84], [196, 90]], 10, { seed: s + 'R', taper: 0.55 });

const GRUMPY = {
  stare: () => brows('s') + dot(LX, EY + 4, 11, { seed: 'g1' }) + dot(RX, EY + 6, 10.5, { seed: 'g2' }) + frown('gf'),
  smug: () => brows('m', 0.6) +
    brush([[66, 110], [88, 107], [110, 110]], 9, { seed: 'ml', taper: 0.75 }) + brush([[148, 110], [170, 107], [192, 110]], 9, { seed: 'mr', taper: 0.75 }) +
    smirk('ms'),
  meh: () => brush([[60, 88], [82, 90], [106, 96]], 11, { seed: 'e1', taper: 0.55 }) +
    brush([[152, 82], [176, 70], [198, 74]], 11, { seed: 'e2', taper: 0.55 }) +
    dot(LX, EY + 6, 10.5, { seed: 'e3' }) + dot(RX, EY + 2, 11, { seed: 'e4' }) +
    brush([[110, 166], [132, 164], [150, 168]], 10, { seed: 'e5', taper: 0.7 }),
  // the reluctant smile: brows still angry
  grin: () => brows('r') + dot(LX, EY + 6, 10, { seed: 'r1' }) + dot(RX, EY + 8, 9.5, { seed: 'r2' }) + smile('rs'),
  blink: () => brows('b') + closed('bk', 6) + frown('bf'),
  shock: () => upBrows('h') + dot(LX - 4, EY, 7.5, { seed: 'h1', squash: 1 }) + dot(RX + 4, EY + 2, 7, { seed: 'h2', squash: 1 }) + oMouth() + sweat(206, 60),
  panic: () => upBrows('p') + dot(LX - 4, EY - 2, 8, { seed: 'p1', squash: 1 }) + dot(RX + 4, EY - 2, 8, { seed: 'p2', squash: 1 }) + scream() + sweat(46, 60) + sweat(212, 56),
  cry: () => upBrows('c') + dot(LX, EY + 2, 10.5, { seed: 'c1' }) + dot(RX, EY + 4, 10, { seed: 'c2' }) + tears() + frown('cf'),
  dizzy: () => brows('d', 0.5) + xEyes('dx') + squiggle('dq'),
};

// Derp: one big eye, one tiny eye, a tongue that never goes back in.
const tongue = (x = 138, y = 164) =>
  brush([[x - 13, y - 1], [x - 14, y + 30], [x + 14, y + 30], [x + 13, y - 1]], 8, { seed: 'tg' + x, taper: 0.25 }) +
  brush([[x, y + 4], [x + 1, y + 16]], 5, { seed: 'tc' + x, taper: 0.6 });
const bigEye = (s, dx = 0, dy = 0) => dot(LX - 4 + dx, EY - 2 + dy, 15, { seed: s, squash: 0.95 });
const tinyEye = (s, dx = 0, dy = 0) => dot(RX + 6 + dx, EY + 10 + dy, 6.5, { seed: s, squash: 1 });

const DERP = {
  stare: () => bigEye('a1') + tinyEye('a2') + brush([[104, 160], [126, 164], [158, 158]], 10, { seed: 'a3', taper: 0.7 }) + tongue(),
  smug: () => brush([[60, 104], [84, 100], [108, 104]], 9, { seed: 'm1', taper: 0.75 }) + blob(`M${LX - 16} 106 Q${LX - 4} 128 ${LX + 10} 106 Z`) +
    tinyEye('m2') + smirk('ms') + tongue(150, 158),
  meh: () => bigEye('e1', 4) + brush([[166, EY + 10], [176, EY + 11], [186, EY + 9]], 7, { seed: 'e2', taper: 0.8 }) +
    brush([[110, 164], [122, 158], [134, 164], [150, 160]], 9, { seed: 'e3', taper: 0.7 }) + tongue(126, 164),
  grin: () => bigEye('g1') + tinyEye('g2') + smile('gs') + tongue(128, 170),
  blink: () => brush([[66, EY], [84, EY + 4], [102, EY]], 9, { seed: 'b1', taper: 0.8 }) + tinyEye('b2') +
    brush([[104, 160], [126, 164], [158, 158]], 10, { seed: 'b3', taper: 0.7 }) + tongue(),
  shock: () => dot(LX - 8, EY - 6, 17, { seed: 'h1', squash: 1 }) + dot(RX + 10, EY + 12, 5, { seed: 'h2', squash: 1 }) + oMouth() + sweat(206, 66),
  panic: () => dot(LX - 8, EY - 8, 17, { seed: 'p1', squash: 1 }) + dot(RX + 12, EY + 6, 6, { seed: 'p2', squash: 1 }) + scream() + sweat(214, 58),
  cry: () => bigEye('c1') + tinyEye('c2') + tears() + frown('cf') + tongue(130, 166),
  // spinning: one spiral eye, one tiny dot, tongue sideways
  dizzy: () => brush([[LX - 4, EY - 2], [LX + 12, EY - 14], [LX + 14, EY + 10], [LX - 14, EY + 12]], 7, { seed: 'd1', taper: 0.4 }) +
    brush([[LX - 14, EY + 12], [LX - 22, EY - 6], [LX - 4, EY - 22], [LX + 20, EY - 12]], 7, { seed: 'd2', taper: 0.4 }) +
    tinyEye('d3', 0, -6) + squiggle('dq') + tongue(152, 162),
};

export const PACKS = { sleepy: SLEEPY, grumpy: GRUMPY, derp: DERP };
export const PACK_NAMES = { deadpan: 'Deadpan', sleepy: 'Sleepy', grumpy: 'Grumpy', derp: 'Derp' };

/** Inner SVG (256 box) of one face of a pack, same framing as faces.mjs faceInner. */
export function packFaceInner(pack, name) {
  const fn = PACKS[pack]?.[name];
  if (!fn) throw new Error(`unknown face ${pack}/${name}`);
  return `<g transform="translate(128 128) scale(1.32) translate(-128 -134)">${fn()}</g>`;
}

export { FACES };
