# Feel spec — chiều sâu & hiệu ứng

Mục tiêu: hết "phẳng". Mọi vật có **khối** (shading + bóng đổ), vật liệu có **chất** (thủy tinh, thạch, giấy),
mỗi va chạm có **âm thanh đúng chất liệu**, mỗi khoảnh khắc lớn có **nhịp** (lấy đà → nổ → dư âm).
Hình ảnh của hiệu ứng lấy từ asset ChatGPT (`GPT_ASSET_BRIEF.md`); file này chỉ quy định **cách chúng chạy**.
Mọi con số là giá trị khởi điểm để tune trong Unity (đặt trong ScriptableObject `FeelConfig`, chỉnh không cần build lại).

---

## 1. Chiều sâu chung (cả 3 game)

| Lớp | Cách làm | Asset |
|---|---|---|
| **Shading thân** | Thân nhân vật/bi/khối vẽ sẵn 2 tông sáng-tối + highlight + rim light (GPT), không còn màu phẳng tint | Brief đợt 02–03 |
| **Bóng đổ tiếp xúc** | Sprite bóng mềm (elip) dưới mỗi vật, lệch xuống-phải theo hướng sáng trên-trái, alpha 25–35%. Vật càng cao so với mặt đỡ → bóng càng nhỏ và nhạt | `shadow_soft` (đợt 05) |
| **Bóng khi nhấc lên** | Vật đang kéo/đang cầm: bóng tách ra xa 18–24px, mờ hơn → cảm giác vật nhấc khỏi mặt bàn | dùng lại `shadow_soft` |
| **Nền có lớp** | Nền 2–3 lớp: mảng màu xa (mờ, trôi chậm) · mặt bàn/khay gần · vật chơi. Parallax nhẹ theo con quay hồi chuyển (tùy chọn, tắt được) | Brief đợt 09 |
| **UI có độ dày** | Nút có "đáy" dày 6–8px; nhấn xuống thì mặt nút lún bằng đáy (đổi sprite pressed), thả ra nảy lên | Brief đợt 06 |
| **Bloom nhẹ** | URP Bloom threshold cao, chỉ ăn vào lấp lánh/flash/cấp 11. Tắt được trong Cài đặt (máy yếu) | — |

---

## 2. Eye Merge — hũ thủy tinh, bi thạch

### 2.1 Hũ thủy tinh (shader + 3 lớp)
Vẽ theo thứ tự sau lưng → trước mặt:
1. **Bóng hũ** trên mặt bàn (elip tối mờ dưới đáy hũ).
2. **Lưng hũ** (`jar_back`): thủy tinh phía sau, tối hơn, có gradient dọc và viền trong.
3. **Bi** (và bóng đổ của bi lên đáy hũ).
4. **Mặt hũ** (`jar_front`): lớp kính trước **gần như trong suốt**, chỉ có vệt phản chiếu, viền miệng hũ, cạnh sáng.
5. Shader `GlassFront` (Shader Graph, sprite unlit):
   - `_Rim`: cạnh hũ sáng hơn ở mép trái/phải (fresnel giả theo UV.x).
   - `_Glint`: vệt sáng chéo chạy qua mặt kính mỗi 6–8s, và chạy ngay khi có bi va mạnh vào thành.
   - `_Tint`: phủ màu rất nhẹ lên phần bi nằm sau kính (0–8%), để bi "ở trong" hũ chứ không dán đè.

### 2.2 Bi thạch (jelly)
- **Squash & stretch khi chạm**: theo lực va (impulse). Nén theo trục va chạm tối đa 18%, phục hồi bằng lò xo (tần số 9Hz, tắt dần 0.25s).
- **Rung thạch khi đứng yên sau cú rơi**: 2–3 nhịp nhỏ.
- **Mặt nhân vật**: nhắm mắt/nheo khi bị va mạnh (0.2s), rồi trở lại.
- Vật lý: gravityScale ~0.9, bounciness 0.28 (cấp 1) → 0.12 (cấp 11), linear damping 0.2, giới hạn vận tốc 18 u/s.

### 2.3 Âm thanh va chạm
| Va chạm | Âm | Điều kiện |
|---|---|---|
| Bi ↔ thành/đáy hũ | **"ting" thủy tinh** trong, ngắn | impulse > ngưỡng; âm lượng & cao độ theo lực và cỡ bi (bi to trầm hơn); mỗi bi tối đa 1 tiếng / 0.12s |
| Bi ↔ bi | "boop" mềm như thạch | impulse > ngưỡng; tối đa 6 tiếng cùng lúc toàn hũ |
| Thả bi | "whoop" nhẹ khi rời tay | luôn |

### 2.4 Chuỗi hợp thể (theo hướng bạn chốt)
Toàn bộ chuỗi ~0.45s; hai bi bị tắt vật lý ngay từ bước 1.

| t (s) | Bước | Hình | Âm | Ghi chú |
|---|---|---|---|---|
| 0.00–0.10 | **Hút vào nhau** | 2 bi trượt về trung điểm (ease-in), mỗi bi dẹt theo trục nối 0.85×1.15, mặt → "surprised" | "vwip" hút | Nếu 1 bi bị kẹt: vẫn trượt, không va chạm |
| 0.10 | **Flash trắng** | Shader `_Flash` = 1 trên cả 2 bi (1–2 frame), phồng 1.1× | — | **Hit-stop**: cấp ≥ 6 dừng 40ms, cấp ≥ 9 dừng 70ms |
| 0.11–0.40 | **Khói nổ "bùm"** | **Toon splat** (flipbook 8 khung) tại trung điểm, tô màu cấp mới, lõi trắng + văng 6–10 giọt/sao nhỏ theo tia | "pop-bùm" + chuông "ting" cao độ tăng theo cấp | Kích cỡ splat = 1.6× đường kính bi mới |
| 0.12–0.30 | **Bi mới bật lên** | Scale 0 → 1.25 (OutBack 0.12s) → 0.92 → 1.0 (lò xo), mặt → "laugh"/"starstruck" | — | Bật vật lý lại + xung lực hướng lên nhỏ; **đẩy các bi trong bán kính 1.5r ra ngoài** |
| 0.20–0.80 | **Dư âm** | "+điểm" bật ra từ bi rồi bay về bộ đếm (bộ đếm nảy), ô cấp trên vòng tiến hóa sáng lên | tiếng "đếm" nhỏ | Combo trong 1s: cao độ tăng dần + chữ x2, x3 |

Cấp 8–10: thêm tia sáng xoay sau splat + camera kick 0.15s.
Cấp 11 + 11: slow-mo 0.3s, flash toàn màn, pháo giấy theo màu theme, splat lớn gấp đôi.

### 2.5 Nguy hiểm
Viền đỏ mờ quanh màn hình (vignette) nhịp theo tiếng tim đập, bi trên vạch run nhẹ, vạch đỏ nhấp nháy. Thoát nguy hiểm → vignette tắt dần 0.3s.

---

## 3. Eye Blast — khối "gạch men", khay lõm

| Hạng mục | Cách chạy |
|---|---|
| Bàn chơi | Khay **lõm**: viền ngoài có độ dày, ô trống có bóng trong (inner shadow) → khối đặt vào như "rơi xuống lỗ" |
| Khối | Mặt trên bóng + cạnh dưới dày (bevel, GPT vẽ), bóng đổ nhỏ lên khay |
| Kéo | Khối phóng to 1.0 → 1.08, bóng tách xa 20px; bám ngón tay bằng lerp (độ trễ ~60ms); gần ô hợp lệ < 0.35 ô → **hút vào** |
| Đặt | Khối rơi 12px xuống ô trong 0.08s, squash 0.9×1.1 → hồi; bụi toon nhỏ ở 4 góc; tiếng "cạch" gạch men |
| Báo trước | Hàng sắp xóa: viền sáng chạy quanh hàng (shader outline), mặt → mắt sao, khối rung rất nhẹ |
| Xóa hàng | Tia sáng quét dọc hàng (0.18s) → từng khối: **flash trắng → toon splat nhỏ màu khối → mảnh vỡ bắn ra** theo sóng từ điểm đặt → điểm bay về bộ đếm. Âm: chuỗi "pop" cao dần theo thứ tự nổ |
| Nhiều hàng | Thêm vòng sóng lớn + chữ khen (xem UI review) + camera kick + hit-stop 40ms |
| Thua | Khối xám dần từng hàng từ dưới lên (0.6s), mặt → "cry", âm trầm dần |

---

## 4. Arrow Out — giấy & mực, yên tĩnh

Trò chơi trí tuệ nên hiệu ứng **êm và rõ nghĩa**, không lòe loẹt.

| Hạng mục | Cách chạy |
|---|---|
| Bàn cờ | Tấm thẻ giấy có bóng đổ mềm, ô lưới là chấm nhỏ ấn chìm |
| Mũi tên | Nổi nhẹ khỏi giấy (bóng 2–3px), thân có rim sáng mảnh |
| Bay ra | **Trượt mượt liên tục** dọc thân (không nhảy ô), vệt mờ phía sau (trail), tới mép bàn thì "phụt" khói nhỏ màu mũi tên, âm "vút" nhẹ cao độ tăng theo chuỗi |
| Chạm sai | Mũi tên lao 0.3 ô về phía mũi tên cản → chạm → bật lại; mũi tên cản **nháy sáng**; âm "cộc" gỗ trầm; rung nhẹ |
| Gợi ý | Vẽ **đường bay** chấm chấm từ đầu mũi tên tới mép bàn + mũi tên phát sáng thở |
| Qua màn | Mũi tên cuối bay ra → bàn cờ "thở" 1 nhịp → sao bay vào bảng thắng lần lượt, mỗi sao 1 nốt nhạc cao dần |

---

## 5. Âm thanh cần thêm (Suno/ElevenLabs, xem SUNO_PROMPTS.md)
`glass_ting` (3 biến thể) · `jelly_boop` (3) · `suck_vwip` · `merge_boom` · `chime_tier` (1 mẫu, đổi cao độ trong game) ·
`tile_clack` (gạch men, 3) · `sweep_line` · `pop_chain` · `paper_whoosh` · `wood_knock` · `heartbeat_soft` · `counter_tick` · `star_note` (1 mẫu, đổi cao độ).

## 6. Shader cần viết (Shader Graph, URP 2D)
| Shader | Dùng cho | Thuộc tính chính |
|---|---|---|
| `SpriteFlash` | mọi vật có flash trắng / đỏ | `_Flash` (0–1), `_FlashColor` |
| `GlassFront` | mặt hũ Merge | `_Rim`, `_GlintPos`, `_GlintWidth`, `_Tint` |
| `JellyWobble` | bi Merge, khối Blast | biến dạng đỉnh theo `_Squash` (vector), `_Wobble` |
| `Outline` | hàng sắp xóa, gợi ý, bi đang cầm | `_OutlineColor`, `_OutlineWidth`, `_Pulse` |
| `Dissolve` | hồi sinh (xóa bi/khối), mũi tên tan ở mép | `_Cutoff`, `_EdgeColor`, noise texture |
