# Prompt batch cho ChatGPT

> **Không dùng nữa (2026-09-30).** Icon, logo, feature graphic và splash giờ vẽ bằng code theo đúng style game: `Tools/art/make-brand.mjs` (xuất ra `Tools/art/brand/out`, `--install` chép vào `Assets/_Game/Art/Brand` và `Store/graphics`). Ảnh screenshot cho store cũng sẽ ghép bằng code từ ảnh chụp thật.


Luật: **chỉ icon ứng dụng là vẽ mới.** Mọi ảnh khác là **chỉnh sửa (edit) ảnh chụp thật trong game**: giữ nguyên nội
dung game, chỉ thêm chữ. Không bling (sparkle, glow, confetti, tia sáng): hiệu ứng là việc của particle system.

Mở **1 chat mới cho mỗi game**. Dán khối **SETUP**, rồi dán từng khối việc bên dưới, mỗi lần kèm đúng file ảnh ghi trong
khối (kéo thả từ đường dẫn). Tải ảnh về, đặt tên như ghi, bỏ vào `C:\Projects\Casual Game\Store\gpt\`.

---

## SETUP (dán đầu mỗi chat)

```text
Talk to me in Vietnamese. You help me make Google Play store images for a casual mobile game by Bill The Dev.
I will send jobs one by one. There are two kinds of jobs:

1) GENERATE (only the app icon). Draw a new image following the job description.
2) EDIT (every other job). I attach a REAL in-game screenshot. You must EDIT it, not redraw it:
   - Keep the game screenshot content exactly as it is: same characters, faces, colors, board/jar, UI, hand
     pointer, positions. Do NOT redraw, restyle, re-color, add, move or remove anything inside the game image.
   - Only add what the job asks for (caption text on a background band, or extending the canvas).
   - Text: chunky rounded bold sans-serif, white with a thick dark navy (#1E2240) outline, spelled EXACTLY as given,
     max 2 lines, centered, easy to read on a phone. A " | " inside a caption means a line break (do not draw it).
   - No sparkles, glitter, glow, lens flare, light rays, confetti, speed lines, stickers, badges, arrows or extra
     characters. Flat and clean.
Before each image, repeat back in one Vietnamese line which file you got and what you will change.
After each image, wait for my APPROVE or my notes.
```

---

## Bruh Arrows

**B1 · Icon (GENERATE)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\02_level.png` và
`C:\Projects\Casual Game\Tools\art\preview\faces_preview.png`
```text
JOB: GENERATE the app icon for "Bruh Arrows" (calm puzzle: tap an arrow whose way out is clear and it slides off).
Use the attached screenshot for the arrow shape and colors (navy #2E3A59, teal #35B09F, purple #7B4FAE,
amber #EDA93C on cream #F4EFE8), and faces_preview.png for the face style (short brush strokes, deadpan, no blush).
Scene: cream background; one thick teal arrow pointing right with a SHOCK face on its round tail; behind it a navy
arrow with a MEH face (one eyebrow up) looking at it. Flat vector, thick navy outline, one hard cel shadow at most.
1024x1024, FULL BLEED (no rounded corners, no border, no outer shadow), all key shapes inside the central 80 % circle,
no text. File name: bruh-arrows-icon.png
```

**B2 · Screenshot 1 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\02_level.png`
```text
JOB: EDIT. Output 1080x1920. Put a flat cream (#F4EFE8) band across the top 300 px and shrink the attached
screenshot to fit the remaining area below it (keep its aspect, centered, no crop of the board).
Caption in the band: "Tap an arrow. | Watch it slide out." File name: bruh-arrows-shot-1.png
```

**B3 · Screenshot 2 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\03_tutorial.png`
```text
JOB: EDIT, same layout as the previous screenshot. Caption: "Learn it in one tap". File name: bruh-arrows-shot-2.png
```

**B4 · Screenshot 3 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\01_home.png`
```text
JOB: EDIT, same layout. Caption: "100 calm levels. | Plus a daily puzzle." File name: bruh-arrows-shot-3.png
```

**B5 · Feature graphic (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\bruh-arrows\02_level.png`
```text
JOB: EDIT. Output 1024x500. Canvas filled with flat cream #F4EFE8. Place the board area of the attached screenshot
(the arrows, not the top bar or buttons) on the right half, unchanged. On the left half write the title
"BRUH ARROWS" big, and under it smaller "Calm brain puzzle". Nothing important within 60 px of the edges.
File name: bruh-arrows-feature.png
```

---

## Nah Blocks

**N1 · Icon (GENERATE)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\nah-blocks\01_board.png` và
`C:\Projects\Casual Game\Tools\art\preview\faces_preview.png`
```text
JOB: GENERATE the app icon for "Nah Blocks" (8x8 block puzzle, every block is a rounded square with a deadpan face).
Use the screenshot for block shape and colors and faces_preview.png for the faces.
Scene: navy #2B2F55 background, a 2x2 group of blocks (yellow, pink, blue, green); three STARE, the yellow one in
front is SMUG; a small pink block falling in from the top with a PANIC face. Flat vector, thick navy outline.
1024x1024, FULL BLEED, key shapes inside the central 80 % circle, no text. File name: nah-blocks-icon.png
```

**N2 · Screenshot 1 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\nah-blocks\01_board.png`
```text
JOB: EDIT. Output 1080x1920. Flat navy (#2B2F55) band across the top 300 px, the attached screenshot shrunk to fit
below it (keep aspect, centered, no crop of the board or tray). Caption: "Fill a line. | Watch it pop."
File name: nah-blocks-shot-1.png
```

**N3 · Screenshot 2 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\nah-blocks\02_tutorial.png`
```text
JOB: EDIT, same layout. Caption: "Drag. Drop. Nah." File name: nah-blocks-shot-2.png
```

**N4 · Feature graphic (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\nah-blocks\01_board.png`
```text
JOB: EDIT. Output 1024x500, flat navy #2B2F55. The board of the attached screenshot on the right half, unchanged.
Left half: title "NAH BLOCKS" big, under it smaller "Line puzzle". Nothing important within 60 px of the edges.
File name: nah-blocks-feature.png
```

---

## Meh Merge

**M1 · Icon (GENERATE)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\meh-merge\01_jar.png` và
`C:\Projects\Casual Game\Tools\art\preview\faces_preview.png`
```text
JOB: GENERATE the app icon for "Meh Merge" (drop round deadpan balls into a glass jar, equal balls merge).
Use the screenshot for the jar: FLAT toon glass, navy outline, light tint, a few hard diagonal white bands, no
refraction. Faces from faces_preview.png.
Scene: navy-purple #2F2552 background, the jar holding 3 balls (big yellow SMUG in front, blue and green STARE),
a small pink ball dropping in from above with a SHOCK face. Flat vector, thick navy outline.
1024x1024, FULL BLEED, key shapes inside the central 80 % circle, no text. File name: meh-merge-icon.png
```

**M2 · Screenshot 1 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\meh-merge\01_jar.png`
```text
JOB: EDIT. Output 1080x1920. Flat #2F2552 band across the top 300 px, the attached screenshot shrunk to fit below it
(keep aspect, centered, no crop of the jar). Caption: "Two alike? | They merge." File name: meh-merge-shot-1.png
```

**M3 · Screenshot 2 (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\meh-merge\02_tutorial.png`
```text
JOB: EDIT, same layout. Caption: "Drag to aim. | Let go. Meh." File name: meh-merge-shot-2.png
```

**M4 · Feature graphic (EDIT)**: đính kèm `C:\Projects\Casual Game\Store\screenshots\v2\meh-merge\01_jar.png`
```text
JOB: EDIT. Output 1024x500, flat #2F2552. The jar of the attached screenshot on the right half, unchanged (it may
be cropped at the bottom of the jar only). Left half: title "MEH MERGE" big, under it smaller "Drop & merge".
Nothing important within 60 px of the edges. File name: meh-merge-feature.png
```

---

## Nếu ChatGPT vẽ lại thay vì edit

Nhắn: `You changed the game image. Keep the screenshot pixels exactly as attached, only add the band and the text. Redo.`
Nếu vẫn sai: bỏ qua, báo Claude. Claude ghép band + chữ bằng code từ đúng ảnh chụp (giữ nguyên từng pixel).
