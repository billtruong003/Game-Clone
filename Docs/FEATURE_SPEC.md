# Feature spec v1.0 — Bruh Arrows · Nah Blocks · Meh Merge

(Tên cũ trong code/scene: ArrowOut · EyeBlast · EyeMerge.)

Tài liệu gốc cho 3 bước tiếp theo: **spec này → mockup (design) → dev**. Mockup vẽ theo đúng layout ở đây; dev làm theo
đúng feature ID và kiểm bằng user scenario ở đây. Có gì đổi thì sửa file này trước rồi mới sửa mockup/code.

Ký hiệu mức: **M** = phải có ở v1.0 · **S** = nên có nếu kịp · **L** = để sau ra mắt.
Trạng thái: ✅ có trong code · ⚠️ có nhưng sai/thiếu (kèm lý do) · ❌ chưa có.

---

## 0. Nguyên tắc scope

- Game casual: **một vòng chơi, làm thật mượt**. Không thêm chế độ, không thêm hệ thống chỉ để thu tiền.
- Kiếm tiền v1.0: **quảng cáo có thưởng** (người chơi tự chọn), **quảng cáo xen ván** (ở điểm nghỉ, có giới hạn), **Gỡ quảng cáo** (1 lần mua), **Shop skin** (xu kiếm trong game + gói All Skins mua 1 lần, mục 1b).
- **Không làm ở v1.0**: bán xu bằng tiền thật, hộp quà ngẫu nhiên (loot box), vòng quay, pass, chế độ Phiêu lưu, cơ chế chương mới của Bruh Arrows. (Xem mục 6.)
- Ưu tiên theo thứ tự: **không kẹt, không mất dữ liệu** → **cảm giác tay (feel)** → **hiểu luật mà không cần đọc** → đẹp.

---

## 1. Tính năng chung cả 3 game

| ID | Tính năng | Chi tiết | Mức | Hiện tại |
|---|---|---|---|---|
| G1 | Khởi động | Splash logo riêng từng game (≤1s), vào thẳng màn chơi/Home. Không có màn loading nếu tải < 1s | M | ⚠️ splash Unity mặc định |
| G2 | Consent (UMP) | Lần đầu: form Google cho người EEA/UK/CH. Chỉ sau khi xong mới khởi tạo quảng cáo. Offline: không chặn chơi, **thử lại khi có mạng** | M | ⚠️ không thử lại |
| G3 | Tạm dừng | Nút Pause góc trên. Popup: Tiếp tục · Chơi lại · 3 công tắc (Âm thanh, Nhạc, Rung) · Gỡ quảng cáo (ẩn nếu đã mua) · Chính sách bảo mật · Quyền riêng tư (chỉ khi UMP yêu cầu) | M | ✅ (thiếu nút Tiếp tục rõ ràng) |
| G4 | Nút Back Android | Có popup → đóng popup. Đang chơi → mở Tạm dừng. Ở Home (Arrow) → hỏi "Thoát game?" | M | ❌ |
| G5 | Ra nền / quay lại | Ra nền: tự mở Tạm dừng, lưu ngay. Quay lại: vẫn ở Tạm dừng, không mất gì | M | ❌ |
| G6 | Lưu dữ liệu | Kỷ lục lưu **ngay khi vượt**, không đợi thua. Cài đặt lưu ngay khi bấm. **Ván dở** (Blast/Merge, màn đang chơi của Arrow) lưu khi ra nền và khôi phục khi mở lại | M | ⚠️ chỉ lưu khi thua |
| G7 | Quảng cáo có thưởng | Chỉ khi người chơi bấm. **Không có QC / tắt sớm → quay lại đúng bảng trước đó** kèm toast "Chưa có video, thử lại sau". Nút hiện trạng thái "Đang tải…" nếu QC chưa sẵn | M | ⚠️ **kẹt game** |
| G8 | Quảng cáo xen ván | Chỉ ở điểm nghỉ (sau khi bấm "Chơi lại"/"Màn tiếp"). Bỏ 2 lần đầu mỗi phiên, cách nhau ≥ 90s, **không ngay sau QC có thưởng**, chỉ tính cooldown khi QC thật sự hiện | M | ⚠️ tính cooldown cả khi không hiện |
| G9 | Gỡ quảng cáo | 1 lần mua, tự khôi phục khi cài lại. Mua xong: toast + ẩn mọi nút gỡ QC. Không có mạng: báo "Cửa hàng chưa sẵn sàng", **thử kết nối lại** | M | ⚠️ chỉ kết nối 1 lần |
| G10 | Mời đánh giá | In-App Review của Google, sau 3 "khoảnh khắc vui", tối đa 1 lần/30 ngày, **không bao giờ đè lên bảng kết quả** (gọi sau khi bảng đóng) | M | ⚠️ đang gọi trùng lúc hiện bảng |
| G11 | Ngôn ngữ | EN mặc định, VI nếu máy tiếng Việt. Mọi chữ trên nút **tự co cỡ** để không tràn | M | ⚠️ chưa co cỡ |
| G12 | Rung | 3 mức: nhẹ (đặt/thả), vừa (xóa/hợp thể), mạnh (combo lớn, thua). Tắt được | M | ⚠️ thiếu quyền VIBRATE → không rung |
| G13 | Âm thanh | SFX có cao độ ngẫu nhiên ±5%, cao độ tăng theo combo. **Nhạc nền riêng từng game**, nhạc tắt khi Tạm dừng | M | ⚠️ chưa có file nhạc |
| G14 | Chạm | Một ngón. Ngón thứ 2 bị bỏ qua hoàn toàn | M | ⚠️ đang dùng API input cũ, không có tác dụng |
| G15 | Hiệu ứng mạnh | Tùy chọn "Giảm nháy màn hình" (tắt impact frame, rung camera) trong Cài đặt | S | ❌ |
| G16 | Bảng xếp hạng Play Games | Kỷ lục lên bảng xếp hạng toàn cầu | L | ❌ |

---

## 1b. Shop & skin (v1.0)

Mục tiêu: người chơi có thứ để "cày" và có lý do mua 1 lần, **không ảnh hưởng luật chơi** (skin chỉ đổi hình). Mọi skin
vẽ bằng pipeline của mình (`Tools/art`, faces.mjs, shader). **Ngoại lệ duy nhất: bàn tay** (PNG trong suốt do ChatGPT vẽ).

| ID | Tính năng | Chi tiết | Mức | Hiện tại |
|---|---|---|---|---|
| SH1 | Xu | Mỗi game ví riêng. Kiếm: Merge = điểm/50 khi hết ván; Blast = 2 xu/hàng xóa; Arrow = 10 xu/màn, 30 xu/Hôm nay, 1 xu/mũi tên ở Vô hạn. Bảng kết quả: "+N xu", nút **x2 xu (QC)** | M | ❌ |
| SH2 | Danh mục skin | ScriptableObject `SkinCatalog` mỗi game: id, tên EN/VI, loại (tab), giá xu / "xem QC x lần" / "chỉ All Skins", dữ liệu hình (bảng màu, lát mặt, tint kính…) | M | ❌ |
| SH3 | Màn Shop | Nút **Shop** ở Home (Arrow) / Pause + nút túi ở HUD (Blast, Merge, ngoài ván). Trên: preview lớn đang sống (mặt chớp mắt). Dưới: tab + lưới 2 cột thẻ skin. Thẻ: preview nhỏ + nút giá (xu / QC 1/3 / "Đang dùng" / "Dùng") | M | ❌ |
| SH4 | Mua & trang bị | Bấm thẻ → preview đổi ngay (thử trước khi mua). Đủ xu → mua + trang bị + confetti. Thiếu xu → nút rung + toast "Chưa đủ xu". Lưu ngay (SaveStore), áp dụng từ ván tiếp theo không cần khởi động lại | M | ❌ |
| SH5 | All Skins (IAP) | Non-consumable, mỗi game 1 gói **All Skins** (~2,99 $) + gói **All Skins + Gỡ QC** (~4,99 $). Mở hết skin hiện tại và sau này. Khôi phục khi cài lại. Giá hiển thị lấy từ Google Play | M | ❌ |
| SH6 | Skin mở bằng QC | 2–3 skin mỗi game mở bằng xem QC có thưởng (đếm 1/3, 2/3…), tiến độ lưu | S | ❌ |
| SH7 | Chấm đỏ "mới" | Nút Shop có chấm khi đủ xu mua ít nhất 1 skin chưa có | S | ❌ |
| SH8 | Skin bàn tay | Tab **Tay** dùng chung cả 3 game (8 skin, PNG do ChatGPT vẽ, xem STORE_ART_PROMPTS "Bàn tay"). Tay đang dùng hiện ở hướng dẫn, gợi ý, và ở điểm chạm khi kéo/chạm (chọc nhẹ khi tap, bám theo ngón khi kéo). Tắt được trong Cài đặt | M | ❌ |

**Danh sách skin v1.0** (mỗi game 8–10, 1 mặc định miễn phí):

| Game | Tab | Skin |
|---|---|---|
| Meh Merge | Bóng | Classic (free), Candy, Ocean, Forest, Sunset, Mono (đen trắng), Neon, Pastel — mỗi bộ = 11 màu theo tier |
| Meh Merge | Hũ | Glass (free), Frosted (sọc dày hơn), Amber tint, Mint tint, Night (nền tối + kính xanh) |
| Nah Blocks | Khối | Classic (free), Candy, Ocean, Retro (4 màu gameboy), Mono, Neon, Pastel, Jelly (bo tròn hơn) |
| Nah Blocks | Bàn | Navy (free), Paper (nền kem), Midnight |
| Bruh Arrows | Mũi tên | Classic (free), Candy, Ocean, Forest, Mono, Neon |
| Bruh Arrows | Giấy | Cream (free), Grid (giấy ô ly), Kraft, Night (giấy tối, mực sáng) |
| Cả 3 | Tay | Găng trắng (free), Tay trần, Bộ xương, Hiệp sĩ, Cổ động, Đá quý, Robot, Vàng |
| Cả 3 | Mặt | Deadpan (free), Sleepy, Grumpy, Derp — mỗi bộ 9 mặt = 1 Texture2DArray riêng vẽ bằng faces.mjs |

Luật policy: không random, không bán xu, giá IAP hiện rõ, không có nút "mua" giả dạng nút chơi, trẻ em không bị ép xem
QC (QC có thưởng luôn là tùy chọn).

---

## 2. Layout chuẩn (chung)

Khung tham chiếu **1080 × 1920 dọc**. Toạ độ tính từ **góc trên-trái**, đơn vị px tham chiếu.
Mọi thứ nằm trong **Safe Area** (tai thỏ, thanh điều hướng). Máy 20:9 cao hơn: phần dư chia đều cho vùng chơi.

```
 0 ┌──────────────────────────────┐
   │  TOP BAR  (0–280)            │  nút tròn 116px ở hai góc (tâm cách mép 100px),
   │  [⏸]     ĐIỂM 110pt     [x]  │  điểm ở giữa, dòng phụ (kỷ lục/màn) 44pt bên dưới
280├──────────────────────────────┤
   │                              │
   │  PLAY AREA (280–1640)        │  vùng chơi chính, căn giữa, lề trái/phải ≥ 60px
   │                              │
1640├─────────────────────────────┤
   │  BOTTOM BAR (1640–1920)      │  khay / nút phụ / dải tiến hóa; nút cao ≥ 140px
1920└──────────────────────────────┘
```

| Quy chuẩn | Giá trị |
|---|---|
| Vùng chạm tối thiểu | 116 × 116 (nút tròn), nút chữ cao ≥ 140 |
| Khoảng cách giữa 2 nút | ≥ 24 |
| Cỡ chữ | Điểm lớn 110–120 · tiêu đề popup 72 · nút 56–64 · dòng phụ 40–46 · tối thiểu 34 |
| Popup | Rộng 900, bo góc, nền tối 60% phía sau, ribbon tiêu đề; nút xếp dọc rộng 640 cao 150, cách nhau 20; nút X góc trên-phải |
| Nút chính trong popup | Luôn là nút **trên cùng**, màu xanh lá; nút phụ trắng; nút QC có icon ▶ |
| Toast | Giữa màn, y ≈ 700, tự tắt sau 1.5s, không chặn chạm |
| Màu | Mỗi game một bảng màu riêng (mục 3–5); chữ tối trên nền sáng hoặc ngược lại, tương phản ≥ 4.5:1 |

---

## 3. Eye Merge

### 3.1 Vòng chơi
Kéo ngang để ngắm → thả tay để rơi bi → 2 bi cùng cấp chạm nhau hợp thành 1 bi cấp cao hơn → bi tràn qua vạch đỏ quá 2s là thua.

### 3.2 Tính năng
| ID | Tính năng | Chi tiết | Mức | Hiện tại |
|---|---|---|---|---|
| M1 | **Đường ngắm** | Vạch nét đứt trắng 40% chạy **thẳng từ đáy bi đang cầm xuống điểm bi sẽ chạm đầu tiên** (tính bằng CircleCast bán kính bi). Tại điểm chạm vẽ **vòng tròn bóng mờ** cỡ bi. Cập nhật mỗi frame khi kéo | M | ❌ |
| M2 | **Vật lý "trôi" chứ không "chồng"** | Bi phải **lăn/trượt khỏi nhau và dàn đều xuống**, không đứng chồng lên đỉnh bi khác. Thông số xuất phát (chỉnh khi mockup chạy thử): **bỏ khóa xoay** (`freezeRotation = true` là nguyên nhân chính khiến bi đứng im trên nhau) — **mặt xoay theo bi**, không cần giữ thẳng; ma sát **0.35 → 0.05**; nảy 0.15 cho bi nhỏ giảm dần còn 0.05 ở bi lớn; khối lượng tỉ lệ diện tích (r²) để bi lớn đẩy được bi nhỏ; lệch ngẫu nhiên ±0.02 khi thả để không bao giờ cân bằng tuyệt đối trên đỉnh | M | ⚠️ |
| M3 | Cảm giác rơi | Trọng lực 1.4 → ~1.1, giới hạn vận tốc rơi. **Smear shader giả** khi rơi: thân bi (và mặt) kéo dãn theo hướng vận tốc, đuôi mờ dần, độ dãn tỉ lệ tốc độ, về tròn ngay khi chạm — giống khung "smear" trong hoạt hình 2D, tạo cảm giác có trọng lượng. Đây là **biến dạng chính quả bi**, không phải tia tốc độ/speed line (loại đó đã bị loại). Chạm: squash theo lực va chạm + rung thạch (đã có JellyWobble) | M | ⚠️ nặng như đá, chưa có smear |
| M4 | Hợp thể | Hút vào nhau (goo) → flash → bi mới bật overshoot → **đẩy nhẹ bi xung quanh** (lực nổ tỉ lệ cấp, bán kính 1.5r). Collider **không** phóng to theo hiệu ứng nảy | M | ⚠️ goo có; chưa đẩy; collider đang to theo |
| M5 | Combo | Hợp thể liên tiếp trong 1s: cao độ tăng dần + chữ "x2 x3…" bay lên + điểm nhân | S | ❌ |
| M6 | Bi kế tiếp | Ô "Tiếp" góc phải trên hiện đúng bi kế (có mặt). Bi mới xuất hiện **ở vị trí ngón tay/lần thả trước**, không nhảy về giữa | M | ⚠️ luôn về giữa |
| M7 | Nhịp thả | Cooldown 0.5s. Giữ tay trong lúc chờ thì bi mới xuất hiện là **ngắm được ngay** | M | ⚠️ phải nhấc tay ra |
| M8 | Cảnh báo nguy hiểm | Bi vượt vạch: vạch nhấp nháy + viền đỏ quanh hũ + tiếng tim đập + rung nhẹ + mặt bi hoảng. Thanh đếm 2s ngay trên vạch | M | ⚠️ chỉ vạch nhấp nháy |
| M9 | Điểm | Điểm hợp thể = n(n+1)/2 theo cấp; số điểm **bay từ chỗ hợp thể về bộ đếm**, bộ đếm đếm tăng dần + nảy | M | ⚠️ nhảy số tức thì |
| M10 | Dải tiến hóa | 11 chấm cấp ở đáy màn, cấp đã đạt sáng lên. Cấp mới lần đầu trong ván: chấm nảy + âm | M | ✅ |
| M11 | Cấp 11 | Hào quang + mưa sao 2s + chữ "MẮT THẦN!" + rung mạnh | S | ⚠️ chỉ confetti |
| M12 | Thua | Bi đóng băng, mặt chóng mặt, hũ xám dần 0.6s → bảng thua | M | ✅ |
| M13 | Hồi sinh | 1 lần/ván, xem QC → xóa bi phía trên (vùng 3.2 đơn vị dưới vạch) có hiệu ứng, **ra bi mới ngay** | M | ⚠️ kẹt nếu QC lỗi; không ra bi mới |
| M14 | Hướng dẫn lần đầu | Bàn tay chỉ "Kéo để ngắm, thả để rơi" + lần hợp thể đầu có chữ "Cùng loại thì gộp!" | M | ❌ |
| M15 | Lưu ván dở | Vị trí + cấp từng bi, điểm, bi kế. Mở lại app → tiếp tục đúng ván | M | ❌ |

### 3.3 Layout
```
 0 ┌──────────────────────────────┐
   │ [⏸]       1 234        Tiếp │  Pause (100,100) · Điểm 110pt (540,110)
   │          Kỷ lục 5 678   (●) │  Kỷ lục 44pt (540,200) · ô Tiếp 130px (970,150)
280├──────────────────────────────┤
   │            ( ● )             │  bi đang cầm y≈410 (HoldY 5.5)
   │              ┆               │  M1 đường ngắm nét đứt
   │ ╭────────────┆─────────────╮ │  miệng hũ y≈520
   │ │- - - - - - ┆- - - - - - -│ │  vạch nguy hiểm y≈630 (DangerY 3.3)
   │ │            ◌             │ │  M1 vòng bóng điểm rơi
   │ │     ●●   ●●●  ●          │ │  hũ rộng 900 (x 90–990), đáy y≈1650
   │ ╰──────────────────────────╯ │
1640├─────────────────────────────┤
   │  ● ● ● ● ● ● ● ● ● ● ●       │  dải 11 cấp, chấm 72px cách 88, y≈1850
1920└──────────────────────────────┘
```
Popup: Tạm dừng (G3) · Thua: tiêu đề "Hũ đầy rồi!", điểm 140pt, kỷ lục/"Kỷ lục mới!", cấp cao nhất đạt được, nút [▶ Hồi sinh] (nếu chưa dùng) · [Chơi lại].

### 3.4 User scenarios
| # | Tình huống | Kết quả mong đợi |
|---|---|---|
| EM1 | Mở game lần đầu | Consent (nếu EEA) → màn chơi + bàn tay hướng dẫn. Bi đầu tiên ở giữa |
| EM2 | Thả 5 bi cùng 1 chỗ | Bi lăn/trượt sang hai bên, **không đứng thành cột**; mặt xoay theo bi |
| EM2b | Thả bi từ trên cao | Bi dãn dài theo hướng rơi (smear), chạm là bẹp rồi nảy về tròn |
| EM3 | Thả bi lớn lên đống bi nhỏ | Bi lớn chen xuống, đẩy bi nhỏ dạt ra |
| EM4 | Ngắm | Đường ngắm + vòng bóng luôn chỉ đúng chỗ bi sẽ chạm đầu tiên |
| EM5 | 3 bi cùng cấp chạm nhau | 2 bi hợp thể, bi thứ 3 ở lại, không lỗi |
| EM6 | Pause trong 0.5s sau khi thả / giữa lúc hợp thể | Tiếp tục → vẫn có bi mới để thả, không bi nào rơi xuyên |
| EM7 | Bấm Hồi sinh, QC không có / tắt sớm | Quay lại bảng thua, toast "Chưa có video". Không kẹt |
| EM8 | Hồi sinh thành công | Bi phía trên nổ, có bi mới để thả ngay |
| EM9 | Pause → Chơi lại khi đang phá kỷ lục | Kỷ lục đã được lưu |
| EM10 | Ra nền giữa ván, Android kill app | Mở lại: đúng ván cũ, đang ở Tạm dừng |
| EM11 | Bấm Back khi đang chơi / khi mở popup | Mở Tạm dừng / đóng popup |
| EM12 | Chạm nút Pause | Không làm bi rơi / không bắt đầu ngắm |
| EM13 | Chơi trên máy 16:9 có tai thỏ | Bi đang cầm không đè lên chữ kỷ lục |

---

## 4. Eye Blast

### 4.1 Vòng chơi
Kéo 1 trong 3 khối ở khay lên bàn 8×8 → hàng/cột đầy thì xóa → dùng hết 3 khối thì khay đầy lại → không khối nào đặt được là thua.

### 4.2 Tính năng
| ID | Tính năng | Chi tiết | Mức | Hiện tại |
|---|---|---|---|---|
| B1 | Kéo khối | Chạm là khối **phóng to ngay** (không đợi kéo), nâng cao hơn ngón 1.5 ô, bám tay mượt (lerp), **đổ bóng** dưới khối | M | ⚠️ chỉ phóng to khi đã kéo |
| B2 | Hút ô | Gần ô hợp lệ → hiện **bóng mờ đúng màu khối** tại vị trí sẽ đặt; thả là đặt đúng vị trí bóng | M | ✅ bóng trắng |
| B3 | Báo trước xóa | Hàng/cột sắp xóa sáng viền + mặt nhân vật "mắt sao" | M | ✅ mắt sao; ❌ viền sáng |
| B4 | Thả sai | Khối bay về khay có nảy + âm "blocked" + rung nhẹ | M | ⚠️ im lặng |
| B5 | Đặt khối | Squash khi chạm + ô lân cận nảy nhẹ + bụi (đã có) | M | ✅ |
| B6 | Xóa hàng | 1 hàng: nổ theo sóng. ≥2 hàng: impact frame + flash + tia sáng + chữ "Tuyệt!/Xuất sắc!/Không thể tin nổi!" + rung (đã có impact frame) | M | ⚠️ thiếu chữ khen, "Combo x3" chưa dịch |
| B7 | Điểm | Số bay về bộ đếm, đếm tăng dần + nảy. Phá kỷ lục giữa ván: toast "Kỷ lục mới!" nhỏ, không dừng | M | ⚠️ |
| B8 | Chia khối | Đảm bảo **cả 3 khối đặt được theo ít nhất 1 thứ tự** (không chỉ 1 khối) | M | ⚠️ chỉ đảm bảo 1 |
| B9 | Khay | Khối không đặt được thì mờ đi (xám 50%) | S | ❌ |
| B10 | Sắp hết chỗ | Bàn > 70% đầy: mặt nhân vật lo lắng | S | ❌ |
| B11 | Thua | Khối không đặt được rung lên → bàn xám dần từng hàng từ dưới lên → mặt khóc → bảng thua | M | ⚠️ thua ngay |
| B12 | Hồi sinh | 1 lần/ván: xóa **3 hàng/cột đầy nhất** (không phải ô 4×4 cố định) có hiệu ứng + chia lại khay | M | ⚠️ có thể xóa trúng ô trống; kẹt nếu QC lỗi |
| B13 | Hướng dẫn lần đầu | Bàn tay kéo khối đầu tiên vào vị trí gợi ý tạo được 1 hàng | M | ❌ |
| B14 | Lưu ván dở | Bàn + 3 khối + điểm + streak | M | ❌ |
| B15 | Kéo khi tạm dừng/thua | Khối đang kéo luôn về khay, không bao giờ kẹt giữa màn | M | ⚠️ kẹt |

### 4.3 Layout
```
 0 ┌──────────────────────────────┐
   │            1 234        [⏸] │  Điểm 120pt (540,130) · Pause (980,100)
   │          ★ 5 678             │  Kỷ lục 46pt (540,225)
280├──────────────────────────────┤
   │ ┌──────────────────────────┐ │
   │ │ ▢▢▢▢▢▢▢▢                 │ │  bàn 8×8, ô 116, khung 972
   │ │ ...                      │ │  tâm y≈900 (khung 414–1386)
   │ └──────────────────────────┘ │
   │                              │
1410├─────────────────────────────┤
   │   [khối]   [khối]   [khối]   │  khay: 3 ô rộng 320, tâm x 210/540/870,
   │                              │  khối thu nhỏ 52%, vùng 1410–1770
1920└──────────────────────────────┘
```
Chú ý máy 16:9 có tai thỏ: đảm bảo khay không chạm khung bàn (khoảng hở ≥ 24).
Popup Thua: "Hết chỗ rồi!", điểm, kỷ lục, [▶ Hồi sinh] · [Chơi lại].

### 4.4 User scenarios
| # | Tình huống | Kết quả mong đợi |
|---|---|---|
| EB1 | Lần đầu mở | Bàn tay hướng dẫn kéo khối đầu tiên |
| EB2 | Chạm khối rồi thả không kéo | Khối phóng to rồi về khay, không lỗi |
| EB3 | Thả ra ngoài bàn / lên ô đã có | Khối bay về khay, có âm + rung nhẹ |
| EB4 | Kéo bằng 2 ngón, 2 khối cùng lúc | Chỉ ngón đầu tiên có tác dụng |
| EB5 | Đang kéo thì bấm Pause (ngón khác) / thì thua | Khối về khay |
| EB6 | Xóa 2 hàng + 1 cột cùng lúc | Impact frame + chữ khen + điểm đúng công thức |
| EB7 | Khay mới vừa chia | Có ít nhất 1 thứ tự đặt được cả 3 khối |
| EB8 | Hồi sinh, QC lỗi | Quay lại bảng thua, không kẹt |
| EB9 | Hồi sinh thành công | Xóa 3 hàng/cột đầy nhất, có khay mới |
| EB10 | Pause → Chơi lại khi đang vượt kỷ lục | Kỷ lục đã lưu |
| EB11 | Ra nền, app bị kill | Mở lại đúng ván |
| EB12 | Điểm 7 chữ số | Không đè lên icon sao |

---

## 5. Arrow Out

### 5.1 Vòng chơi
Chạm mũi tên có đường thoáng tới mép bàn → nó bay ra. Chạm mũi tên bị chặn → mất 1 tim. Gỡ hết là qua màn.
Chế độ: **100 màn** (có sao) · **Vô hạn** (mũi tên mới liên tục, tính điểm) · **Hôm nay** (1 đề chung mỗi ngày).

### 5.2 Tính năng
| ID | Tính năng | Chi tiết | Mức | Hiện tại |
|---|---|---|---|---|
| A1 | Mũi tên bay ra | Trượt mượt dọc thân + vệt mực + "poof" ở mép bàn | M | ✅ |
| A2 | Chạm sai | Mũi tên **lao tới chạm mũi tên cản đường rồi bật lại**, mũi tên cản **nháy sáng**, rung. Màn 1–5 **không trừ tim**, chỉ giải thích | M | ⚠️ chỉ rung đỏ, trừ tim từ màn 1 |
| A3 | Mũi tên mới xuất hiện | Chạm vào trong lúc đang hiện ra vẫn xử lý đúng, không bị kẹt nhỏ | M | ⚠️ **bị kẹt nhỏ** |
| A4 | Gợi ý | Vẽ **đường bay** của mũi tên gợi ý. Bấm lại khi đang hiện gợi ý thì không tốn thêm. Hết gợi ý → hỏi "Xem QC để nhận 1 gợi ý?" | M | ⚠️ chỉ nhấp nháy, tốn trùng, mở QC không hỏi |
| A5 | Tim | 3 tim. Hết tim → bảng "Hết tim" [▶ +1 tim] · [Chơi lại] · [Về menu] | M | ✅ (kẹt nếu QC lỗi) |
| A6 | Sao | 0 sai = 3 sao, ≤2 sai = 2 sao, còn lại 1 sao | M | ✅ |
| A7 | Chọn màn | 10 nhóm × 10 màn (chỉ đổi cách chia trang + tiêu đề nhóm "Màn 1–10" + tổng sao nhóm). **Không** thêm cơ chế mới | S | ⚠️ trang 20 màn |
| A8 | Màn Boss | Mỗi màn thứ 10, tiêu đề đỏ | M | ✅ |
| A9 | Vô hạn | Điểm, combo cùng màu (x2 x3…), cấp độ tăng dần, +1 tim mỗi 40 mũi tên. Hồi sinh **giữ nguyên số tim** | M | ⚠️ hồi sinh đặt tim = 1 |
| A10 | Hôm nay | Đề theo ngày **UTC**, kỷ lục lưu theo ngày bắt đầu ván. Home có chấm đỏ nếu hôm nay chưa chơi | M | ⚠️ theo giờ máy, lưu nhầm qua nửa đêm |
| A11 | Kỷ lục Vô hạn/Hôm nay | Lưu cả khi thoát giữa chừng / chơi lại | M | ⚠️ chỉ lưu khi thua |
| A12 | Hướng dẫn | Màn 1: bàn tay + ẩn nút phụ. Màn 1–3: dòng mục tiêu. Lần đầu vào Vô hạn/Hôm nay: 1 tip (lưu đã xem) | M | ⚠️ tip lặp mỗi lần |
| A13 | Qua màn | Sao bay lần lượt + confetti → bảng [Màn tiếp] · [Chơi lại] · [Chọn màn]. QC xen ván **sau khi bấm Màn tiếp** | M | ✅ |
| A14 | Phá đảo màn 100 | Bảng "Phá đảo!" + gợi ý chơi Vô hạn; Home đổi nút thành "Chơi Vô hạn" | S | ⚠️ |
| A15 | Mù màu | Combo Vô hạn phụ thuộc màu → mỗi màu khác cả độ sáng; đỏ lỗi khác xa màu cam | S | ⚠️ |

### 5.3 Layout
**Home**
```
 0 ┌──────────────────────────────┐
   │        (hình mũi tên)        │  y≈330
   │         ARROW OUT            │  150pt y≈520
   │  Trò chơi trí tuệ · gỡ mũi tên│  50pt y≈625
   │  [▶ Chơi · Màn 12        ]   │  nút xanh 720×170, y≈840
   │  [▦ Chọn màn             ]   │  720×150, y≈1040
   │  [∞ Chế độ vô hạn        ]   │  y≈1220
   │  [📅 Thử thách hôm nay  •]   │  y≈1400 (chấm đỏ nếu chưa chơi)
   │     [⚙]    [🏆]    [🚫AD]    │  3 nút tròn y≈1770
1920└──────────────────────────────┘
```
**Đang chơi (màn)**
```
 0 ┌──────────────────────────────┐
   │ [←] [💡3]  Màn 12   [↻] [⏸] │  y≈100, 5 nút/nhãn trên 1 hàng
   │          ♥ ♥ ♥               │  y≈250
   │      Còn 10 mũi tên          │  y≈330
   ├──────────────────────────────┤
   │   bàn 7×9, ô 136 (952×1224)  │  tâm y≈990
   ├──────────────────────────────┤
   │  (dòng tip, không đè bàn)    │  chỉ hiện ở vùng trống, không chồng hàng cuối
   │ [← Màn trước] [Màn sau →]   │  430×140, y≈1780
1920└──────────────────────────────┘
```
(Nút phải trên đổi từ ⚙ sang ⏸ cho thống nhất với 2 game kia.)

### 5.4 User scenarios
| # | Tình huống | Kết quả mong đợi |
|---|---|---|
| AO1 | Lần đầu mở | Home → Chơi → màn 1 có bàn tay, không nút thừa |
| AO2 | Chạm mũi tên bị chặn ở màn 3 | Mũi tên lao tới vật cản, vật cản nháy, **không** mất tim |
| AO3 | Chạm 2 lần liên tiếp vào mũi tên bị chặn | Mũi tên về đúng ô, không lệch |
| AO4 | Chạm mũi tên đang hiện ra | Xử lý bình thường, không kẹt nhỏ |
| AO5 | Hết tim, bấm +1 tim, QC lỗi | Quay lại bảng hết tim |
| AO6 | Hết tim → mở Pause → đóng | Không chơi tiếp được với 0 tim |
| AO7 | Đổi màn khi mũi tên đang bay | Không sót vệt mực |
| AO8 | Chơi Hôm nay qua nửa đêm | Kỷ lục ghi vào ngày bắt đầu ván |
| AO9 | Vô hạn: đầy bàn, hồi sinh khi còn 3 tim | Hồi sinh xong vẫn 3 tim |
| AO10 | Thoát Vô hạn giữa chừng khi đang vượt kỷ lục | Kỷ lục đã lưu |
| AO11 | Qua màn 100 | "Phá đảo!", Home gợi ý Vô hạn |
| AO12 | Back ở Home | Hỏi "Thoát game?" |

---

## 6. Không làm ở v1.0 (để sau khi có số liệu)
- (Shop, skin, All Skins đã chuyển vào v1.0, mục 1b.) Bán xu bằng tiền thật, skin theo mùa/sự kiện.
- Cơ chế chương mới của Arrow Out (mũi tên khóa, ô cấm, mũi tên đôi…).
- Chế độ Phiêu lưu Eye Blast, nhiệm vụ ngày, phần thưởng đăng nhập, album sưu tập.
- Bảng xếp hạng / thành tựu Play Games (G16).

Lý do: đều là nội dung "giữ chân / thu tiền". Làm khi closed test hoặc số liệu sau ra mắt cho thấy người chơi bỏ game vì thiếu mục tiêu.

## 7. Style cố định & cách làm asset

- **Một style duy nhất cho cả 3 game**, chốt trong mockup trước khi vẽ bất kỳ asset nào: bảng màu (theo từng game), độ dày viền, bán kính bo góc, cách đổ sáng (1 mảng sáng cứng, không gradient mềm), kiểu mặt emoji, font, icon.
- **Asset vẽ thẳng trong file mockup** (vector), dùng lại đúng các token style ở trên → **export ra PNG/sprite sheet** đưa thẳng vào project (qua `Tools/art` → `SheetSlicer`), không vẽ lại lần hai. Mockup chính là nguồn asset.
- Mỗi asset trong mockup có tên trùng tên sprite trong game (`block_fill`, `circle_line`, `icon_pause`…) để export/thay thế tự động.
- Hiệu ứng động (smear, squash, goo, impact frame, vệt mực) mô tả trong mockup bằng khung hình/ghi chú, làm bằng shader/FX trong Unity.

## 8. Bước tiếp theo
1. Duyệt spec này (thêm/bớt feature, đổi mức M/S).
2. **Mockup**: trang đầu là **style sheet** (mục 7), sau đó vẽ toàn bộ màn theo layout mục 2–5 (kể cả trạng thái: hướng dẫn, nguy hiểm, popup, toast, QC đang tải). Asset vẽ trong mockup rồi export thẳng vào game.
3. **Dev**: làm hết mục M, kiểm bằng toàn bộ user scenario (mỗi scenario = 1 test tay, cái nào tự động được thì viết EditMode test).
