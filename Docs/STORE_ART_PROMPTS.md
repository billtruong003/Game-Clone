# Prompt ChatGPT cho icon, logo tên game, feature graphic

**Cách dùng**
- Mỗi game mở **1 chat ChatGPT mới**. Dán **khối SETUP** + khối của game đó, kéo thả đúng các file ref ghi trong khối.
- ChatGPT không mở được file trên máy hay link claude.ai. Mọi context nằm trong prompt và ảnh đính kèm.
- Làm theo thứ tự: **ICON → LOGO → FEATURE**. Duyệt từng ảnh (`APPROVE` hoặc góp ý), ảnh đã duyệt làm ref cho ảnh sau.
- Tải ảnh về, đặt đúng tên, bỏ vào `C:\Projects\Casual Game\Store\gpt\` rồi báo t. T sẽ cắt, resize và gắn vào profile:
  - `arrow-out-icon.png`, `eye-blast-icon.png`, `eye-merge-icon.png`
  - `arrow-out-logo.png`, `eye-blast-logo.png`, `eye-merge-logo.png`
  - `arrow-out-feature.png`, `eye-blast-feature.png`, `eye-merge-feature.png`

**File ref** (có sẵn trên máy):

| File | Dùng cho |
|---|---|
| `C:\Projects\Casual Game\Store\screenshots\<game>\*.png` | **Ảnh chụp gameplay thật (1080×1920, có FX)**: nguồn chính. Danh sách: `Store\screenshots\README.md` |
| `C:\Projects\Casual Game\Tools\art\gpt-refs\REF_current_look.png` | Style nhân vật / nét / màu hiện tại (mọi game) |
| `C:\Projects\Casual Game\Store\graphics\arrow-out-icon-512.png` | Bố cục icon tạm của Arrow Out |
| `C:\Projects\Casual Game\Store\graphics\eye-blast-icon-512.png` | Bố cục icon tạm của Eye Blast |
| `C:\Projects\Casual Game\Store\graphics\eye-merge-icon-512.png` | Bố cục icon tạm của Eye Merge |
| `C:\Projects\Casual Game\Store\graphics\<game>-feature.png` | Bố cục feature graphic tạm |

---

## SETUP (dán đầu mọi chat)

```text
ROLE
You are the lead artist for a small mobile game studio. Talk to me in Vietnamese. You will make store art for ONE game
in this chat, one image at a time, in this order: APP ICON -> TITLE LOGO -> FEATURE GRAPHIC. After each image, wait
for my APPROVE or my notes. Every approved image becomes a style reference for the next one: match it exactly.

REFERENCE FILES I ATTACH (folder on my PC, you only see what I attach):
  C:\Projects\Casual Game\Store\screenshots\<game>\*.png           = REAL gameplay screenshots with the real FX. This is
      the truth about what the game looks and plays like: use its characters, board / jar, colors and FX moments.
      Store\screenshots\README.md says what each screenshot shows.
  C:\Projects\Casual Game\Tools\art\gpt-refs\REF_current_look.png  = the in-game style and character identity
  C:\Projects\Casual Game\Store\graphics\<game>-icon-512.png         = a rough layout idea only (drawn in code, NOT the real
      look). Improve freely; the screenshots win when they disagree.
First tell me the file names you received. If one is missing, ask for it by its full path. Before each image, write 3
short Vietnamese bullets: what you take from the refs, the layout, and what you will improve. Then draw.

STYLE (every image)
- Flat vector look, like a hand-made Illustrator sticker / premium casual mobile game. NOT glossy AI 3D, no plastic
  shine, no gradients except one soft cel shadow, no noise, no bokeh, no lens flare, no realistic lighting.
- Thick, uniform dark navy outline (#1E2240) on every character and object, round line caps.
- Characters = simple shapes with cute faces exactly like REF_current_look.png: navy oval eyes with one white dot,
  simple curved mouth, flat pink blush. Keep faces simple (emoji-like), never realistic, never extra details.
- Palette: navy #2B2F55, cream #F5F1EA, paper #FFF8EC, yellow #FFD23F, orange #FF9F1C, green #3DDC97,
  blue #4EA8DE, pink #F15BB5, red #FF5A5F, purple #9B5DE5, teal #2A9D8F, amber #E9A23B, slate #2E3A59.
- Big, bold, readable at 48 px. Max 3 main elements. Strong silhouette and contrast.

OUTPUT RULES
- APP ICON: exactly square 1024x1024, FULL BLEED background (edge to edge, NO rounded corners, NO border, NO drop
  shadow around the icon). Google Play crops it to a rounded shape: keep every important element inside the central
  circle of about 80% width. NO text, NO letters, NO numbers, NO logos of other games.
- TITLE LOGO: the game name as a wordmark, 1600x600, on a plain flat background of ONE solid color I can key out
  (pure #00FF00), chunky rounded bold letters with the navy outline, can include one tiny character or icon element.
  Spelling must be EXACT.
- FEATURE GRAPHIC: exactly 1024x500, the approved title logo on the left half, the icon's characters / scene on the
  right half, flat background in the game's main color. Keep all important content away from the outer 60 px.
- Never copy the style, characters or names of existing games (no Suika, no Block Blast, no Arrows).
```

---

## Arrow Out
Đính kèm: `REF_current_look.png`, `arrow-out-icon-512.png`, `arrow-out-feature.png`.

```text
GAME: "Arrow Out" - a calm brain puzzle. Snake-like arrows lie on a dotted paper card; tap an arrow whose path to the
board edge is clear and it slides out. Mood: calm, clever, satisfying. Main colors: cream paper #F5F1EA background,
arrows in teal #2A9D8F, slate #2E3A59, amber #E9A23B, plum #8E5BB5. Arrows are thick rounded strokes with a round tail
dot and a chevron head, drawn with the navy outline. No faces in this game.

1) APP ICON: one bold teal L-shaped arrow sliding out of a cream dotted card to the right, a second slate arrow still on
   the card, 2-3 short speed lines. Clean, geometric, satisfying.
2) TITLE LOGO: "ARROW OUT" (exact), letters in navy with cream inner highlight; the O of OUT can be a small arrow
   circle or an arrow can shoot out of the last T.
3) FEATURE GRAPHIC: cream background, logo left, a few arrows on a dotted card on the right with one flying out.
```

## Eye Blast
Đính kèm: `REF_current_look.png`, `eye-blast-icon-512.png`, `eye-blast-feature.png`.

```text
GAME: "Eye Blast" - a block puzzle on an 8x8 board: drag pieces onto the board, full rows and columns clear. Every block
is a cute square character with a face (see REF_current_look.png). Mood: bright, cheerful, satisfying pops.
Main colors: navy #2B2F55 background, dark board cells #232748, blocks in yellow, pink, blue, green (palette above).

1) APP ICON: a few rounded-square blocks with happy faces on a dark board, one big yellow laughing block in front,
   one pink surprised block. Strong contrast against the navy.
2) TITLE LOGO: "EYE BLAST" (exact), chunky white letters with the navy outline; the two E's or the O-like shapes may
   carry tiny eyes. A small burst behind BLAST.
3) FEATURE GRAPHIC: navy background, logo left, a board corner with faced blocks on the right, one line clearing with
   a flat star burst.
```

## Eye Merge
Đính kèm: `REF_current_look.png`, `eye-merge-icon-512.png`, `eye-merge-feature.png`.

```text
GAME: "Eye Merge" - drop round characters into a glass jar; two of the same size merge into a bigger one. Characters
are round balls with cute faces (see REF_current_look.png). The jar is FLAT TOON GLASS: a dark navy outline, a very
light tint inside, one hard-edged white highlight stripe, NO refraction, NO soft reflections.
Main colors: navy #2B2F55 background, balls in yellow, orange, green, pink.

1) APP ICON: a glass jar holding 3 balls (big yellow laughing ball in front, orange and green behind), a small pink
   ball dropping in from above the rim. Keep the jar inside the safe circle.
2) TITLE LOGO: "EYE MERGE" (exact), chunky white letters with navy outline; the letters can look a little squishy
   like goo, one small ball character leaning on the logo.
3) FEATURE GRAPHIC: navy background, logo left, the jar with balls on the right, two same balls merging with a flat pop.
```

---

## (Tuỳ chọn) Brainstorm tên game
Nếu muốn đổi tên trước khi lên store (tên có thể đổi sau, package name thì không):

```text
Suggest 10 English names for a casual mobile game: <mô tả game>. Rules: max 20 characters, easy to say, no existing
big game names or trademarks, works for kids and adults worldwide. For each name give a 1-line reason and one possible
short subtitle (max 30 characters incl. the name, for the Google Play title). Then pick your top 3.
```
