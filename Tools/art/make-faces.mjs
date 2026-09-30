// Deadpan faces → Assets/_Game/Art/Faces/faces.png, a 3×3 flipbook that Unity imports as a Texture2DArray
// (FaceTextureImporter). `node make-faces.mjs --preview` also writes preview/faces_preview.png (faces on bodies).
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
import { fileURLToPath } from 'node:url';
import { FACES, faceInner, INK } from './faces.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT_DIR = path.resolve(HERE, '../../Assets/_Game/Art/Faces');
const SLICE = 256;

async function atlas() {
  const cells = FACES.map((name, i) =>
    `<g transform="translate(${(i % 3) * SLICE} ${Math.floor(i / 3) * SLICE})">${faceInner(name)}</g>`).join('');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${SLICE * 3}" height="${SLICE * 3}" viewBox="0 0 ${SLICE * 3} ${SLICE * 3}">${cells}</svg>`;
  fs.mkdirSync(OUT_DIR, { recursive: true });
  await sharp(Buffer.from(svg)).png().toFile(path.join(OUT_DIR, 'faces.png'));
  console.log('faces.png', SLICE * 3);
}

// Faces on the real bodies at game sizes, rotated like the balls do, to judge them before export.
async function preview() {
  const cols = ['#FF5A5F', '#FFD23F', '#4EA8DE', '#3DDC97', '#9B5DE5', '#FF9F1C', '#F15BB5', '#2A9D8F', '#E76F51'];
  const body = (shape, x, y, s, col, face, rot = 0) => {
    const bw = Math.max(2, s * 0.035);
    const outline = shape === 'ball'
      ? `<circle cx="${x + s / 2}" cy="${y + s / 2}" r="${s / 2 - bw / 2}" fill="${col}" stroke="${INK}" stroke-width="${bw}"/>` +
        `<ellipse cx="${x + s * 0.3}" cy="${y + s * 0.2}" rx="${s * 0.09}" ry="${s * 0.045}" transform="rotate(-30 ${x + s * 0.3} ${y + s * 0.2})" fill="#fff" opacity="0.45"/>`
      : `<rect x="${x + bw / 2}" y="${y + bw / 2}" width="${s - bw}" height="${s - bw}" rx="${s * 0.2}" fill="${col}" stroke="${INK}" stroke-width="${bw}"/>` +
        `<rect x="${x + s * 0.12}" y="${y + s * 0.09}" width="${s * 0.22}" height="${s * 0.07}" rx="${s * 0.035}" fill="#fff" opacity="0.4"/>`;
    const fs_ = s * 0.66, fx = x + (s - fs_) / 2, fy = y + (s - fs_) / 2 - s * 0.04;
    return outline + `<g transform="rotate(${rot} ${x + s / 2} ${y + s / 2}) translate(${fx} ${fy}) scale(${fs_ / 256})">${faceInner(face)}</g>`;
  };
  let g = '';
  FACES.forEach((name, i) => {
    g += body('ball', 20 + i * 170, 20, 150, cols[i], name, 0);
    g += body('block', 30 + i * 170, 200, 130, cols[(i + 3) % 9], name);
    g += body('ball', 40 + i * 170, 350, 90, cols[(i + 5) % 9], name, [0, -25, 40, 15, -60, 90, -120, 20, 180][i]);
    g += body('block', 45 + i * 170, 460, 55, cols[(i + 1) % 9], name);
    g += body('block', 110 + i * 170, 475, 28, cols[(i + 2) % 9], name);
    g += `<text x="${95 + i * 170}" y="570" font-family="Arial" font-size="20" font-weight="700" fill="#C9C3E8" text-anchor="middle">${name}</text>`;
  });
  const w = 20 + FACES.length * 170, h = 590;
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w * 2}" height="${h * 2}" viewBox="0 0 ${w} ${h}"><rect width="${w}" height="${h}" fill="#2F2552"/>${g}</svg>`;
  fs.mkdirSync(path.join(HERE, 'preview'), { recursive: true });
  await sharp(Buffer.from(svg)).png().toFile(path.join(HERE, 'preview', 'faces_preview.png'));
  console.log('preview written');
}

if (process.argv.includes('--preview')) await preview();
else await atlas();
