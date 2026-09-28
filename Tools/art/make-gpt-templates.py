"""Grid templates to attach in ChatGPT: exact canvas size, cell lines, 80% safe area, cell names in reading order.
python Tools/art/make-gpt-templates.py → Tools/art/gpt-refs/T<batch>_<name>.png (+ REF_current_look.png)."""
import json, os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'gpt-refs')
os.makedirs(OUT, exist_ok=True)
try:
    FONT = ImageFont.truetype('arialbd.ttf', 22)
    SMALL = ImageFont.truetype('arial.ttf', 17)
except OSError:
    FONT = SMALL = ImageFont.load_default()

def template(fname, size, cols, rows, names, title, full_bleed=False):
    W, H = size
    im = Image.new('RGB', size, (246, 246, 250))
    d = ImageDraw.Draw(im)
    cw, ch = W / cols, H / rows
    for i in range(cols * rows):
        c, r = i % cols, i // cols
        x0, y0 = c * cw, r * ch
        d.rectangle((x0, y0, x0 + cw - 1, y0 + ch - 1), outline=(150, 156, 190), width=2)
        if not full_bleed:
            m = 0.1
            dash = (x0 + cw * m, y0 + ch * m, x0 + cw * (1 - m), y0 + ch * (1 - m))
            for k in range(int(dash[0]), int(dash[2]), 14):
                d.line((k, dash[1], k + 7, dash[1]), fill=(255, 140, 140), width=2)
                d.line((k, dash[3], k + 7, dash[3]), fill=(255, 140, 140), width=2)
            for k in range(int(dash[1]), int(dash[3]), 14):
                d.line((dash[0], k, dash[0], k + 7), fill=(255, 140, 140), width=2)
                d.line((dash[2], k, dash[2], k + 7), fill=(255, 140, 140), width=2)
        label = names[i] if i < len(names) else '(empty)'
        d.text((x0 + 8, y0 + 6), f'{i + 1}', fill=(120, 124, 160), font=SMALL)
        tw = d.textlength(label, font=FONT)
        d.text((x0 + (cw - tw) / 2, y0 + ch / 2 - 12), label, fill=(30, 34, 64) if i < len(names) else (180, 180, 200), font=FONT)
    d.text((10, H - 26), title + ('   [full-bleed cells]' if full_bleed else '   [red dashes = 80% safe area]'), fill=(200, 60, 60), font=SMALL)
    im.save(os.path.join(OUT, fname))

T = [
    ('T00_style_bible.png', (1536, 1024), 3, 2, ['ball', 'block', 'button', 'panel_corner', 'toon_splat', 'glass_jar'], 'Batch 00 style bible', False),
    ('T01_face_kit.png', (1024, 1024), 4, 4, ['eye_open', 'eye_half', 'eye_closed_happy', 'eye_closed_sleep', 'eye_heart', 'eye_star', 'eye_spiral', 'eye_wide',
                                              'mouth_smile', 'mouth_grin', 'mouth_laugh', 'mouth_o', 'mouth_tongue', 'mouth_frown', 'mouth_wavy', 'acc_sunglasses'], 'Batch 01 face kit', False),
    ('T01b_face_extras.png', (1024, 1024), 4, 4, ['brow_up', 'brow_angry', 'blush', 'tear', 'sweat_drop', 'snot_bubble', 'pupil', 'eye_glint'], 'Batch 01b face extras', False),
    ('T02_blocks.png', (1024, 1024), 3, 3, ['block_red', 'block_orange', 'block_yellow', 'block_green', 'block_blue', 'block_purple', 'block_pink', 'block_gray', 'block_ghost'], 'Batch 02 blocks (same size)', False),
    ('T03_merge_theme.png', (1024, 1024), 4, 4, [f'tier_{i:02d}' for i in range(1, 12)] + ['shop_icon'], 'Batch 03 one Merge theme (tiers same size, perfect circles)', False),
    ('T04_glass_jar.png', (1536, 1024), 2, 1, ['jar_back', 'jar_front'], 'Batch 04 glass jar (layers must overlap exactly)', True),
    ('T05a_splat_puff.png', (1024, 1024), 4, 4, [f'splat_{i}' for i in range(8)] + [f'puff_{i}' for i in range(8)], 'Batch 05a flipbooks (same center, same frame)', False),
    ('T05b_fx.png', (1024, 1024), 4, 4, ['flash_star', 'sparkle', 'glint_4pt', 'ring_shock', 'drop', 'shard', 'star_small', 'heart_small', 'confetti_rect', 'confetti_circle',
                                          'confetti_tri', 'confetti_curl', 'dust', 'light_rays', 'shadow_soft', 'glow_soft'], 'Batch 05b single fx', False),
    ('T06_buttons.png', (1536, 1024), 3, 4, ['btn_green', 'btn_green_pressed', 'btn_blue', 'btn_blue_pressed', 'btn_yellow', 'btn_yellow_pressed',
                                             'btn_white', 'btn_white_pressed', 'btn_red', 'btn_red_pressed', 'btn_gray', 'round_btn'], 'Batch 06 buttons (same size)', False),
    ('T07_frames.png', (1024, 1024), 4, 4, ['panel', 'panel_dark', 'card', 'ribbon', 'tab_on', 'tab_off', 'badge', 'chip', 'bar_bg', 'bar_fill', 'toggle_on', 'toggle_off',
                                            'tile_open', 'tile_current', 'tile_boss', 'tile_locked'], 'Batch 07 frames & controls', False),
    ('T08_icons.png', (1024, 1024), 6, 6, ['play', 'pause', 'home', 'settings', 'sound_on', 'sound_off', 'music_on', 'music_off', 'vibrate', 'language', 'trophy', 'leaderboard',
                                           'achievement', 'cloud_save', 'restore', 'privacy', 'lock', 'close', 'hint', 'restart', 'back', 'next', 'levels', 'star',
                                           'star_empty', 'heart', 'heart_empty', 'ad_video', 'no_ads', 'check', 'infinity', 'calendar', 'gift', 'shop', 'share', 'info'], 'Batch 08 icons (same size)', False),
    ('T09a_backgrounds.png', (1024, 1536), 1, 1, ['bg_<game> (one image per game)'], 'Batch 09a background', True),
    ('T09b_play_surfaces.png', (1536, 1024), 2, 1, ['blast_tray', 'arrow_card'], 'Batch 09b play surfaces', True),
]
for t in T:
    template(*t)

# Current placeholder look, so GPT keeps the characters' identity and palette (but improves quality).
S = os.path.join(HERE, '..', '..', 'Assets', '_Game', 'Art', 'Sheets')
def load(sheet):
    im = Image.open(os.path.join(S, sheet + '.png')).convert('RGBA'); js = json.load(open(os.path.join(S, sheet + '.json')))
    return {s['name']: im.crop((s['x'], s['y'], s['x'] + s['w'], s['y'] + s['h'])) for s in js['sprites']}
sp = {**load('shapes'), **load('ui'), **load('faces_a'), **load('faces_b')}
def tint(im, hexc):
    r, g, b = (int(hexc[i:i + 2], 16) for i in (1, 3, 5)); px = im.copy()
    px.putdata([(p[0] * r // 255, p[1] * g // 255, p[2] * b // 255, p[3]) for p in px.getdata()]); return px
ref = Image.new('RGBA', (1536, 1024), (43, 47, 85, 255))
items = [('circle', '#F15BB5', 'face_love_0'), ('block', '#3DDC97', 'face_happy_0'), ('circle', '#FFD23F', 'face_starstruck_0'), ('block', '#4EA8DE', 'face_cool_0'),
         ('circle', '#9B5DE5', 'face_laugh_0'), ('block', '#FF9F1C', 'face_silly_0')]
for i, (shape, col, face) in enumerate(items):
    x, y = 60 + (i % 3) * 300, 60 + (i // 3) * 300
    body = tint(sp[f'{shape}_fill'], col); body.alpha_composite(sp[f'{shape}_line']); body.alpha_composite(sp[face].resize((208, 208)), (24, 24))
    ref.alpha_composite(body, (x, y))
for j, n in enumerate(['btn_green', 'btn_blue', 'btn_yellow']):
    ref.alpha_composite(sp[n].resize((360, 168)), (1000, 80 + j * 190))
ref.alpha_composite(sp['panel'].resize((240, 240)), (1040, 690))
ref.save(os.path.join(OUT, 'REF_current_look.png'))
print(len(T) + 1, 'files ->', OUT)
