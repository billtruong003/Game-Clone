# In-app products (Google Play), v1.0

Shared plan for Bruh Arrows, Nah Blocks and Meh Merge. Source of truth for what to create in Play Console
(Monetize → Products → In-app products) and what the code's product ids must match. Spec: Docs/FEATURE_SPEC.md §1b (SH5).

Rules (FEATURE_SPEC §1b and Play policy):
- Every product is **non-consumable** (bought once, kept forever, restored after reinstall). No consumables, no coin packs,
  no random rewards, no subscriptions.
- Coins are earned by playing only. They buy colour sets; premium sets are real-money only.
- Each app has its own product list, so the same id (`remove_ads`) is reused in all three apps.
- A product id can never be reused once created, even after deletion: create them exactly as written here.
- Prices are set in USD and Play converts them to local prices; check the VND price before activating (aim for round numbers).

## Products

| Product id | Apps | Price (USD) | What it unlocks |
|---|---|---|---|
| `remove_ads` | all 3 | **2.99** ← TODO confirm (spec gives no price) | Turns off between-round ads. Rewarded ads stay optional. Already in code (`GameConfig.removeAdsProductId`). |
| `all_skins` | all 3 | 4.99 | Every premium set and every coin colour set of that game, plus sets added later. |
| `all_skins_noads` | all 3 | 6.99 | `all_skins` + `remove_ads`. Owning it counts as owning both. |
| `skin_billiard` | Meh Merge | 1.99 | Billiard balls set |
| `skin_sports` | Meh Merge | 1.99 | Sports balls set |
| `skin_eyeballs` | Meh Merge | 1.99 | Eyeballs set |
| `skin_planets` | Meh Merge | 1.99 | Planets set |
| `skin_retro_bricks` | Nah Blocks | 1.99 | Retro Bricks set |
| `skin_pixel` | Nah Blocks | 1.99 | Pixel set |
| `skin_toy_studs` | Nah Blocks | 1.99 | Toy Studs set |
| `skin_gems` | Nah Blocks | 1.99 | Gems set |
| `theme_blueprint` | Bruh Arrows | 1.99 | Blueprint theme |
| `theme_chalkboard` | Bruh Arrows | 1.99 | Chalkboard theme |
| `theme_terminal` | Bruh Arrows | 1.99 | Terminal theme |

Counts: Meh Merge 7 products, Nah Blocks 7, Bruh Arrows 6.

## Store texts (Play Console product name ≤55, description ≤200)

| Product id | Name EN | Description EN | Tên VI | Mô tả VI |
|---|---|---|---|---|
| `remove_ads` | Remove ads | No more ads between rounds. Optional reward videos stay available. | Gỡ quảng cáo | Không còn quảng cáo giữa các ván. Video có thưởng vẫn còn nếu bạn muốn xem. |
| `all_skins` | All Skins | Unlock every skin in the game, including future sets. | Trọn bộ skin | Mở mọi skin trong game, kể cả các bộ ra sau này. |
| `all_skins_noads` | All Skins + No Ads | Every skin, future sets included, and no ads between rounds. | Trọn bộ skin + Gỡ QC | Mọi skin, kể cả bộ sau này, và không còn quảng cáo giữa các ván. |
| `skin_billiard` | Billiard set | Turn every ball into a pool ball. Still deadpan. | Bộ Bi-a | Biến mọi quả bóng thành bi-a. Vẫn mặt lạnh. |
| `skin_sports` | Sports set | From marble to beach ball: merge your way through the sports shelf. | Bộ Thể thao | Từ bi ve tới bóng bãi biển: gộp hết cả kệ thể thao. |
| `skin_eyeballs` | Eyeballs set | The balls are eyes now. They are watching you merge. | Bộ Nhãn cầu | Bóng giờ là con mắt. Tụi nó đang nhìn bạn gộp. |
| `skin_planets` | Planets set | Merge your way from the Moon to the Sun. | Bộ Hành tinh | Gộp từ Mặt Trăng tới Mặt Trời. |
| `skin_retro_bricks` | Retro Bricks set | Bevelled bricks and a black well, straight out of the 80s. | Bộ Gạch cổ điển | Gạch vát cạnh, giếng đen, đúng chất thập niên 80. |
| `skin_pixel` | Pixel set | 8-bit blocks with 8-bit attitude. | Bộ Pixel | Khối 8-bit, thái độ 8-bit. |
| `skin_toy_studs` | Toy Studs set | Plastic toy blocks with studs on top. | Bộ Khối đồ chơi | Khối nhựa đồ chơi có núm. |
| `skin_gems` | Gems set | Cut gems instead of blocks. Still say nah. | Bộ Đá quý | Đá quý cắt giác thay cho khối. Vẫn nói nah. |
| `theme_blueprint` | Blueprint theme | White lines on engineer blue. | Theme Bản vẽ | Nét trắng trên nền xanh bản vẽ kỹ thuật. |
| `theme_chalkboard` | Chalkboard theme | Chalk arrows on a school board. | Theme Bảng phấn | Mũi tên phấn trên bảng đen. |
| `theme_terminal` | Terminal theme | Green-on-black, like it's 1985. | Theme Terminal | Chữ xanh nền đen, như năm 1985. |

## Before the products can be created (Play Console order)

1. Payments profile / merchant account linked to the developer account (needed for any paid product).
2. Upload one build that contains the billing permission (Unity IAP adds `com.android.vending.BILLING`) to any testing track.
   Play does not allow creating in-app products until such a build exists.
3. Create the products above (non-consumable = "one-time product"), set prices, activate.
4. Add license testers (Setup → License testing) so test purchases are free and can be refunded.

## Code work still to do (part of the Shop build, FEATURE_SPEC SH2–SH5)

- `IapStore` handles one product today (`remove_ads`). It needs the full list per game (from `SkinCatalog`), restore on start,
  and the bundle rules: `all_skins_noads` ⇒ owns `all_skins` + `remove_ads`; `all_skins` ⇒ owns every set present and future.
- Prices shown in the shop must come from Google Play (localized string), never hard-coded.
- Ownership is cached locally (SaveStore) but always re-checked against Play on start, so a refund removes the item.
- Build checks (`BuildSwitcher` release check): every catalog product id exists in this file's list for that game.
