# Hands are painted outside (ChatGPT, transparent PNG in Tools/art/src/hand_*.png). This packs them into the
# Assets/_Game/Art/Sheets/hands sheet (256 px cells) so SheetSlicer picks them up like every other sheet,
# and prints where the fingertip sits so UIKit.Hand can put the tip exactly on a target.
import glob, json, os
from PIL import Image
CELL = 256
srcs = sorted(glob.glob(os.path.join(os.path.dirname(__file__), 'src', 'hand_*.png')))
sheet = Image.new('RGBA', (CELL * len(srcs), CELL), (0, 0, 0, 0))
sprites = []
for i, p in enumerate(srcs):
    im = Image.open(p).convert('RGBA')
    im = im.crop(im.getbbox())
    side = max(im.size)
    sq = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
    sq = sq.resize((CELL - 4, CELL - 4), Image.LANCZOS)
    sheet.paste(sq, (i * CELL + 2, 2))
    a = sq.getchannel('A').load()
    tip = min(((x, y) for y in range(sq.height) for x in range(sq.width) if a[x, y] > 128), key=lambda q: q[0] + q[1])
    name = os.path.splitext(os.path.basename(p))[0]
    sprites.append({'name': name, 'x': i * CELL, 'y': 0, 'w': CELL, 'h': CELL})
    print(name, 'tip (fraction of cell, top-left origin):', round((tip[0] + 2) / CELL, 3), round((tip[1] + 2) / CELL, 3))
out = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', '_Game', 'Art', 'Sheets')
sheet.save(os.path.join(out, 'hands.png'))
json.dump({'sheet': 'hands', 'width': sheet.width, 'height': CELL, 'pixelsPerUnit': 100, 'sprites': sprites}, open(os.path.join(out, 'hands.json'), 'w'), indent=1)
