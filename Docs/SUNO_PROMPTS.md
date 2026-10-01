# Prompt Suno — nhạc nền & âm thanh

> **Không dùng nữa (2026-09-30).** Không dùng Suno. Nhạc nền và SFX lấy từ 2 pack đã import: `Assets/Casual Game Sounds U6` (SFX) và `Assets/Scenes/Season Cycle Casual Gaming Music Pack` (nhạc). Tên clip trong game (bảng SFX bên dưới, `music_arrow` / `music_blast` / `music_merge`) vẫn giữ nguyên.


Game đang dùng 17 SFX của Kenney (CC0) trong `Assets/_Game/Audio/Sfx`. Chúng đủ dùng cho bản prototype.
File này dành cho lúc bạn muốn thay bằng âm thanh riêng.

## Cách đưa vào game
1. Tạo bài trên Suno, tải file **MP3/WAV**.
2. Nhạc nền: bỏ vào `Assets/_Game/Audio/Music/`, **đặt đúng tên** trong bảng dưới (vd. `music_arrow.mp3`).
3. SFX: ghi đè file cùng tên trong `Assets/_Game/Audio/Sfx/` (vd. `merge.ogg` → `merge.wav` cũng được, chỉ cần **trùng tên file**, khác đuôi thì xóa file cũ).
4. Trong Unity: `Tools → Casual Game → Rebuild Audio Library`.

Quyền dùng: nhạc Suno chỉ được dùng thương mại khi tạo bằng **gói trả phí** (Pro/Premier) tại thời điểm tạo bài.
Bài tạo bằng gói Free thì không được dùng trong game có kiếm tiền.

Mẹo chung cho Suno:
- Bật **Instrumental** cho mọi bài (không lời).
- Nhạc nền: xin độ dài ~2 phút, rồi cắt đoạn lặp mượt trong Audacity (fade 0.3 s ở đầu/cuối).
- SFX: Suno làm SFX không ổn định. Nên tạo 1 bài "sound kit" dài 30–60 giây gồm nhiều âm ngắn, sau đó cắt từng âm ra.
  Nếu không ưng, dùng ElevenLabs Sound Effects hoặc tiếp tục dùng Kenney.

---

## Nhạc nền (3 bài)

Chất chung của 3 game: **deadpan** (mặt tỉnh bơ, hài kiểu "ờ, rồi sao"). Nhạc nên thư giãn, hơi lệch nhịp một chút cho
vui, không dễ thương kiểu kawaii, không hào hứng kiểu arcade. Tránh tiếng chuông lấp lánh dày (dễ thành "AI jingle").

### `music_arrow` — Bruh Arrows (trí tuệ, cần tập trung)
```
dry lo-fi puzzle music, soft felt piano and muted rhodes, lazy brushed drums, warm upright bass,
76 bpm, calm, slightly wonky and deadpan, sparse melody with small pauses, no drops, seamless loop, instrumental
```

### `music_blast` — Nah Blocks (vui, gọn gàng)
```
laid-back quirky puzzle groove, muted marimba and plucked nylon guitar, soft finger snaps, round synth bass,
104 bpm, cheerful but unimpressed, simple 4-bar hook, tidy and satisfying, seamless loop, instrumental
```

### `music_merge` — Meh Merge (thư giãn, lười biếng)
```
sleepy bossa lo-fi, soft electric piano, gentle ukulele strums, light shaker and rim clicks, warm bass,
90 bpm, relaxed and a little lazy, deadpan humor, soft bounce, seamless loop, instrumental
```

Tên file đặt đúng như trên (`music_arrow.mp3`, `music_blast.mp3`, `music_merge.mp3`) và bỏ vào `Assets/_Game/Audio/Music/`.
Game đã gọi sẵn 3 tên này; nhạc tự tạm dừng khi Pause.

---

## SFX (tên file = tên trong game)

| File | Dùng khi | Prompt Suno (sound kit / đoạn ngắn) |
|---|---|---|
| `ui_click` | Bấm nút | `single soft UI click, short wooden tap, clean, no reverb` |
| `ui_open` / `ui_close` | Mở / đóng popup | `short airy whoosh up` / `short airy whoosh down` |
| `drop` | Thả viên (Merge) | `soft plop of a rubber ball dropping, cute, short` |
| `merge` | Hai viên hợp thể (game đổi cao độ theo cấp) | `bubbly pop with a tiny sparkle tail, cute, very short` |
| `big` | Bóng tier 11 ("LEGENDARY MEH!") / lên cấp | `magical chime fanfare, short, bright glockenspiel arpeggio up` |
| `pick` / `place` | Nhặt / đặt khối (Blast) | `soft plastic pick up click` / `satisfying soft block thud` |
| `clear` | Xóa hàng (Blast) | `glassy sparkle sweep, satisfying line clear, short` |
| `fly` | Mũi tên bay ra (Arrow) | `quick soft whoosh with a light pluck, airy, short` |
| `blocked` | Chạm mũi tên bị chặn | `muted low bonk, gentle error, not harsh` |
| `hint` | Dùng gợi ý | `curious soft chime, two notes rising` |
| `star` | Sao hiện ở bảng thắng | `bright twinkle ding, single note` |
| `win` | Qua màn | `short happy jingle, marimba, 2 seconds, resolved ending` |
| `lose` | Thua | `short soft descending jingle, gentle and cute, not sad` |
| `thud`, `tick` | Dự phòng | `soft cushion impact` / `tiny clock tick` |

Nhạc nền được `GameAudio.PlayMusic("music_arrow")` phát nếu file tồn tại; không có file thì game im lặng, không lỗi.
