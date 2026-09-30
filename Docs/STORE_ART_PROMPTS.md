# Việc cho ChatGPT (v2): icon, logo, feature graphic, khung screenshot

> **Cũ.** Bản đang dùng: **Docs/CHATGPT_PROMPT.md** (chỉ icon là vẽ mới; screenshot + feature graphic là edit ảnh chụp thật). Bàn tay: đã chốt `Tools/art/src/hand_glove.png`, bỏ bộ skin tay.

ChatGPT làm art cho trang Google Play **và bàn tay trong game** (mục cuối). Mọi asset trong game khác (nhân vật, skin, UI, FX) Claude vẽ
bằng `Tools/art`.

## Checklist

| # | Việc | File nộp (bỏ vào `C:\Projects\Casual Game\Store\gpt\`) | Kích thước |
|---|---|---|---|
| 1 | Icon Bruh Arrows | `bruh-arrows-icon.png` | 1024×1024, full bleed |
| 2 | Icon Nah Blocks | `nah-blocks-icon.png` | 1024×1024, full bleed |
| 3 | Icon Meh Merge | `meh-merge-icon.png` | 1024×1024, full bleed |
| 4 | Logo chữ ×3 | `bruh-arrows-logo.png`, `nah-blocks-logo.png`, `meh-merge-logo.png` | 1600×600, nền #00FF00 |
| 5 | Feature graphic ×3 | `bruh-arrows-feature.png`, `nah-blocks-feature.png`, `meh-merge-feature.png` | 1024×500 |
| 6 | Nền khung screenshot ×3 | `bruh-arrows-frame-bg.png`, `nah-blocks-frame-bg.png`, `meh-merge-frame-bg.png` | 1080×1920, không chữ |

Việc 6 là nền trang trí phía sau ảnh chụp game. Claude tự ghép ảnh chụp thật + caption lên nền này (caption không nhờ
ChatGPT viết vào ảnh, vì hay sai chính tả).

**Cách làm**
- Mỗi game **1 chat mới**. Dán khối **SETUP**, rồi khối của game đó, kéo thả các file ref ghi trong khối.
- Thứ tự: ICON → LOGO → FEATURE → FRAME BG. Mỗi ảnh: `APPROVE` hoặc góp ý. Ảnh đã duyệt làm ref cho ảnh sau.
- Xong thì báo Claude: Claude cắt, resize (512 icon, 1024×500 feature), gắn vào profile và ghép screenshot.
- Ảnh cũ `Store\gpt\*-batch01.png` là tên cũ (Eye …), bỏ.

**File ref** (trên máy):

| File | Là gì |
|---|---|
| `Tools\art\preview\faces_preview.png` | **9 mặt deadpan** (stare, smug, meh, grin, blink, shock, panic, cry, dizzy) trên bóng tròn và khối vuông. Đây là bản gốc của nhân vật. |
| `Store\screenshots\v2\bruh-arrows\01_home.png`, `02_level.png` | Ảnh chụp thật Bruh Arrows |
| `Store\screenshots\v2\nah-blocks\01_board.png` | Ảnh chụp thật Nah Blocks |
| `Store\screenshots\v2\meh-merge\01_jar.png` | Ảnh chụp thật Meh Merge (hũ kính toon) |

---

## SETUP (dán đầu mọi chat)

```text
ROLE
You are the lead artist for a tiny mobile game studio ("Bill The Dev"). Talk to me in Vietnamese. In this chat you
make Google Play store art for ONE game, one image at a time: APP ICON -> TITLE LOGO -> FEATURE GRAPHIC ->
SCREENSHOT FRAME BACKGROUND. After each image wait for my APPROVE or notes. Every approved image is a style reference
for the next one: match it exactly.

REFERENCE FILES I ATTACH
  faces_preview.png = the 9 official character faces. Characters are flat round balls or rounded squares with a thick
      dark navy outline, one small white highlight top-left, and a DEADPAN face drawn with short brush strokes:
      stare, smug, meh (one eyebrow up), grin, blink, shock, panic, cry, dizzy. Copy these faces exactly. They are
      funny because they are unimpressed. NO blush, NO big shiny anime eyes, NO eyelashes, NO teeth, NO extra details.
  *_screenshot / 0x_*.png = REAL gameplay. This is the truth about colors, board / jar and characters.
First list the file names you received; if one is missing ask for it. Before each image write 3 short Vietnamese
bullets: what you take from the refs, the layout, what you will do. Then draw.

STYLE (every image)
- NO bling: no sparkles, glitter, glow, lens flare, light rays, confetti, speed lines or particles (the game adds them with particles).
- Flat vector, like a hand-made Illustrator sticker. NOT glossy 3D, no plastic shine, no soft gradients (one hard cel
  shadow at most), no noise, bokeh, lens flare, sparkles everywhere or realistic light. It must NOT look AI-made:
  few elements, clean shapes, consistent line width, lots of calm space.
- Thick uniform dark navy outline (#1E2240), round caps.
- Big and readable at 48 px. Max 3 main elements. Strong silhouette.
- Humor = deadpan: one character reacts (shock / panic / smug) while the others just stare.

OUTPUT RULES
- APP ICON: 1024x1024, FULL BLEED background edge to edge, NO rounded corners, NO border, NO shadow around the icon.
  Everything important inside the central circle (~80 % width). NO text, letters or numbers.
- TITLE LOGO: 1600x600, flat pure #00FF00 background (I key it out), chunky rounded bold letters with the navy outline,
  may include ONE tiny character. Spelling EXACT.
- FEATURE GRAPHIC: 1024x500, approved logo on the left half, characters / scene on the right half, flat background in
  the game's main color. Nothing important in the outer 60 px.
- FRAME BACKGROUND: 1080x1920, the game's main color with 2-4 big calm flat shapes or characters peeking from the
  edges. Keep the central area 860x1500 (starting 300 px from the top) EMPTY and quiet: a phone screenshot goes there.
  NO text.
- Never copy existing games (no Suika, no Block Blast, no "Arrows" apps), no brand names.
```

---

## Bruh Arrows
Đính kèm: `faces_preview.png`, `01_home.png`, `02_level.png`.

```text
GAME: "Bruh Arrows" - a calm brain puzzle. Thick rounded arrows lie on a cream paper board with small grey dots. Tap an
arrow whose path to the edge is clear and it slides out. Each arrow has a round deadpan face on its tail (see
faces_preview.png and the screenshots). Mood: calm, clever, a little sarcastic.
Colors: cream paper #F4EFE8 background, arrows navy #2E3A59, teal #35B09F, purple #7B4FAE, amber #EDA93C,
title red #D6334A.

1) APP ICON: cream background, one bold teal arrow shooting out to the right with a SHOCK face on its tail and 2 short
   speed lines; behind it one navy arrow with a MEH face (one eyebrow up) staring at it.
2) TITLE LOGO: "BRUH ARROWS" (exact), navy letters, the last S or the W turns into a small arrow head.
3) FEATURE GRAPHIC: cream background, logo left, a few faced arrows on a dotted board right, one flying out.
4) FRAME BACKGROUND: cream paper with a faint dot grid, two big faced arrows peeking from the top-left and bottom-right.
```

## Nah Blocks
Đính kèm: `faces_preview.png`, `01_board.png`.

```text
GAME: "Nah Blocks" - block puzzle on an 8x8 board: drag pieces in, full rows and columns clear. Every block is a rounded
square character with a deadpan face. Mood: bright, satisfying, unbothered.
Colors: navy #2B2F55 background, dark board cells #232748, blocks yellow #FFD23F, pink #F15BB5, blue #4EA8DE,
green #3DDC97, orange #FF9F1C, purple #9B5DE5.

1) APP ICON: navy background, a 2x2 group of blocks; three just STARE, the yellow one in front is SMUG. One small pink
   block falling in with a PANIC face.
2) TITLE LOGO: "NAH BLOCKS" (exact), chunky white letters with navy outline, the O is a tiny block with a MEH face.
3) FEATURE GRAPHIC: navy background, logo left, a board corner right with one row clearing (flat white flash),
   the clearing blocks GRIN.
4) FRAME BACKGROUND: navy with a few big blocks peeking from the corners, all staring at the empty center.
```

## Meh Merge
Đính kèm: `faces_preview.png`, `01_jar.png`.

```text
GAME: "Meh Merge" - drop round characters into a glass jar; two of the same size merge into a bigger one. Balls have
deadpan faces. The jar is FLAT TOON GLASS exactly like the screenshot: navy outline, very light blue tint, a few hard
diagonal white stripe bands, NO refraction, NO soft reflections.
Colors: navy #2B2F55 background, balls red #FF5A5F, yellow #FFD23F, blue #4EA8DE, green #3DDC97, orange #FF9F1C,
pink #F15BB5, purple #9B5DE5.

1) APP ICON: the toon glass jar with 3 balls inside (big yellow SMUG in front, blue and green STARE), a small pink ball
   dropping in from above with a SHOCK face. Jar inside the safe circle.
2) TITLE LOGO: "MEH MERGE" (exact), chunky white letters with navy outline, slightly squishy; one small ball with a
   MEH face leaning on the last E.
3) FEATURE GRAPHIC: navy background, logo left, jar right, two same balls touching with a flat pop, both GRIN.
4) FRAME BACKGROUND: navy with a few big balls peeking from the edges, staring at the empty center.
```

---

## Bàn tay (asset TRONG game, ngoại lệ duy nhất)

Bàn tay chỉ hướng dẫn và các skin tay trong Shop **không vẽ bằng code**: ChatGPT làm, **PNG nền trong suốt**.
Ref: ảnh 6 bàn tay của bạn (bộ xương, tay trần, găng trắng, găng hiệp sĩ, găng tay cổ động, găng đá quý), lưu thành
`Tools\art\gpt-refs\REF_hands.png` rồi đính kèm.

| # | Skin | File nộp (`Store\gpt\hands\`) | Ghi chú |
|---|---|---|---|
| H1 | Găng trắng (mặc định, miễn phí) | `hand_glove.png` | dùng cho hướng dẫn |
| H2 | Tay trần | `hand_bare.png` | |
| H3 | Bộ xương | `hand_skeleton.png` | |
| H4 | Găng hiệp sĩ | `hand_knight.png` | |
| H5 | Găng cổ động | `hand_foam.png` | xanh + băng cổ tay đỏ vàng |
| H6 | Găng đá quý | `hand_gems.png` | **không** giống Infinity Gauntlet của Marvel |

Một chat riêng, dán khối dưới, đính kèm `REF_hands.png`, mỗi lượt 1 bàn tay:

```text
ROLE
You draw game UI sprites. Talk to me in Vietnamese. I attach REF_hands.png: 6 pointing hands in one style. Make ONE
hand per image in EXACTLY that style, and every hand must share the same pose so they can swap in the game.

STYLE (copy the reference)
- Cartoon mobile-game sticker: thick black outline (~14 px at 1024), cel shading with 2-3 tones, one soft white
  highlight, small dark drop shadow offset down-right inside the outline shape.
- Same pose for all: index finger pointing to the UPPER-LEFT at 45 degrees, other fingers curled, wrist at the
  bottom-right. The TIP of the index finger sits at about x=190, y=190 of the canvas; the wrist ends near x=820,
  y=860. Same size and angle every time.

OUTPUT
- 1024x1024 PNG with a TRANSPARENT background (real alpha, no checkerboard, no white or colored backdrop, no floor
  shadow outside the hand). One hand only, no text, no sparkles.
- No logos or characters from other brands (no Marvel Infinity Gauntlet, no Mickey glove).

Hand to draw now: <paste one line>
  glove    = plain white cartoon glove, three stitch lines on the back
  bare     = bare cartoon hand, warm neutral skin
  skeleton = bone hand, ivory bones, dark gaps between bones
  knight   = steel plate gauntlet, brown leather under the plates
  foam     = blue foam finger glove with a red cuff and yellow-orange rim bands
  gems     = silver gauntlet with 3 round gems (green, red, blue) set in a row along the knuckles, rivets on the cuff
```

Nộp xong báo Claude: Claude cắt, thu về 256 px, nén ASTC, nối vào hướng dẫn và Shop.
