# Kế hoạch nâng cấp: từ prototype lên bản ship

> Tài liệu liên quan: `UI_LOGIC_REVIEW.md` (soát logic 32 màn mockup) · `FEEL_SPEC.md` (chiều sâu, shader thủy tinh, âm va chạm,
> chuỗi hợp thể hút → flash → toon splat → bật lên) · `GPT_ASSET_BRIEF.md` (10 đợt gen theo lưới chuẩn + `Tools/art/import-gpt.py`).

Chẩn đoán chung: cả 3 game đã **đúng luật** nhưng **chưa có "cảm giác"**. Mỗi hành động chỉ có 1 phản hồi
(một tween + một âm thanh), trong khi game casual bán được thường xếp 4–6 lớp phản hồi cho cùng một hành động:
anticipation → hành động → va chạm (squash, hit-stop, rung) → phần thưởng (hạt, số bay, âm tăng cao độ) → dư âm (nhân vật phản ứng).

Ký hiệu độ ưu tiên: 🔴 phải có trước khi ship · 🟡 nên có · ⚪ để sau khi có số liệu.

---

## 0. Nền tảng dùng chung (làm trước, cả 3 game hưởng)

| # | Hạng mục | Hiện tại | Cần làm | Ưu tiên |
|---|---|---|---|---|
| C1 | **Juice kit** | Chỉ có Tween + Punch | `Juice.Squash(t, dir, amount)`, `Juice.Wobble` (lò xo tắt dần), `HitStop(0.05s)`, `CameraKick`, `Flash` (lớp trắng 1 frame), `ScorePopup` (số bay về bộ đếm) | 🔴 |
| C2 | **Bộ đếm điểm** | Nhảy số tức thì | Đếm tăng dần + nảy + đổi màu khi combo; "Kỷ lục mới!" có dải băng + pháo giấy | 🔴 |
| C3 | **Nền màn chơi** | Màu trơn | Nền có hoa văn nhẹ + vài hình tròn trôi chậm (parallax), mỗi game một bảng màu | 🔴 |
| C4 | **Nhân vật có hồn** | Mặt chạy loop riêng | Nhân vật **nghiêng/nhìn về phía ngón tay** (dịch lớp mặt vài px), chớp mắt ngẫu nhiên, co giãn "thở"; phản ứng dây chuyền với hàng xóm | 🔴 |
| C5 | **Âm thanh** | 17 SFX, cao độ cố định | Random ±5% cao độ, chuỗi cao độ tăng dần theo combo, âm "whoosh" khi mở popup, nhạc nền (Suno) | 🔴 |
| C6 | **Pool object** | Blast tạo/hủy UI mỗi lần đặt, Merge tạo/hủy bi | Pool cho khối, bi, số bay, particle → không giật GC trên máy yếu | 🔴 |
| C7 | **Ngôn ngữ** | Chỉ tiếng Việt | EN mặc định + VI (Play Store toàn cầu). Bảng chuỗi đơn giản + font có Latin mở rộng | 🔴 |
| C8 | **Chuyển cảnh / popup** | Fade + scale | Popup trượt + nảy, nền mờ dần, nút xuất hiện lần lượt; nút chính "thở" nhẹ để gọi bấm | 🟡 |
| C9 | **Haptic** | Một kiểu rung | 3 mức (nhẹ khi đặt, vừa khi xóa, mạnh khi combo lớn) | 🟡 |
| C10 | **Ads/IAP thật** | Giả lập | LevelPlay/AdMob + Unity IAP (remove_ads, gói theme) | 🔴 (trước ship) |
| C11 | **Analytics** | Không có | Sự kiện: bắt đầu/kết thúc ván, điểm, màn thua, dùng gợi ý, xem ads → để cân bằng bằng số thật | 🟡 |

---

## 1. Eye Merge (Suika): vấn đề lớn nhất là **độ nặng & độ nảy**

### Cảm giác vật lý
| # | Vấn đề | Cách sửa | Ưu tiên |
|---|---|---|---|
| M1 | Rơi nhanh, chạm là đứng im → "nặng như đá" | gravityScale 1.4 → ~0.9, bounciness 0.12 → 0.3 cho bi nhỏ (giảm dần theo cấp), ma sát thấp hơn, giới hạn tốc độ tối đa | 🔴 |
| M2 | Bi cứng như bi-a | **Squash & stretch** theo lực va chạm + **rung thạch** (lò xo tắt dần) sau mỗi cú chạm | 🔴 |
| M3 | Hợp thể "biến mất – xuất hiện" | Anticipation: 2 bi hút vào nhau 0.08s → nổ vòng sóng → bi mới bật overshoot 1.3 → **đẩy nhẹ bi xung quanh** (lực nổ nhỏ) | 🔴 |
| M4 | Không biết bi sẽ rơi đâu | **Đường ngắm** nét đứt từ bi đang cầm xuống điểm chạm, bi cầm lắc lư nhẹ | 🔴 |
| M5 | Combo không có cảm giác | Chuỗi hợp thể liên tiếp trong 1s → cao độ tăng dần + chữ "x2 x3" + hit-stop ở cấp ≥ 8 | 🟡 |
| M6 | Nguy hiểm chỉ có vạch nhấp nháy | Viền đỏ quanh màn hình + tiếng tim đập + bi trên vạch run lên | 🟡 |

### Hình ảnh
- Hũ thủy tinh: thêm lớp phản chiếu, đáy có bóng đổ, nền phía sau hũ có hoa văn. 🔴
- HUD: ô "Tiếp" hiện đúng nhân vật kế tiếp (có mặt), vòng tiến hóa dạng **vòng tròn** như Suika thay cho dải chấm. 🟡
- Cấp 11: hào quang xoay + hạt lấp lánh liên tục. 🟡

### Theme: **10 skin bán IAP** (Emoji là gói mở rộng)
Mỗi theme = 11 cấp nhân vật + màu hũ + nền + bộ particle + (tùy chọn) âm thanh riêng. Luật chơi không đổi.
| # | Theme | 11 cấp (nhỏ → lớn) | Điểm nhấn hình ảnh | Giá đề xuất |
|---|---|---|---|---|
| 1 | **Mắt** (mặc định) | chấm → mèo → dê → sao → tim → xoáy → vòng → lửa → hoa → cầu vồng → Mắt Thần | mắt nhìn theo bi đang rơi | Miễn phí |
| 2 | **Emoji Pop** | sleepy → shy → happy → wink → grin → silly → laugh → love → cool → starstruck → 👑 | **cùng biểu cảm ghép thành biểu cảm kế tiếp**, mặt chạy 12 khung | $1.99 |
| 3 | **Trái cây** | cherry → dâu → nho → quýt → hồng → táo → lê → đào → dứa → dưa lưới → dưa hấu | vỏ bóng, hợp thể bắn giọt nước | $0.99 |
| 4 | **Bi-a** | bi số 1 → 11 (sọc/đặc), cấp 11 = bi số 8 vàng | shader bóng loáng + tiếng "cạch" khi va chạm | $0.99 |
| 5 | **Hành tinh** | thiên thạch → Mặt Trăng → Sao Thủy → … → Sao Mộc → Mặt Trời | shader bề mặt cuộn chậm, vành đai Sao Thổ, cấp 11 phát sáng | $1.99 |
| 6 | **Kẹo ngọt** | kẹo viên → kẹo mút → donut → macaron → cupcake → … → bánh kem | sprinkles bắn ra khi hợp thể | $0.99 |
| 7 | **Thú cưng** | chuột hamster → thỏ → mèo → chó → gấu trúc → … → cá voi | tai/đuôi nhún nhảy | $1.99 |
| 8 | **Bóng thể thao** | bóng bàn → golf → tennis → bóng chày → … → bóng rổ → bóng bãi biển | mỗi loại âm va chạm riêng | $0.99 |
| 9 | **Đá quý** | thạch anh → ngọc lục bảo → sapphire → ruby → … → kim cương | shader lấp lánh xoay theo góc | $1.99 |
| 10 | **Neon Galaxy** | 11 quả cầu neon | shader phát sáng + nền vũ trụ cuộn, bloom | $2.99 |
Gói **All Skins** $5.99 (rẻ hơn mua lẻ ~60%). Mở 1 theme bằng **xem 5 quảng cáo** (1 theme/tuần) để người không trả tiền vẫn có mục tiêu.

Kỹ thuật:
- `MergeTheme` (ScriptableObject): sprite từng cấp (ảnh màu đầy đủ, không tint), tỉ lệ bán kính, màu particle, nền, hũ, SFX, shader tùy chọn.
- Mỗi theme một sheet **1024×1024** (11 ô 256 + icon shop) → nạp theo theme đang dùng, không load cả 10.
- Màn chọn skin: nút nhỏ trên HUD → bảng lưới 10 theme (xem trước 3 cấp chuyển động, giá / "Đang dùng" / "Xem 5 quảng cáo").
- Tên theme tự đặt; hình trái cây tự vẽ. **Không dùng chữ "Suika"** hay copy hình của game gốc.

---

## 2. Eye Blast: kéo thả chưa "dính tay", xóa hàng chưa "đã"

| # | Hạng mục | Cần làm | Ưu tiên |
|---|---|---|---|
| B1 | Kéo khối | Khối bám ngón tay có độ trễ mượt (lerp), đổ bóng dưới khối, **hút vào ô gần nhất** khi đủ gần; thả sai thì bay về khay có nảy | 🔴 |
| B2 | Đặt khối | Squash khi chạm, các ô lân cận nảy nhẹ (sóng), mặt nhân vật vui | 🔴 |
| B3 | Báo trước xóa hàng | Đã có mặt "mắt sao"; thêm **hàng sáng viền vàng** + khối bóng mờ đổi sang màu của khối đang kéo | 🔴 |
| B4 | Xóa hàng | Tia sáng quét dọc hàng → khối nổ lần lượt theo sóng → điểm bay về bộ đếm; nhiều hàng: rung + chữ lớn **"Tuyệt!" / "Xuất sắc!" / "Không thể tin nổi!"** | 🔴 |
| B5 | Nhân vật nhìn khối | Khi kéo, các mặt trên bàn **nhìn theo** khối (C4); gần hết chỗ thì mặt lo lắng | 🟡 |
| B6 | Khay | Bệ đỡ cho 3 khối, khối mới rơi vào khay có nảy, khối không đặt được thì mờ đi | 🟡 |
| B7 | Thua | Bàn xám dần từng hàng từ dưới lên, mặt khóc, rồi mới hiện bảng | 🟡 |
| B8 | Bàn chơi | Khung có viền sáng, ô trống có hoa văn nhẹ, nền tối có hạt trôi | 🔴 |
| B9 | Meta | Chế độ "Phiêu lưu" (mục tiêu mỗi màn: xóa X khối màu…) | ⚪ |

---

## 3. Arrow Out: đúng chất trí tuệ nhưng còn khô

| # | Hạng mục | Cần làm | Ưu tiên |
|---|---|---|---|
| A1 | Mũi tên bay ra | Hiện nhảy từng ô (28ms/bước) → **trượt mượt liên tục** dọc thân, có vệt mờ phía sau, "pop" nhỏ ở mép bàn | 🔴 |
| A2 | Chạm sai | Rung đỏ + **mũi tên lao tới chạm vào mũi tên cản đường rồi bật lại**, mũi tên cản nháy sáng → người chơi hiểu vì sao sai | 🔴 |
| A3 | Bàn cờ | Có tấm nền bo góc, ô lưới dịu hơn, mũi tên có bóng rất nhẹ; theo từng chương đổi bảng màu | 🔴 |
| A4 | Qua màn | Bàn "quét sạch" + sao bay vào thanh tiến độ chương | 🟡 |
| A5 | Chọn màn | Chia **10 chương × 10 màn**, mỗi chương một màu/tên, thanh sao theo chương, mở khóa chương có hiệu ứng | 🔴 |
| A6 | Độ khó đầu game | Màn 4–20 gần như đi ngang (độ khó ~1) → rút xuống còn ~10 màn nhập môn, đẩy đường cong lên sớm hơn | 🔴 |
| A7 | Chiều sâu trí tuệ | Mỗi chương giới thiệu **1 cơ chế mới**: mũi tên khóa (cần gỡ chìa trước), mũi tên đổi hướng khi bị chạm, ô cấm, mũi tên đôi (bay 2 đầu)… | 🟡 |
| A8 | Daily | Lịch + chuỗi ngày, huy hiệu 7 ngày | 🟡 |
| A9 | Gợi ý | Hiện chỉ nhấp nháy → vẽ **đường bay** của mũi tên gợi ý | 🟡 |

---

## 3b. Shader: nâng hình ảnh mà không cần vẽ thêm
Tất cả viết bằng **Shader Graph cho URP 2D** (Sprite Unlit/Lit + UI), có bản fallback tắt được cho máy yếu.
| # | Shader | Dùng ở đâu | Ưu tiên |
|---|---|---|---|
| S1 | **Flash / Hit** (trộn màu trắng theo `_Flash`) | Mọi va chạm, hợp thể, đặt khối, chạm sai (đỏ) | 🔴 |
| S2 | **Dissolve có viền phát sáng** (noise + ngưỡng) | Khối Blast bị xóa, mũi tên tan khi ra khỏi bàn, bi biến mất khi hồi sinh | 🔴 |
| S3 | **Shine sweep** (vệt sáng chạy chéo) | Nút chính, sao ở bảng thắng, cấp 11, ô theme trong shop | 🔴 |
| S4 | **Outline / glow** (dilate alpha) | Gợi ý Arrow, hàng sắp xóa ở Blast, bi đang cầm ở Merge | 🔴 |
| S5 | **Wobble / jelly** (biến dạng đỉnh theo vận tốc + va chạm) | Bi Merge, khối Blast lúc rơi vào ô | 🟡 |
| S6 | **Soft shading giả 3D** (rim light + gradient theo normal tròn) | Thân nhân vật, bi-a, đá quý | 🟡 |
| S7 | **UV scroll / pattern** | Nền động (hoa văn trôi), bề mặt hành tinh, Neon Galaxy | 🟡 |
| S8 | **Rainbow / hologram** | Theme trả phí cao cấp, huy hiệu, cấp 11 | ⚪ |
| S9 | Post-process URP: **Bloom** nhẹ + **Vignette** đỏ khi nguy hiểm + chromatic 1 frame khi combo lớn | Toàn game (tắt được trong cài đặt) | 🟡 |
| S10 | **Shockwave distortion** (vòng khúc xạ) | Hợp thể cấp ≥ 8, clear 3+ hàng | ⚪ (tốn GPU, làm sau cùng) |

## 3c. Particle / VFX: làm lại từ đầu
**Vấn đề hiện tại:** 5 hiệu ứng, mỗi cái 1 lớp, 1 hình tròn/sao phẳng, không có ánh sáng, không có khói, không có vệt, không có flipbook.
Cảm giác "nổ" không có trọng lượng nên chơi không thỏa mãn.

**Nguyên tắc mới:** mỗi sự kiện là một **preset nhiều lớp** (3–6 lớp chạy cùng lúc), cấu hình bằng ScriptableObject `FxPreset`:
flash (1–2 frame, additive) → vòng sóng → mảnh vụn (có trọng lực, xoay) → tia lửa (stretched billboard, có trail) → khói/bụi (flipbook) → lấp lánh còn đọng lại.
Dùng thêm các module: Trails, Noise, Sub Emitters, Size/Color over Lifetime có gradient, Velocity over Lifetime (hút về điểm), Texture Sheet flipbook.

**Công thức cho từng sự kiện:**
| Sự kiện | Lớp hiệu ứng |
|---|---|
| Merge hợp thể (cấp thường) | flash trắng + vòng sóng màu cấp + 8–14 giọt/mảnh cùng màu văng ra + 6 lấp lánh + "+điểm" bay lên |
| Merge cấp ≥ 8 | thêm tia sáng xoay (light rays), khói màu, pháo giấy, hit-stop 0.05s, rung camera |
| Merge cấp 11 / đỉnh | vụ nổ ánh sáng toàn màn, mưa sao 2s, confetti theo màu theme |
| Blast đặt khối | bụi nhỏ ở 4 góc, flash ô |
| Blast xóa 1 hàng | tia quét dọc hàng (trail) → mỗi khối nổ mảnh vụn màu + lấp lánh theo sóng |
| Blast xóa ≥ 2 hàng / combo | thêm vòng sóng lớn, sao bay về bộ đếm, chữ khen phát sáng |
| Arrow bay ra | vệt trail theo màu mũi tên + "pop" khói nhỏ ở mép bàn |
| Arrow chạm sai | tia lửa đỏ ở điểm va + chấm "!" |
| Qua màn / kỷ lục | pháo giấy 2 phía + sao + tia sáng sau dải băng |
| Nút bấm / nhận thưởng | lấp lánh nhỏ, xu/sao bay theo đường cong về đích |

**Quyết định phong cách (28/09):** mọi hiệu ứng phải **cartoon, vector mượt, không rỗ hạt**.
- Texture tả thực (khói Kenney Smoke, flipbook Thomas Iché trong gói Brackeys) và pixel art (CodeManu) → **loại**.
- Khói (toon splat), nổ, lấp lánh, vòng sóng, pháo giấy… → **ChatGPT vẽ theo lưới** (`GPT_ASSET_BRIEF.md` đợt 05).
  Bộ `Tools/art/fx-cartoon/` mình vẽ **chỉ là placeholder** để test logic, không phải hướng art.
- Từ gói CC0 chỉ giữ vài texture **mượt** làm lớp ánh sáng phụ (quầng sáng tròn, tia sáng, vòng, sao lóe).

**Nguồn texture (đều CC0, dùng thương mại tự do):**
| Gói | Đã tải về `Tools/art/src-fx/` | Dùng cho |
|---|---|---|
| [Kenney Particle Pack](https://kenney.nl/assets/particle-pack) (80 texture 512px) | 25 texture: vòng `circle_02/03`, quầng sáng `circle_05` `flare_01` `light_03`, lấp lánh `star_01/04/06/07/08/09` `magic_03/05`, vệt `trace_01/02` `slash_03/04`, xoáy `twirl_01–03`, khói `smoke_07/08/10`, tim `symbol_01`, bụi `dirt_02` | lấp lánh, vòng sóng, vệt bay, xoáy khi hợp thể, khói tiếp đất, tim cho emoji "love" |
| [Kenney Smoke Particles](https://kenney.nl/assets/smoke-particles) (5 bộ flipbook) | White puff (25 khung), Flash (9), Explosion (9) | khói phụt khi đặt/tiếp đất, flash khi hợp thể lớn, nổ khi clear nhiều hàng |
- Gộp các texture đã chọn vào **1 sheet `fx` 1024×1024** (lũy thừa 2, nén được), flipbook khói 4×4 trong cùng sheet; particle dùng Texture Sheet Animation chế độ Sprites.
- Gói **Brackeys VFX Bundle** (CC0, 26MB, itch.io) có thêm flipbook nổ/lửa đẹp. itch.io không cho tải bằng script → bạn tải tay nếu muốn, bỏ vào `Tools/art/src-fx/brackeys/`.
- Không dùng: Epic Toon FX (bản lậu); Vefects Flipbook VFX ($19, trang của họ ghi không chạy với 2D Renderer của project).

## 4. Asset cần vẽ thêm (bản tạm mình vẽ trước, ChatGPT thay sau)
| Nhóm | Asset |
|---|---|
| Nền | 3 nền có hoa văn tile được (Arrow sáng, Blast tối, Merge tím), sprite "hạt trôi" |
| Merge | 11 cấp theme "Mắt", vương miện cấp 11, lớp phản chiếu hũ, bóng đổ đáy, vòng tiến hóa |
| Blast | Bệ khay, khung bàn phát sáng, tia quét hàng, 3 chữ khen (Tuyệt / Xuất sắc / Không thể tin nổi) |
| Arrow | Tấm nền bàn cờ, 10 màu chương, icon chương, huy hiệu daily |
| Chung | Dải băng "Kỷ lục mới", viền đỏ nguy hiểm, vệt sáng (trail), mảnh sóng xung kích |
| Theme Merge | 10 sheet 1024² (11 cấp × 256px + icon shop): Mắt, Emoji Pop, Trái cây, Bi-a, Hành tinh, Kẹo, Thú cưng, Bóng thể thao, Đá quý, Neon → **110 nhân vật**, lý tưởng nhất để gen bằng ChatGPT theo lưới 4×3 |
| Shop | Ô theme (mở / khóa / đang dùng), nhãn giá, icon "xem 5 quảng cáo", banner gói All Skins |
| Shader | Texture noise (dissolve), texture gradient rim (shading giả 3D), texture sao (Galaxy) |

---

## 4b. Mockup (bước bắt buộc trước khi sửa)
Canvas thiết kế 32 màn: https://claude.ai/artifact/CKUsrWUzb9ZUodwQa57DSA (chỉ chủ sở hữu mở được tới khi bấm Share).
Trang: Hệ thống & cài đặt (khởi động, đồng ý quảng cáo UMP, cài đặt đầy đủ, Google Play Games 5 trạng thái, mua Gỡ quảng cáo,
quảng cáo có thưởng 5 trạng thái, mời đánh giá, Hub) · Arrow Out 11 màn · Eye Blast 6 màn · Eye Merge 7 màn (gồm cửa hàng 10 skin).
Mỗi trang có ghi chú kịch bản chính + edge case. Duyệt mockup xong mới bắt đầu Phase A.

## 4c. Build nhiều game trong 1 project
`Tools → Casual Game → Build Switcher` (Ctrl+Shift+B): mỗi game một `GameProfile` (scene, sheet, âm thanh, package id, version, define).
Switch = đổi Build Settings + Player Settings + define + thư viện art/âm thanh riêng + nối scene. Không còn gì trong `Resources`
ngoài material particle, nên build chỉ chứa đúng game được chọn (Arrow Out 7,7 MB vs 15,9 MB nếu kéo theo mặt emoji).
Nút Report liệt kê dung lượng và cảnh báo asset lọt từ game khác.

## 5. Lộ trình đề xuất

| Phase | Nội dung | Kết quả kiểm chứng |
|---|---|---|
| **A: Feel** | C1, C2, C4, C5, C6 · M1–M4 · B1–B4 · A1–A2 · **S1, S4 · FxPreset + sheet fx mới (3c)** | Quay clip 15s mỗi game, so trước/sau; test không rớt khung (Profiler) |
| **B: Visual & UI** | C3, C8 · M-hình ảnh · B8 · A3, A5 · asset mục 4 · **S2, S3, S5–S7, S9** | Ảnh chụp màn hình từng screen |
| **C: Nội dung & kiếm tiền** | Theme system + **10 skin Merge + shop** · A6–A7 · Daily · C10 | EditMode test cho luật mới; mua gói/giả lập mua chạy đúng |
| **D: Ship** | C7 ngôn ngữ · C11 analytics · Android build (ASTC), icon, store listing | AAB chạy trên máy thật |
