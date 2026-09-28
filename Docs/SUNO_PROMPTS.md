# Prompt Suno — nhạc nền & âm thanh

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

### `music_arrow` — Arrow Out (game trí tuệ, cần tập trung)
```
calm lo-fi puzzle music, soft felt piano and warm rhodes, gentle brushed drums, mellow sub bass,
slow 78 bpm, cozy and focused, minimal melody, no big drops, seamless loop, study music, instrumental
```

### `music_blast` — Eye Blast (vui, nảy)
```
upbeat cute casual game music, bouncy marimba and pizzicato strings, light claps, playful bass,
120 bpm, cheerful and bright, simple catchy hook, seamless loop for mobile puzzle game, instrumental
```

### `music_merge` — Eye Merge (thư giãn, dễ thương)
```
relaxing kawaii game music, music box and ukulele, soft glockenspiel sparkles, light shaker,
96 bpm, dreamy and happy, gentle groove, seamless loop, cozy mobile game, instrumental
```

---

## SFX (tên file = tên trong game)

| File | Dùng khi | Prompt Suno (sound kit / đoạn ngắn) |
|---|---|---|
| `ui_click` | Bấm nút | `single soft UI click, short wooden tap, clean, no reverb` |
| `ui_open` / `ui_close` | Mở / đóng popup | `short airy whoosh up` / `short airy whoosh down` |
| `drop` | Thả viên (Merge) | `soft plop of a rubber ball dropping, cute, short` |
| `merge` | Hai viên hợp thể (game đổi cao độ theo cấp) | `bubbly pop with a tiny sparkle tail, cute, very short` |
| `big` | Mắt Thần / lên cấp | `magical chime fanfare, short, bright glockenspiel arpeggio up` |
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
