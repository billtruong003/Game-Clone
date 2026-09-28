# Brief gen asset bằng ChatGPT — lưới chuẩn

Asset hiện có trong project (mặt emoji, nút, FX vector…) chỉ là **placeholder để test logic**. Art thật lấy từ các đợt dưới đây.
Hướng chiều sâu & cách hiệu ứng chạy: `FEEL_SPEC.md`. Logic màn hình: `UI_LOGIC_REVIEW.md`.

## 1. Quy chuẩn lưới (áp dụng cho MỌI đợt)
| Quy tắc | Giá trị |
|---|---|
| Khổ ảnh | Chỉ dùng 3 khổ ChatGPT xuất được: **1024×1024**, **1536×1024** (ngang), **1024×1536** (dọc) |
| Nền | **PNG nền trong suốt thật** (alpha), không nền trắng, không ô caro giả |
| Lưới | Chia đều N cột × M hàng như từng đợt ghi. **Mỗi vật nằm giữa ô của nó**, không chạm/vượt mép ô |
| Lề an toàn | Vật chiếm tối đa **80%** ô (chừa 10% mỗi phía) — riêng ô "full-bleed" (nền, 9-slice) ghi rõ |
| Cùng cỡ | Khi đợt ghi "cùng cỡ": mọi vật cùng khung bao, cùng vị trí tâm (để thay nhau không bị nhảy) |
| Thứ tự | Đọc **trái → phải, trên → dưới**; tên file theo đúng thứ tự trong bảng |
| Hướng sáng | Ánh sáng từ **trên-trái 45°**: highlight trên-trái, tối dưới-phải, rim light mảnh cạnh phải |
| Viền | Navy `#1E2240`, dày ~4% kích thước vật, đều nét, bo tròn đầu nét |
| Shading | Cel-shading mềm 2–3 tông + highlight bóng; **được** dùng gradient mềm; **cấm** noise, grain, texture giấy, nét chì |
| Không có | chữ, số (trừ khi ghi), watermark, bàn tay, bóng đổ nền (bóng đổ là asset riêng) |
| Vật để tô màu (tint) | Vẽ **trắng + xám nhạt cho phần tối**, viền navy giữ nguyên → Unity nhân màu vẫn giữ khối |

**Khối STYLE — dán đầu mỗi cuộc chat:**
```
You are generating game art for 3 casual mobile games (block puzzle, merge-drop, arrow puzzle).
STYLE for every image: premium cartoon, smooth vector look, soft cel shading (2–3 tones + a glossy highlight),
light from the TOP-LEFT at 45°, thin rim light on the right edge, uniform dark navy outline (#1E2240) about 4% of the
object size with rounded line caps. Smooth gradients are OK; NO noise, grain, paper texture, pencil strokes or
photorealism. Palette: navy #1E2240, cream #FFF8EC, red #FF5A5F, orange #FF9F1C, yellow #FFD23F, green #3DDC97,
blue #4EA8DE, purple #9B5DE5, pink #F15BB5. No text, letters, numbers or watermark unless I ask.
GRID RULES: when I give a grid, split the canvas into exactly that many equal cells, put ONE object centered in each
cell, keep every object inside 80% of its cell, never cross cell borders, keep the reading order I list
(left→right, top→bottom), and keep "same size" items identical in bounding box and center.
Always export a PNG with a REAL transparent background (alpha), not white and not a checkerboard.
Reply "ok" and wait.
```

## 2. Cách nhập ảnh GPT vào project
```bash
python Tools/art/import-gpt.py <ảnh GPT>.png --grid 4x4 --batch <tên đợt> <tên1> <tên2> …
```
Script cắt đúng lưới (hoặc `--auto` theo vùng trong suốt), xóa nhiễu alpha, cắt lề, giữ độ phân giải gốc, lưu vào
`Tools/art/gpt/<đợt>/<tên>.png` và in bảng kiểm tra (số ô trống, vật chạm mép ô). Bước đóng sheet 2048/1024 (lũy thừa 2)
và nối vào game làm ở Phase A.

---

## 3. Các đợt gen (theo thứ tự)

### Đợt 00 · Style bible (duyệt trước, không nhập)
Khổ 1536×1024, lưới 3×2: `ball` (bi thạch màu hồng có mặt cười) · `block` (khối vuông xanh lá có mặt) · `button` (nút xanh lá kiểu viên thuốc, có đáy dày) · `panel_corner` (góc bảng kem) · `toon_splat` (vụ nổ khói kiểu cartoon splat) · `glass_jar` (hũ thủy tinh nhỏ).
> Mọi đợt sau đính kèm ảnh đợt 00 đã duyệt: "match this style exactly".

### Đợt 01 · Bộ phận mặt (face kit) — thay cho 14 emoji vẽ sẵn
Mặt sẽ được **ráp từ bộ phận** trong Unity (mắt, miệng, phụ kiện đổi riêng, nháy mắt/nhìn theo mượt) thay vì 12 khung vẽ tay: GPT vẽ bộ phận đẹp và đồng nhất hơn nhiều so với vẽ 168 khung hình.
Khổ 1024×1024, lưới **4×4**, "cùng cỡ" theo từng nhóm, nét navy, nền trong suốt:
| # | Tên | Mô tả |
|---|---|---|
| 1–4 | `eye_open` · `eye_half` · `eye_closed_happy` (^) · `eye_closed_sleep` (u) | mắt đơn (sẽ lật gương cho mắt kia) |
| 5–8 | `eye_heart` · `eye_star` · `eye_spiral` · `eye_wide` (mở to hoảng) | mắt đặc biệt |
| 9–12 | `mouth_smile` · `mouth_grin` (răng) · `mouth_laugh` (há, lưỡi) · `mouth_o` | miệng |
| 13–16 | `mouth_tongue` · `mouth_frown` · `mouth_wavy` · `acc_sunglasses` | miệng + kính râm |
Đợt 01b (4×4): `brow_up` · `brow_angry` · `blush` · `tear` · `sweat_drop` · `snot_bubble` · `pupil` · `eye_glint` · 8 ô trống.

### Đợt 02 · Khối Eye Blast (gạch men có bevel)
Khổ 1024×1024, lưới **3×3**, cùng cỡ, vuông bo góc, mặt trên bóng, cạnh dưới dày:
`block_red` · `block_orange` · `block_yellow` · `block_green` · `block_blue` · `block_purple` · `block_pink` · `block_gray` (khi thua) · `block_ghost` (trắng mờ 40%, để xem trước).
> Khối **không có mặt** — mặt ráp từ đợt 01.

### Đợt 03 · 10 theme Eye Merge (mỗi theme 1 ảnh)
Khổ 1024×1024, lưới **4×4**: ô 1–11 = 11 cấp **cùng cỡ trong ô** (game tự scale), silhouette **tròn hoàn hảo** (vật lý dùng hình tròn), ô 12 = icon cửa hàng, ô 13–16 trống.
| Theme | 11 cấp (nhỏ → lớn) | Có mặt? |
|---|---|---|
| `eyes` (mặc định) | chấm → mèo → dê → sao → tim → xoáy → vòng → lửa → hoa → cầu vồng → Mắt Thần | là con mắt |
| `emoji` | 11 thân tròn màu (mặt ráp từ đợt 01 theo biểu cảm từng cấp) | ráp |
| `fruit` | cherry, dâu, nho, quýt, hồng, táo, lê, đào, dứa, dưa lưới, dưa hấu | mặt nhỏ dễ thương |
| `billiards` | bi 1–11 (đặc/sọc, số trên vòng trắng — **được phép có số**) | không |
| `planets` | thiên thạch, Mặt Trăng, Sao Thủy, Sao Hỏa, Sao Kim, Trái Đất, Sao Hải Vương, Sao Thiên Vương, Sao Thổ, Sao Mộc, Mặt Trời | tùy chọn |
| `candy` | kẹo viên, kẹo mút, marshmallow, donut, macaron, cupcake, bánh bí, bánh cuộn, bánh tart, bánh kem, bánh tầng | mặt nhỏ |
| `pets` | hamster, gà con, thỏ, mèo, cún, cáo, gấu trúc, gấu, hổ, sư tử, cá voi (đầu tròn) | có |
| `sports` | bóng bàn, golf, tennis, bóng chày, cricket, bowling, bóng chuyền, bóng đá, bóng rổ, bóng bầu dục (tròn hóa), bóng bãi biển | không |
| `gems` | thạch anh, thạch anh tím, topaz, ngọc lục bảo, sapphire, ruby, opal, ngọc trai, hổ phách, kim cương xanh, kim cương | ánh lấp lánh |
| `neon` | 11 quả cầu neon phát sáng trên nền tối (vẽ glow mềm) | mặt neon |

### Đợt 04 · Hũ thủy tinh (Eye Merge)
Khổ 1536×1024, lưới **2×1**, full-bleed trong ô, cùng cỡ:
`jar_back` (lưng hũ: kính tối, gradient dọc, viền trong) · `jar_front` (mặt kính **gần như trong suốt**: chỉ vệt phản chiếu dọc bên trái, cạnh sáng, viền miệng dày).
> Hai lớp phải khớp khít khi chồng lên nhau. Thân hũ phải thẳng để cắt 9-slice (đoạn giữa cao ≥ 40% chiều cao).

### Đợt 05 · Hiệu ứng (trắng + xám nhạt, để tô màu)
05a — Khổ 1024×1024, lưới **4×4**, **flipbook** (cùng tâm, cùng khung):
- ô 1–8: `splat_0…7` — **toon splat**: khói cartoon phụt ra từ tâm, cạnh tròn phồng, có viền navy, tan dần ở khung 7–8.
- ô 9–16: `puff_0…7` — khói bụi nhỏ (đặt khối, mũi tên ra mép).
05b — Khổ 1024×1024, lưới **4×4**, vật đơn:
`flash_star` · `sparkle` · `glint_4pt` · `ring_shock` · `drop` · `shard` · `star_small` · `heart_small` · `confetti_rect` · `confetti_circle` · `confetti_tri` · `confetti_curl` · `dust` · `light_rays` · `shadow_soft` (elip đen mờ) · `glow_soft`.

### Đợt 06 · Nút (có độ dày, 2 trạng thái)
Khổ 1536×1024, lưới **3×4**, nút viên thuốc cùng cỡ, **đoạn giữa thẳng dài** (để 9-slice), không chữ:
hàng 1: `btn_green` · `btn_green_pressed` · `btn_blue` · hàng 2: `btn_blue_pressed` · `btn_yellow` · `btn_yellow_pressed` ·
hàng 3: `btn_white` · `btn_white_pressed` · `btn_red` · hàng 4: `btn_red_pressed` · `btn_gray` (tắt) · `round_btn` (nút tròn trắng).

### Đợt 07 · Khung & điều khiển
Khổ 1024×1024, lưới **4×4**:
`panel` (bảng kem, full-bleed 9-slice) · `panel_dark` · `card` (thẻ trắng nhỏ) · `ribbon` (dải băng tiêu đề) ·
`tab_on` · `tab_off` · `badge` (chấm đỏ thông báo) · `chip` ·
`bar_bg` · `bar_fill` · `toggle_on` · `toggle_off` ·
`tile_open` · `tile_current` · `tile_boss` · `tile_locked`.

### Đợt 08 · Icon (nét navy, một màu nhấn)
Khổ 1024×1024, lưới **6×6**, cùng cỡ:
play · pause · home · settings · sound_on · sound_off · **music_on · music_off** · vibrate · language · trophy · leaderboard ·
achievement · cloud_save · restore · privacy · lock · close · hint · restart · back · next · levels · star ·
star_empty · heart · heart_empty · ad_video · no_ads · check · infinity · calendar · gift · shop · share · info.

### Đợt 09 · Nền & mặt chơi
- `bg_merge`, `bg_blast`, `bg_arrow`: 1024×1536, **full-bleed**, mảng khối mềm tạo chiều sâu, **giữa màn yên tĩnh** (vùng chơi), không chi tiết nhỏ.
- 1536×1024 lưới 2×1: `blast_tray` (khay lõm 8×8, ô trống có bóng trong) · `arrow_card` (tấm thẻ giấy kem bo góc, bóng đổ mềm).

### Đợt 10 · Store (sau cùng)
Mỗi game: `icon_512` (1024×1024, có nền, không trong suốt) · `feature_1024x500` (1536×1024 rồi cắt). Không chữ trong icon.

---

## 4. Asset **không** gen bằng GPT
| Asset | Lý do |
|---|---|
| Mảnh mũi tên Arrow Out (đầu/thân/góc/đuôi) | Phải nối khít từng pixel giữa các ô; giữ vẽ vector bằng code, chỉnh màu/độ dày cho khớp style bible |
| Chữ (điểm, tiêu đề) | Dùng font Baloo 2 trong game để dịch được |
| Vạch nguy hiểm, lưới chấm | Hình học đơn giản, vẽ bằng code sạch hơn |
