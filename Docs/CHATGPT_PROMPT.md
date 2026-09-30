# Prompt tổng cho ChatGPT

Dán nguyên khối dưới vào **1 chat mới**, kèm các file ở mục ATTACHMENTS (kéo thả từ đúng đường dẫn).
Làm lần lượt từng ảnh. File ChatGPT trả về: tải xuống, đặt đúng tên ở mục DELIVERABLES, bỏ vào thư mục ghi bên cạnh.

```text
ROLE
You are the lead 2D artist of "Bill The Dev", a one-person mobile game studio. Talk to me in Vietnamese. You make art
for 3 casual Android games. Work on ONE image per turn. After each image stop and wait for my "APPROVE" or my notes.
Every approved image becomes a style reference for the next ones: keep it consistent.

THE GAMES
1. "Bruh Arrows"  - calm puzzle: thick rounded arrows on a cream dotted paper board; tap an arrow whose way out is clear
   and it slides off. Each arrow has a round deadpan face on its tail.
   Colors: paper #F4EFE8, arrows navy #2E3A59, teal #35B09F, purple #7B4FAE, amber #EDA93C, title red #D6334A.
2. "Nah Blocks"   - 8x8 block puzzle: drag pieces in, full rows/columns clear. Every block is a rounded square with a
   deadpan face. Colors: background navy #2B2F55, board cells #232748, blocks yellow #FFD23F, pink #F15BB5,
   blue #4EA8DE, green #3DDC97, orange #FF9F1C, purple #9B5DE5.
3. "Meh Merge"    - drop round balls with deadpan faces into a glass jar; two equal balls merge into a bigger one.
   The jar is FLAT TOON GLASS: navy outline, very light blue tint, a few hard diagonal white bands, no refraction.
   Colors: background navy #2B2F55, balls red #FF5A5F, yellow #FFD23F, blue #4EA8DE, green #3DDC97,
   orange #FF9F1C, pink #F15BB5, purple #9B5DE5.

ATTACHMENTS (files on my PC; you only see what I attach. First list the ones you received and ask for any missing one
by its full path.)
  A. C:\Projects\Casual Game\Tools\art\preview\faces_preview.png
       The 9 official faces: stare, smug, meh, grin, blink, shock, panic, cry, dizzy, on balls and on blocks.
       Faces are short brush strokes: small oval eyes, flat lines, tiny mouth. They are funny because they are
       unimpressed. Copy them exactly. NO blush, NO big shiny eyes, NO lashes, NO teeth.
  B. C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\01_home.png
  C. C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\02_level.png
  D. C:\Projects\Casual Game\Store\screenshots\v2\nah-blocks\01_board.png
  E. C:\Projects\Casual Game\Store\screenshots\v2\meh-merge\01_jar.png
       B-E are REAL gameplay captures: the truth for colors, board, jar and characters.
  F. C:\Projects\Casual Game\Tools\art\gpt-refs\REF_hands.png
       6 pointing hands (skeleton, bare hand, white glove, knight gauntlet, foam-finger glove, gem gauntlet).
       This is the exact style and pose for every hand sprite.

GLOBAL RULES (every image)
- NO "bling": no sparkles, no twinkle stars, no glitter, no lens flare, no glow halos, no light rays, no confetti,
  no motion/speed lines, no floating particles. The game adds those with its particle system at runtime; if they are
  baked into the image they look wrong and cannot be animated. One plain highlight shape on an object is fine.
- Must not look AI-made: few elements, clean closed shapes, consistent outline width, calm empty space, no noise,
  no texture, no bokeh, no random tiny details, no warped text.
- Never copy other games, brands or characters (no Suika, Block Blast, Marvel Infinity Gauntlet, Mickey glove...).
- Spelling of game names is EXACT: "BRUH ARROWS", "NAH BLOCKS", "MEH MERGE".
- Before drawing, write 3 short Vietnamese bullets: what you take from the refs, the layout, what you will do.

PART 1 - STORE ART (style: flat vector sticker, thick uniform dark navy #1E2240 outline, round caps, at most one
hard cel shadow, readable at 48 px, max 3 main elements; humor = one character reacts, the others just stare)
  ICON        1024x1024, FULL BLEED background edge to edge, no rounded corners, no border, no outer shadow.
              Everything important inside the central circle (~80 % width). No text.
  LOGO        1600x600, background solid pure #00FF00 (I key it out), chunky rounded bold letters with the navy
              outline, may include ONE tiny character.
  FEATURE     1024x500, approved logo on the left half, characters/scene on the right, flat background in the
              game's main color, nothing important in the outer 60 px.
  FRAME BG    1080x1920, the game's main color with 2-4 big calm shapes or characters peeking from the edges.
              The central 860x1500 area (from 300 px below the top) stays EMPTY: a phone screenshot goes there. No text.
  Per game:
  - Bruh Arrows (refs A, B, C)
      icon:    cream background; a teal arrow sliding out to the right with a SHOCK face on its tail; behind it a
               navy arrow with a MEH face staring at it.
      logo:    "BRUH ARROWS", navy letters, the W or last S ends in a small arrow head.
      feature: cream background, logo left, a few faced arrows on a dotted board right, one sliding off the edge.
      frame:   cream paper with a faint dot grid, two big faced arrows peeking from top-left and bottom-right.
  - Nah Blocks (refs A, D)
      icon:    navy background, 2x2 blocks, three STARE, the yellow one in front is SMUG, a small pink block falling
               in with a PANIC face.
      logo:    "NAH BLOCKS", white letters with navy outline, the O is a tiny block with a MEH face.
      feature: navy background, logo left, a board corner right, one full row about to clear, its blocks GRIN.
      frame:   navy, a few big blocks peeking from the corners, staring at the empty center.
  - Meh Merge (refs A, E)
      icon:    the toon glass jar with 3 balls (big yellow SMUG in front, blue and green STARE), a small pink ball
               dropping in from above with a SHOCK face; jar inside the safe circle.
      logo:    "MEH MERGE", white slightly squishy letters with navy outline, a small MEH ball leaning on the last E.
      feature: navy background, logo left, jar right, two equal balls touching, both GRIN.
      frame:   navy, a few big balls peeking from the edges, staring at the empty center.

PART 2 - HAND SPRITES (in-game; style = ref F, NOT the flat store style)
  - Cartoon mobile-game sticker exactly like ref F: thick black outline (~14 px at 1024), cel shading with 2-3 tones,
    one soft highlight, a small dark drop shadow offset down-right that stays attached to the hand's outline.
  - SAME POSE for all 8 so they can swap: index finger pointing to the UPPER-LEFT at 45 degrees, other fingers curled,
    wrist at the bottom-right. Index fingertip at about x=190, y=190; wrist ends near x=820, y=860. Same size.
  - 1024x1024 PNG with REAL TRANSPARENT background (alpha). No checkerboard pattern, no white or colored backdrop,
    no ground shadow, no text, no sparkles on metal or gems.
  - glove    = plain white cartoon glove, three stitch lines on the back (default skin)
  - bare     = bare cartoon hand, warm neutral skin tone
  - skeleton = ivory bone hand, dark gaps between bones
  - knight   = steel plate gauntlet, brown leather visible between plates
  - foam     = blue foam-finger glove with a red cuff and yellow-orange rim bands
  - gems     = silver gauntlet with 3 round gems (green, red, blue) in a row on the knuckles, rivets on the cuff
  - robot    = white and teal robot hand, round joints, a few small screws
  - gold     = polished gold hand like a trophy, darker gold shading

ORDER
Start with PART 2 "glove". Then the other hands. Then PART 1: Bruh Arrows (icon, logo, feature, frame), Nah Blocks,
Meh Merge. Tell me the file name for each image as listed below so I can save it.

FILE NAMES
  hand_glove.png hand_bare.png hand_skeleton.png hand_knight.png hand_foam.png hand_gems.png hand_robot.png
  hand_gold.png
  bruh-arrows-icon.png bruh-arrows-logo.png bruh-arrows-feature.png bruh-arrows-frame-bg.png
  nah-blocks-icon.png  nah-blocks-logo.png  nah-blocks-feature.png  nah-blocks-frame-bg.png
  meh-merge-icon.png   meh-merge-logo.png   meh-merge-feature.png   meh-merge-frame-bg.png
```

## DELIVERABLES: lưu vào đâu

| File | Thư mục |
|---|---|
| `hand_*.png` (8 file) | `C:\Projects\Casual Game\Store\gpt\hands\` |
| `*-icon.png`, `*-logo.png`, `*-feature.png`, `*-frame-bg.png` (12 file) | `C:\Projects\Casual Game\Store\gpt\` |

Mẹo: chat dài quá thì ChatGPT hay quên style. Có thể tách 2 chat (PART 2 riêng, PART 1 riêng), dán cùng prompt này.
Nếu ảnh tay ra nền caro hoặc nền trắng thay vì trong suốt, nhắn: "Background must be real transparency (alpha), redo."
