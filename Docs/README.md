# Casual Game — 3 game trong 1 project

| Scene | Game | Ghi chú |
|---|---|---|
| `Hub` | Menu dev chọn game | Bỏ khỏi build khi tách từng app |
| `ArrowOut` | Arrow Out — game trí tuệ | Home, Chọn màn (100 màn), Vô hạn, Daily, gợi ý, hướng dẫn |
| `EyeBlast` | Eye Blast — xếp khối 8×8 | Chỉ màn gameplay + tạm dừng |
| `EyeMerge` | Eye Merge — thả & hợp thể | Chỉ màn gameplay + tạm dừng |

Mở `Assets/_Game/Scenes/Hub.unity` → Play. Game View: `Tools → Casual Game → Game View Portrait 1080x1920`.

## Cấu trúc
```
Assets/_Game/
  Art/Sheets/      faces · shapes · ui · fx  (.png + .json, tự cắt khi import)
  Art/Fonts/       Baloo 2 Bold (OFL, đủ dấu tiếng Việt) + TMP font asset
  Audio/Sfx|Music  Kenney CC0 (tạm) — xem Docs/SUNO_PROMPTS.md
  Resources/       ArtLibrary, AudioLibrary, FxMaterial, ArrowOut/levels.json (tự sinh)
  Scripts/Core     save, cài đặt, âm thanh, Ads/IAP (giả lập), tween, UIKit, Popup, EmojiFace, Fx
  Scripts/ArrowOut · EyeBlast · EyeMerge
  Scripts/Editor   SheetSlicer, ProjectSetup, ArrowLevelBaker, SceneBuilder, TestRunTool
  Tests/EditMode   luật Arrow Out: không kẹt, kỹ năng có tác dụng, 100 màn giải được & tăng dần
Tools/art/         công cụ vẽ asset (Node + sharp)
```

## Menu `Tools → Casual Game`
| Mục | Làm gì |
|---|---|
| Project Setup | Tạo AudioLibrary, material particle, khóa màn hình dọc |
| Reslice All Sheets / Rebuild Art Library | Cắt lại sheet, dựng lại danh sách sprite + emoji |
| Rebuild Audio Library | Sau khi thêm/đổi file âm thanh |
| Arrow Out → Bake 100 Levels | Sinh lại 100 màn (seed cố định) |
| Build Scenes | Tạo lại 4 scene + Build Settings |
| Run EditMode Tests | Chạy test, kết quả ở `Temp/test-results.txt` |

## Art: tự vẽ → ChatGPT
Asset hiện tại là bản tạm do `Tools/art/make-art.mjs` vẽ (SVG → PNG → sheet + JSON).
- **Thay bằng art ChatGPT:** đặt PNG cùng tên vào `Tools/art/override/<sheet>/<tên>.png`
  (ví dụ `override/faces/face_love_2.png`, `override/ui/btn_green.png`), rồi chạy `npm run build` trong `Tools/art`.
  Ảnh được co về đúng kích thước ô cũ → layout sheet và cách cắt trong Unity giữ nguyên, code không đổi.
- Danh sách tên sprite: xem `Assets/_Game/Art/Sheets/<sheet>.json` hoặc ảnh xem trước `Tools/art/preview/`.
- **Tái sử dụng sprite:** thân tròn/vuông và mảnh mũi tên là ảnh TRẮNG, tô màu trong Unity; mặt emoji là lớp riêng
  dùng chung cho Blast + Merge; nút, bảng, hũ, khung là 9-slice.

## Emoji
14 biểu cảm × 4 khung: happy, grin, laugh, wink, love, cool, surprised, sleepy, silly, starstruck, angry, cry, shy, dizzy.
Mỗi nhân vật chọn ngẫu nhiên (trừ angry/cry/dizzy — dành cho phản ứng thua/nguy hiểm) và chạy lệch pha.
`EmojiFace.React("laugh", 1.2f)` để đổi biểu cảm tạm thời.

## Kiếm tiền (đang giả lập)
`Ads` (rewarded: hồi sinh, gợi ý, +1 tim · interstitial: sau thua/qua màn, bỏ 2 lần đầu, cách nhau ≥ 90 s) và
`Store.BuyRemoveAds` hiện là overlay giả lập. Khi tích hợp thật: viết `IAdProvider` cho LevelPlay/AdMob và thay
`Ads.Provider`; `Store` nối Unity IAP / Play Billing với product `remove_ads`.

## License asset
Kenney (CC0) · Baloo 2 (SIL OFL 1.1) · art còn lại tự vẽ. Không dùng asset từ nguồn chia sẻ lậu.
