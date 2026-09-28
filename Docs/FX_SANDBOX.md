# FX Sandbox

Mở: **Tools ▸ Casual Game ▸ FX Sandbox**. Menu này mở scene `Assets/_Game/Scenes/Sandbox/FxSandbox.unity`, không nằm trong build. Sau đó bấm **Play**.

## Điều khiển
| Thao tác | Tác dụng |
|---|---|
| Click vào màn | Phát effect đang chọn tại con trỏ |
| Space | Phát lại ở vị trí cũ |
| R | Tự lặp lại (khoảng lặp chỉnh bằng slider) |
| 1–9 | Chọn effect |
| H | Ẩn/hiện panel (khi ẩn, camera về giữa, như màn game thật) |
| Time 0.1x → 1x | Slow-mo để soi từng khung |
| Scale | Nhân kích cỡ effect |
| Tint | Màu tô cho các system "tintable" (xem bên dưới) |
| Background | navy (Blast) · night (Merge) · cream (Arrow) · grey |
| Scale reference | Bi 1.4, khối 1.1, bi cấp to 2.2: bằng cỡ thật trong game, để so kích thước |
| Merge chain | Hút vào nhau → flash trắng + hit-stop → burst → bi mới bật lên (FEEL_SPEC §2.4) |
| Blast line clear | Tia quét → từng khối flash → nổ, lan ra từ ô vừa đặt (FEEL_SPEC §3) |

## Thiết kế một effect
- Mỗi effect là 1 prefab trong `Assets/_Game/Fx/Prefabs/`. Gốc prefab có `FxEffect`, bên dưới là các ParticleSystem con.
- Có 2 cách chỉnh: bấm **Select prefab** trong panel, hoặc mở prefab. Chỉnh trong Inspector **ngay khi đang Play**, rồi click lại là thấy thay đổi. Khi thoát Play, thay đổi **được giữ lại** (sửa trên asset prefab).
- Effect mới: duplicate một prefab trong thư mục rồi bấm **Rescan**.
- `FxEffect.tintable`: danh sách system được nhân màu theo tint (khói, giọt…). Các system còn lại (lõi flash trắng, vòng…) giữ màu đã chỉnh.
- Timing của 2 chuỗi demo chỉnh ở Inspector của GameObject `FxSandbox` (components `MergeChainDemo`, `LineClearDemo`). Số nào chốt thì chuyển sang `FeelConfig` của game.

## Goo & jelly (shader/feel, dùng cùng effect ETFX)
- **Goo merge**: `Core/GooMerge.cs` + shader `Fx/Shaders/GooMerge.shader`. Khi hai bi hút nhau, thân sprite được ẩn đi và vẽ thay bằng **smooth union** của hai hình tròn (viền navy + vệt sáng giống sprite), nên hai bi chảy vào nhau như hai giọt nước. Demo **Merge chain** chia làm 2 pha: `approachTime` (tiến lại tới lúc chạm) và `meltTime` (cổ nối hình thành, khối phình tới `growTo` = đúng cỡ bi mới rồi bi mới bật ra liền mạch). `gooBlend` điều chỉnh độ nhão.
- **Jelly wobble**: `Core/JellyWobble.cs` đặt trên transform *hình* của bi (không phải thân vật lý). `Impact(lực, pháp tuyến, bán kính)` nén theo trục va, phình ngang giữ thể tích, rung lò xo tắt dần, mặt tiếp xúc giữ nguyên chỗ. Demo **Drop & wobble**: rơi thì dãn, chạm thì nén + nhắm mắt ^^, nảy nhẹ, khói `Land_Poof` (ETFX "Poof") bật hai bên, nằm sau bi.
- Khi đưa vào game: `GooMerge` dùng `Shader.Find`, nên cần thêm `CasualGame/GooMerge` vào Always Included Shaders (hoặc tham chiếu qua một material) để shader có trong build.

## Kính hũ, impact frame, nét cọ mực (chờ duyệt, chưa đưa vào game)
- **Kính hũ (Merge)**: `Core/GlassJar.cs` + shader `Fx/Shaders/GlassJar.shader`, vẽ trên sorting layer **Glass** (sau Default). Gồm rim sáng ở thành, vệt phản chiếu dọc, tint xanh nhạt, **khúc xạ thật** gần thành (lấy ảnh những gì đã vẽ trên Default rồi bẻ lệch vào tâm), và glint quét chéo khi `Glint()` được gọi (bi đập thành) hoặc định kỳ khi rảnh.
  - Khúc xạ cần **Camera Sorting Layer Texture**. Nó tốn một lần copy toàn màn hình mỗi khung, nên nằm ở renderer riêng `Settings/Renderer2D_Glass.asset` (index 1 trong URP asset). Chỉ camera nào chọn renderer này mới tốn. Menu: **Tools ▸ Casual Game ▸ FX ▸ Setup Glass Renderer**.
  - Demo nút **Jar: drop / fill / clear**: bi vật lý thật, va chạm → `JellyWobble`, đập thành đủ mạnh → glint.
- **Impact frame (Blast)**: `Core/ImpactFrame.cs` + shader `Fx/Shaders/Silhouette.shader`. Đứng hình 2 khung (0.05s mỗi khung): nền giấy + bóng navy, rồi đảo ngược, sau đó mới nổ. Chỉ áp cho SpriteRenderer (particle bị ẩn trong 2 khung đó). Demo nút **Blast 3 lines (impact frame)**: quét sáng → impact frame → `Blast_MultiLine` + rung màn → các khối nổ lan ra.
  - Lưu ý khi đưa vào game: khối của Eye Blast hiện là UI Image (uGUI), không phải sprite. Khi tích hợp cần thêm nhánh silhouette cho `Graphic`.
- **Nét cọ mực (Arrow)**: shader `Fx/Shaders/InkBrush.shader` cho TrailRenderer (Texture Mode = Stretch). Sợi lông cọ chạy dọc vệt, mép gợn, mực đậm ở mép, khô và đứt sợi dần về đuôi. Demo nút **Arrow ink exit**: mũi tên tăng tốc theo đường gấp khúc trên thẻ giấy, ra mép thì phụt `Land_Poof`.

## Effect dùng trong game: Epic Toon FX (bản chính hãng)
Pack nằm ở `Assets/References/Epic Toon FX` (đã nâng lên URP). Menu **Tools ▸ Casual Game ▸ FX ▸ Build Epic Toon FX Picks** tạo **prefab variant** cho từng effect đã chọn trong `Assets/_Game/Fx/Prefabs`. Variant vẫn nối với prefab gốc, được thêm `FxEffect`, effect dạng nổ chuyển thành one-shot, và sorting được đẩy lên trên nhân vật. Riêng effect hợp thể Merge nằm dưới nhân vật để bi mới bật ra từ trong vầng sáng.

| Tên trong game | ETFX gốc | Dùng cho |
|---|---|---|
| `Merge_Fusion_{Pink,Blue,Green,Yellow}` | SparkleExplosion | Hợp thể, chọn theo màu cấp mới |
| `Merge_BigFusion` | StarIntenseExplosionOrange | Hợp thể cấp cao |
| `Blast_BlockPop_{Pink,Blue,Green,Yellow}` | GlitterExplosion | Từng khối khi xóa hàng |
| `Blast_Place` | HitDustExplosion | Đặt khối |
| `Land_Poof` | Poof (Explosions Text) | Bi rơi chạm đáy (khói hai bên, sau bi) |
| `Blast_MultiLine` | FlashExplosionRadial | Xóa nhiều hàng |
| `Combo_Nova` | MagicNovaExplosionYellow | Combo |
| `Arrow_LevelStar` | StarPoof | Qua màn Arrow |
| `Win_Confetti` | ConfettiBlastRainbow | Thắng |
| `Sparkle` | SparkleSoloWhite | Lấp lánh |
| `Fire` | ToonTallFireRed | Lửa |

Trong demo, `{color}` trong tên effect được thay bằng nhóm màu gần nhất (`SandboxArt.Family`). Muốn đổi lựa chọn thì sửa bảng `Picks` trong `Editor/EtfxPicks.cs` rồi chạy lại menu.
Công cụ nghiên cứu: `Assets/References/Editor/FxStudy.cs` (dump thông số, render theo thời gian). Bảng dump ở `Docs/References/etfx_dump.txt`.

## ToonParticle (thử nghiệm, không dùng trong game)
`Assets/_Game/Fx/Shaders/ToonParticle.shader`. Không fade alpha mà **ăn mòn** theo noise, có viền navy (mỏng dần khi tan), 1 mảng bóng cel và ramp tùy chọn.
- Mask hình dạng (`Fx/Textures/mask_*`): puff, star (astroid), ring, drop, shard. Alpha là một field: 0.5 = mép, 1 = lõi. Noise tile 2 octave, ramp 8 hàng màu dải cứng. Tất cả sinh bằng code: **Tools ▸ Casual Game ▸ Toon FX ▸ Rebuild Textures + Materials**.
- Material: `Toon_Puff`, `Toon_PuffRamp`, `Toon_Star`, `Toon_Ring`, `Toon_Drop`, `Toon_Shard`.
- Dữ liệu từng hạt đi qua Custom Vertex Streams: `Custom1.x` là **curve ăn mòn** theo tuổi hạt (0 = hình gốc, ~0.7 = tan hết), `Custom1.y` là độ lệch hàng ramp, `StableRandom.x` làm lệch noise giữa các hạt. Chỉnh curve trong module **Custom Data** của từng ParticleSystem.
- Hệ nào có `behind` thì sort dưới nhân vật (order 2), ví dụ khói hợp thể, để bi mới bật ra từ trong đám khói.

## Prefab nháp ToonParticle (đã cất vào `Assets/_Game/Fx/ToonDrafts`, không hiện trong sandbox)
`Merge_Splat`, `Merge_Droplets`, `Blast_BlockPop`, `Blast_PlaceDust`, `Arrow_ExitPuff`, `Ring_Shock`, `Sparkle_Glint`, `Win_Confetti`, `Fire_Burst`, tất cả dùng ToonParticle.
- **Tools ▸ Casual Game ▸ Toon FX ▸ Rebuild Toon Drafts** sẽ xóa và dựng lại đúng các prefab nháp này. Prefab bạn tự tạo thêm không bị đụng.
- Menu **FX Sandbox** thì không bao giờ ghi đè.
