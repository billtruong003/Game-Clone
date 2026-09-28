"""Import a ChatGPT sheet generated per Docs/GPT_ASSET_BRIEF.md.

  python Tools/art/import-gpt.py blocks.png --grid 3x3 --batch 02_blocks block_red block_orange ...
  python Tools/art/import-gpt.py jar.png --grid 2x1 --batch 04_jar --full-bleed jar_back jar_front
  python Tools/art/import-gpt.py icons.png --auto --batch 08_icons play pause ...

Each cell: keep GPT's alpha (a flat background is only removed if the image has no transparency), drop alpha specks,
trim empty margins (unless --full-bleed), keep native resolution, save Tools/art/gpt/<batch>/<name>.png.
Prints a check table: empty cells, objects touching their cell border (grid rule broken), sizes.
"""
import argparse
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'gpt')


def has_alpha(im):
    return im.getchannel('A').getextrema()[0] < 250


def remove_flat_bg(im, tol=40):
    rgb = im.convert('RGB')
    w, h = im.size
    for seed in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]:
        ImageDraw.floodfill(rgb, seed, (255, 0, 255), thresh=tol)
    px, pr = im.load(), rgb.load()
    for y in range(h):
        for x in range(w):
            if pr[x, y] == (255, 0, 255):
                px[x, y] = (0, 0, 0, 0)
    return im


def clean(im, floor=8):
    r, g, b, a = im.split()
    return Image.merge('RGBA', (r, g, b, a.point(lambda v: 0 if v < floor else v)))


def touches_border(cell, band=3):
    a = cell.getchannel('A').point(lambda v: 255 if v > 40 else 0)
    w, h = a.size
    edges = [a.crop((0, 0, w, band)), a.crop((0, h - band, w, h)), a.crop((0, 0, band, h)), a.crop((w - band, 0, w, h))]
    return any(e.getbbox() for e in edges)


def auto_cells(im, min_gap=12):
    """Islands of opaque pixels, nested islands merged, reading order (see EyeArcade import-art for the same idea)."""
    import numpy as np
    from collections import deque
    k = 4
    a = np.asarray(im.getchannel('A')) > 24
    h, w = a.shape
    a = np.pad(a, ((0, (-h) % k), (0, (-w) % k)))
    m = a.reshape(a.shape[0] // k, k, a.shape[1] // k, k).any(axis=(1, 3))
    H, W = m.shape
    seen = np.zeros_like(m)
    boxes = []
    for y in range(H):
        for x in range(W):
            if not m[y, x] or seen[y, x]:
                continue
            q = deque([(y, x)]); seen[y, x] = True
            y0 = y1 = y; x0 = x1 = x
            while q:
                cy, cx = q.popleft()
                y0, y1, x0, x1 = min(y0, cy), max(y1, cy), min(x0, cx), max(x1, cx)
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < H and 0 <= nx < W and m[ny, nx] and not seen[ny, nx]:
                            seen[ny, nx] = True; q.append((ny, nx))
            boxes.append([x0 * k, y0 * k, (x1 + 1) * k, (y1 + 1) * k])
    near = max(1, min_gap // 2)
    changed = True
    while changed:
        changed = False
        for i in range(len(boxes)):
            for j in range(i + 1, len(boxes)):
                A, B = boxes[i], boxes[j]
                if A[0] - near < B[2] and B[0] - near < A[2] and A[1] - near < B[3] and B[1] - near < A[3]:
                    boxes[i] = [min(A[0], B[0]), min(A[1], B[1]), max(A[2], B[2]), max(A[3], B[3])]
                    boxes.pop(j); changed = True; break
            if changed:
                break
    area = lambda b: (b[2] - b[0]) * (b[3] - b[1])
    big = max(map(area, boxes), default=0)
    boxes = sorted([b for b in boxes if area(b) >= big * 0.02], key=lambda b: (b[1] + b[3]) / 2)
    hs = sorted(b[3] - b[1] for b in boxes)
    tol = hs[len(hs) // 2] / 2 if hs else 0
    rows, cur, anchor = [], [], None
    for b in boxes:
        cy = (b[1] + b[3]) / 2
        if cur and cy - anchor > tol:
            rows.append(cur); cur = []
        if not cur:
            anchor = cy
        cur.append(b)
    if cur:
        rows.append(cur)
    return [im.crop(tuple(b)) for row in rows for b in sorted(row, key=lambda b: b[0])]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('input')
    ap.add_argument('names', nargs='+')
    ap.add_argument('--batch', required=True, help='folder under Tools/art/gpt, e.g. 02_blocks')
    ap.add_argument('--grid', help='CxR')
    ap.add_argument('--auto', action='store_true')
    ap.add_argument('--full-bleed', action='store_true', help='keep the whole cell (backgrounds, 9-slice parts)')
    a = ap.parse_args()

    src = Image.open(a.input).convert('RGBA')
    if not has_alpha(src):
        print('WARN no transparency in the image: removing the flat background (ask GPT for a real transparent PNG)')
        src = remove_flat_bg(src)
    src = clean(src)

    if a.auto:
        cells = auto_cells(src)
        borders = [False] * len(cells)
    else:
        cols, rows = map(int, a.grid.lower().split('x'))
        cw, ch = src.width / cols, src.height / rows
        boxes = [(round(c * cw), round(r * ch), round((c + 1) * cw), round((r + 1) * ch)) for r in range(rows) for c in range(cols)]
        cells = [src.crop(b) for b in boxes]
        borders = [touches_border(c) for c in cells]

    if len(a.names) > len(cells):
        raise SystemExit(f'{len(a.names)} names but only {len(cells)} cells')
    os.makedirs(os.path.join(OUT, a.batch), exist_ok=True)
    print(f'{"name":<22} {"size":>10}  check')
    for name, cell, edge in zip(a.names, cells, borders):
        if name in ('-', '_'):
            continue
        bbox = cell.getchannel('A').getbbox()
        if bbox is None:
            print(f'{name:<22} {"":>10}  EMPTY cell')
            continue
        out = cell if a.full_bleed else cell.crop(bbox)
        out.save(os.path.join(OUT, a.batch, name + '.png'))
        note = 'WARN touches cell border (object crosses the grid?)' if edge and not a.full_bleed else 'ok'
        print(f'{name:<22} {out.width:>4}x{out.height:<5}  {note}')


if __name__ == '__main__':
    main()
