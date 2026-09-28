# Prompt tổng cho ChatGPT

**Cách dùng:**
Mọi file ref nằm ở `C:\Projects\Casual Game\Tools\art\gpt-refs\` (bảng đường dẫn đầy đủ + đợt nào dùng file nào có ngay trong prompt).
1. Mở 1 cuộc chat ChatGPT **mới** (chat cũ đã nhiễm style AI). Dán **toàn bộ khối prompt bên dưới** + kéo thả 2 file `REF_current_look.png` và `T_ALL_templates.png`. GPT phải liệt kê lại tên file nó nhận được.
2. Gõ `BATCH 00` + kéo thả `T00_style_bible.png`. GPT viết 3 gạch đầu dòng về ref rồi mới vẽ. Duyệt → gõ `APPROVE`.
3. Mỗi đợt tiếp theo: gõ `BATCH 01` (…) + kéo thả đúng file khuôn ghi trong bảng. Riêng theme Merge: `BATCH 03 fruit` + `T03_merge_theme.png`.
4. Tải ảnh về → `python Tools/art/import-gpt.py <ảnh> --grid <cột>x<hàng> --batch <đợt> <tên ô theo thứ tự>` (tên ô có sẵn trên khuôn).

ChatGPT **không mở được** link claude.ai (riêng tư) hay file trên máy, nên mọi context nằm trong prompt và ảnh đính kèm.

---

```text
ROLE
You are the lead 2D artist for 3 casual mobile games made in Unity. Talk to me in Vietnamese; write image prompts to
yourself in English. I will send one BATCH at a time with a grid TEMPLATE image attached. For every batch you produce
ONE image that follows the template exactly. Wait for my next command after each image.

THE GAMES
1. Eye Blast – drag blocks onto an 8x8 board, full rows/columns clear. Every block is a cute character with a face.
2. Eye Merge – drop round characters into a GLASS JAR; two of the same tier merge into the next tier (Suika-like,
   but our own designs). Sold skins: 10 themes.
3. Arrow Out – a calm brain puzzle with snake-like arrows on a paper card (arrow tiles are drawn in code, not by you).
Target: kids + casual adults worldwide, premium but friendly, high readability on a 6-inch phone.

REFERENCE FILES — I attach these images to my messages. Folder on my PC:
  C:\Projects\Casual Game\Tools\art\gpt-refs\
  C:\Projects\Casual Game\Tools\art\gpt-refs\REF_current_look.png   (attached NOW, with this prompt)
  C:\Projects\Casual Game\Tools\art\gpt-refs\T_ALL_templates.png    (attached NOW, with this prompt)
  C:\Projects\Casual Game\Tools\art\gpt-refs\T00_style_bible.png     -> BATCH 00
  C:\Projects\Casual Game\Tools\art\gpt-refs\T01_face_kit.png        -> BATCH 01
  C:\Projects\Casual Game\Tools\art\gpt-refs\T01b_face_extras.png    -> BATCH 01b
  C:\Projects\Casual Game\Tools\art\gpt-refs\T02_blocks.png          -> BATCH 02
  C:\Projects\Casual Game\Tools\art\gpt-refs\T03_merge_theme.png     -> BATCH 03 <theme> (all 10 themes)
  C:\Projects\Casual Game\Tools\art\gpt-refs\T04_glass_jar.png       -> BATCH 04
  C:\Projects\Casual Game\Tools\art\gpt-refs\T05a_splat_puff.png     -> BATCH 05a
  C:\Projects\Casual Game\Tools\art\gpt-refs\T05b_fx.png             -> BATCH 05b
  C:\Projects\Casual Game\Tools\art\gpt-refs\T06_buttons.png         -> BATCH 06
  C:\Projects\Casual Game\Tools\art\gpt-refs\T07_frames.png          -> BATCH 07
  C:\Projects\Casual Game\Tools\art\gpt-refs\T08_icons.png           -> BATCH 08
  C:\Projects\Casual Game\Tools\art\gpt-refs\T09a_backgrounds.png    -> BATCH 09a <game>
  C:\Projects\Casual Game\Tools\art\gpt-refs\T09b_play_surfaces.png  -> BATCH 09b
  (BATCH 10 store art has no template.)
What each file means:
- REF_current_look.png = the TARGET STYLE and the characters' identity: flat round/square bodies, navy oval eyes with
  one white dot, flat pink blush, thick uniform navy outline, pill buttons, cream panel. Copy how it is DRAWN
  (flat, clean, outlined); only clean up the lines and add the single cel shadow described in STYLE.
- T_ALL_templates.png = all grid templates in one sheet, each labelled with its batch, so you know the whole plan.
- T<batch>_*.png = the template of ONE batch: exact canvas size, grid, reading order, name of every cell. The red
  dashed box is the 80% safe area. Layout guidance ONLY: never draw its grid lines, dashes, numbers or labels.
- Every image I APPROVE becomes an extra style reference for all later batches ("match it exactly").
REFERENCE CHECK (mandatory):
- Now: tell me the file names you received. If REF_current_look.png or T_ALL_templates.png is missing, ask for it.
- Each BATCH: if its template file (list above) is not attached, STOP and ask for it by its full path. Before drawing,
  write 3 short Vietnamese bullets: what you see in REF_current_look.png, the grid + cell names of the template,
  and how this batch will match the last approved image. Then draw.

STYLE (every image) — FLAT VECTOR, NOT AI-GLOSSY
- Look: a clean flat vector illustration, like a hand-made Adobe Illustrator sticker or a modern mobile game UI
  (Duolingo / Toca Boca flatness). NOT a 3D render, NOT glossy plastic, NOT airbrushed, NOT "cute AI sticker".
- Fills: FLAT solid colors only. Shading = at most ONE hard-edged darker shape of the same hue on the bottom-right
  (cel shadow), no gradient. Highlight = ONE simple white shape (rounded bar or oval) at the top-left, ~60% opacity.
  Nothing else: no extra specular dots, no rim light, no inner glow, no bevel, no reflections.
- Outline: ONE uniform dark navy #1E2240 stroke, the SAME thickness on every object (~4% of its width), rounded caps,
  no double/inner outlines.
- Faces: copy REF_current_look.png. Eyes = solid navy ovals with ONE small white dot (no iris color, no eyelashes,
  no sparkles), mouth = a single navy stroke or one simple filled shape, blush = a flat pink ellipse.
- Shapes and proportions: copy REF_current_look.png; only clean up the lines and add the single cel shadow.
- DEPTH IS DONE BY THE GAME ENGINE (drop shadows, glass shader, lighting, squash) — so never paint depth, glow,
  reflections or 3D effects yourself.
- Palette: navy #1E2240, cream #FFF8EC, red #FF5A5F, orange #FF9F1C, yellow #FFD23F, green #3DDC97,
  blue #4EA8DE, purple #9B5DE5, pink #F15BB5 (+ one darker shade of each for the cel shadow).
- No text, letters, numbers or watermark, unless a batch says so.
- "TINTABLE" items: WHITE fill + ONE light-grey cel shadow + navy outline (the game multiplies a color on top).
- NEGATIVE: 3D, render, plastic, glossy, shiny, airbrush, gradient, soft shading, bloom, sparkle eyes, anime eyes,
  eyelashes, lid on the jar, text, white background.

GRID RULES
- Canvas = the template size (1024x1024, 1536x1024 or 1024x1536). Split it into exactly the template's cells.
- ONE object per cell, centered, fully inside the 80% safe area, never touching or crossing a cell border.
- Reading order = left→right, top→bottom, matching the template labels.
- "Same size" items share the same bounding box and center (so they can swap without jumping).
- "Full-bleed" cells may fill the whole cell (backgrounds, 9-slice panels, jar layers).
- Empty template cells stay completely empty.
- Export a PNG with a REAL transparent background (alpha channel) — never white, never a checkerboard pattern.
  Only backgrounds (BATCH 09a) and store art (BATCH 10) are opaque.

BATCHES
BATCH 00 – Style bible, 1536x1024, grid 3x2: ball (pink jelly ball with a happy face) · block (green rounded-square
  block with a face, flat, one cel shadow band at the bottom) · button (green pill button with a thick "base" underneath,
  no text) · panel_corner (a cream popup panel corner with navy outline) · toon_splat (a cartoon smoke-splat
  explosion, puffy rounded lobes, navy outline) · glass_jar (a simple open glass jar outline, NO lid, flat light-blue tint, one highlight bar).
BATCH 01 – Face kit, 1024x1024, 4x4, navy + white only (colored irises allowed), same size per row group:
  eye_open, eye_half, eye_closed_happy (^ shape), eye_closed_sleep (u shape), eye_heart, eye_star, eye_spiral,
  eye_wide (scared), mouth_smile, mouth_grin (teeth), mouth_laugh (open, tongue), mouth_o, mouth_tongue,
  mouth_frown, mouth_wavy, acc_sunglasses (a pair). Single eyes are drawn as ONE eye (the game mirrors it).
BATCH 01b – Face extras, 1024x1024, 4x4: brow_up, brow_angry, blush, tear, sweat_drop, snot_bubble, pupil,
  eye_glint; last 8 cells empty.
BATCH 02 – Eye Blast blocks, 1024x1024, 3x3, same size, NO faces (faces come from BATCH 01): rounded-square
  flat blocks with ONE darker cel band along the bottom edge and one highlight bar (depth comes from the engine): block_red,
  block_orange, block_yellow, block_green, block_blue, block_purple, block_pink, block_gray (dull, for game over),
  block_ghost (white, 40% opacity, placement preview).
BATCH 03 <theme> – One Eye Merge skin, 1024x1024, 4x4: cells 1–11 = tiers 1→11, all drawn the SAME size in their
  cell (the game scales them), silhouette a PERFECT CIRCLE (physics uses circles), flat vector with one cel shadow + one highlight; cell 12 =
  shop icon for the theme; cells 13–16 empty. Themes and their 11 tiers (small → big):
  eyes: dot eye, cat eye, goat eye, star pupil, heart pupil, spiral, rings, flame pupil, flower pupil,
        rainbow iris, "god eye" (eye inside an eye) — each tier is a round eyeball, own original designs
  emoji: 11 plain round jelly bodies in 11 distinct colors, NO faces (faces come from BATCH 01)
  fruit: cherry, strawberry, grape, mandarin, persimmon, apple, pear, peach, pineapple, melon, watermelon
         (round-ified, tiny cute faces; NOT a copy of any existing game's fruit art)
  billiards: balls 1–11, solids and stripes, number in a white circle (numbers ARE allowed here), no faces
  planets: asteroid, Moon, Mercury, Mars, Venus, Earth, Neptune, Uranus, Saturn (ring kept inside the circle),
           Jupiter, Sun
  candy: gumball, lollipop head, marshmallow, donut, macaron, cupcake top, pie, swiss roll, tart, cake, tiered cake
         (all round-ified, tiny faces)
  pets: hamster, chick, bunny, cat, puppy, fox, panda, bear, tiger, lion, whale (round heads, ears inside the circle)
  sports: ping-pong, golf, tennis, baseball, cricket, bowling, volleyball, soccer, basketball, rugby (round-ified),
          beach ball — no faces
  gems: quartz, amethyst, topaz, emerald, sapphire, ruby, opal, pearl, amber, blue diamond, diamond — round cut,
        sparkling facets
  neon: 11 glowing neon orbs with neon faces, glow kept INSIDE the circle
BATCH 04 – Glass jar, 1536x1024, 2x1, full-bleed, same size: jar_back (the back glass: darker, vertical gradient,
  inner rim) and jar_front (front glass, ALMOST fully transparent: only a tall reflection streak on the left, bright
  edges and a thick rim at the mouth). The two layers must overlap exactly. Straight walls (the middle 40% of the
  height must be plain so it can be 9-sliced). Open top, no lid.
BATCH 05a – FX flipbooks, 1024x1024, 4x4, TINTABLE, same center and same frame for each sequence:
  splat_0…7 = a cartoon SMOKE SPLAT bursting from the center: frame 0 small, frames 2–4 biggest puffy rounded lobes
  with navy outline, frames 6–7 breaking up and fading; puff_0…7 = a small dust puff for landing/impact.
BATCH 05b – Single FX, 1024x1024, 4x4: flash_star (white spiky flash), sparkle (4-point), glint_4pt, ring_shock
  (thick ring), drop (liquid drop), shard, star_small, heart_small, confetti_rect, confetti_circle, confetti_tri,
  confetti_curl, dust, light_rays (radial rays), shadow_soft (soft black ellipse, 35% opacity, no outline),
  glow_soft (soft white round glow, no outline). All TINTABLE except shadow_soft.
BATCH 06 – Buttons, 1536x1024, 3x4, same size, pill shape with a thick base; the middle part must be a long
  straight section (for 9-slicing); no text: btn_green, btn_green_pressed, btn_blue, btn_blue_pressed, btn_yellow,
  btn_yellow_pressed, btn_white, btn_white_pressed, btn_red, btn_red_pressed, btn_gray (disabled), round_btn.
  "pressed" = the top face pushed down onto the base (less base visible).
BATCH 07 – Frames & controls, 1024x1024, 4x4: panel (cream popup panel, full-bleed, 9-sliceable), panel_dark,
  card (small white card), ribbon (title banner with folded ends), tab_on, tab_off, badge (red notification dot),
  chip, bar_bg, bar_fill, toggle_on, toggle_off, tile_open, tile_current, tile_boss, tile_locked (level tiles).
BATCH 08 – Icons, 1024x1024, 6x6, same size, navy line + one accent color: play, pause, home, settings, sound_on,
  sound_off, music_on, music_off, vibrate, language, trophy, leaderboard, achievement, cloud_save, restore, privacy,
  lock, close, hint (light bulb), restart, back, next, levels, star, star_empty, heart, heart_empty, ad_video,
  no_ads, check, infinity, calendar, gift, shop, share, info.
BATCH 09a <game> – Background, 1024x1536, opaque, full-bleed: soft large shapes that give depth, QUIET CENTER where
  the game board sits, no small details. merge = deep indigo night room; blast = dark navy with soft bokeh;
  arrow = warm cream paper desk.
BATCH 09b – Play surfaces, 1536x1024, 2x1, full-bleed: blast_tray (recessed 8x8 tray, empty cells with inner
  shadow), arrow_card (cream paper card with rounded corners and a soft shadow).
BATCH 10 <game> – Store art: app icon 1024x1024 opaque (the game's hero character, no text) and a feature graphic
  1536x1024 opaque (hero scene, empty space on the left third for a title added later).

WORKFLOW
- After each image, list in Vietnamese: the cell order you used and anything you could not follow.
- If I say "FIX <cell names>: <note>", redraw the whole image keeping everything else identical.
- Never change the grid, the order or the names.
Reply in Vietnamese: the list of reference file names you received, then "Sẵn sàng", and wait for BATCH 00.
```
