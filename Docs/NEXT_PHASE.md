# Ghi chú sau vòng FX, việc cho phase tiếp theo (2026-09-28)

## Đánh giá hiện tại
- Visual tổng thể đã ổn: goo merge, jelly wobble, kính hũ, impact frame, nét cọ mực, effect Epic Toon FX. Mục tiêu tiếp theo là làm mọi thứ **tự nhiên hơn nữa**.
- **Mắt, mũi, miệng (emoji face) giữ nguyên sprite như hiện tại.** Phần này đang tốt, không đổi sang rig hay shader.

## 1. Kính hũ Eye Merge: chỉnh lại cho đúng chất toon
| Việc | Chi tiết | Chỗ sửa |
|---|---|---|
| Tắt khúc xạ | Bỏ refraction. Khi đã tắt thì cũng bỏ luôn renderer riêng `Settings/Renderer2D_Glass.asset` (Camera Sorting Layer Texture) để không tốn một lần copy toàn màn hình mỗi khung. Camera EyeMerge quay về renderer mặc định. | `GlassJar.shader` (`_UseRefraction` = 0), `BuildSwitcher.WireGameSpecific`, `GlassRendererSetup` |
| Bo góc 0.9 → **0.7** | Bán kính góc đáy/miệng của lớp kính | `EyeMergeGame.JarCorner`, `GlassJarDemo.cornerRadius` |
| Bỏ phần "smooth" | Kính toon trơn: mảng phẳng, cạnh sắc, **không gradient**. Bỏ rim chuyển màu mềm và vệt phản chiếu mờ dần (fake reflection kiểu smooth). Nếu giữ highlight thì là mảng phẳng cạnh cứng. | `GlassJar.shader` (rim, streak, glint) |
| `jar_line` | Chỉ vẽ **hũ viền đen** (outline ngoài). **Bỏ đường line bên trong**: hồi trước cần, giờ không cần nữa. | `make-art.mjs` (sprite `jar_line`), `EyeMergeGame.BuildJar` |

## 2. Gen lại asset
- Toàn bộ asset (thân nhân vật, UI, nền, hũ, khay…) cần vẽ lại. Mắt/mặt giữ nguyên (mục trên).
- Không dùng ChatGPT (đã loại). Tự vẽ bằng `Tools/art`, hoặc lấy pack có license thương mại cho thứ nhiều chi tiết.

## 3. Phase mockup lại UI toàn bộ game
- Mockup lại UI cho cả 3 game trước khi sửa code UI.
- **Loại bỏ Hub chính thức.** Mỗi game là một app độc lập, không còn menu chọn game. Những chỗ đang dính Hub:
  - `Core/HubMenu.cs`, scene `Hub.unity`, `SceneBuilder` (dòng `("Hub", …HubMenu)`).
  - Nút home/"Về menu" gọi `SceneFlow.Load("Hub")`: `ArrowOutGame.cs:75`, `EyeBlastGame.cs:79` và `:401`, `EyeMergeGame` (popup tạm dừng).
  - Build Switcher: profile **Dev** (`isDev`, scenes gồm Hub), `ArtLibrary_All` / `AudioLibrary_All` / `FxCatalog_Dev` dùng cho Hub.
- **Mỗi game một theme màu khác nhau** để đánh giá phối màu và nét line (độ dày nét, màu viền, tương phản).
- Phương án thay thế: không vẽ màu sẵn vào sprite, mà **tô màu bằng shader**. Sprite trắng/xám + toon shader dùng **URP 2D Light** (Light2D), nên màu, bóng và ánh sáng do shader + đèn quyết định. Đổi theme chỉ cần đổi palette và đèn. Cần mockup thử cả hai hướng (màu vẽ sẵn và shader + 2D light) để chọn.

## 4. Repo
- Toàn bộ project ở https://github.com/billtruong003/Game-Clone.
- Epic Toon FX (bản chính hãng, cài từ Unity Asset Store) nằm ở `Assets/References/Epic Toon FX`. Xem README gốc để biết cách đưa pack vào khi clone.
