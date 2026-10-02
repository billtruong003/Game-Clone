# In-app products (Google Play), v1.0

Shared plan for Bruh Arrows, Nah Blocks and Meh Merge. Source of truth for what to create in Play Console
(Monetize → Products → In-app products) and what the code's product ids must match. Spec: Docs/FEATURE_SPEC.md §1b (SH5),
pricing approved 2026-10-01 (mockup page "Skin VFX & Pricing").

Rules (FEATURE_SPEC §1b and Play policy):
- Every product is **non-consumable** (bought once, kept forever, restored after reinstall). No consumables, no coin packs,
  no random rewards, no subscriptions.
- Coins are earned by playing only and are never sold. A colour set can be earned with coins for free **or** unlocked at once
  for $0.99 (skip the grind). Premium sets are real money only. Each game's Neon set unlocks by watching 3 rewarded videos.
- **Full Game** ($6.99) = every skin of that game, sets added later included, and no ads between rounds. Buying everything one
  by one costs far more (Meh Merge $23.82, Nah Blocks $21.84, Bruh Arrows $18.86), so the bundle is the obvious deal.
- Each app has its own product list, so the same id (`remove_ads`, `color_candy`) is reused across the three apps.
- A product id can never be reused once created, even after deletion: create them exactly as written here.
- Prices are set in USD and Play converts them to local prices; check the VND price before activating (aim for round numbers).

## Bundles and ads (all 3 games)

| Product id | Price (USD) | What it unlocks |
|---|---|---|
| `all_skins_noads` | **6.99** | **Full Game**: every skin of the game (future sets too) + `remove_ads`. Owning it counts as owning everything below. |
| `all_skins` | 4.99 | Every skin of the game (future sets too); keeps the ads. |
| `remove_ads` | 2.99 | Turns off between-round ads. Rewarded ads stay optional. Already in code (`GameConfig.removeAdsProductId`). |

## Meh Merge (15 products + 3 bundles)

| Product id | Price | Unlocks | Also by |
|---|---|---|---|
| `skin_billiard` | 1.99 | Billiard balls (premium) | — |
| `skin_sports` | 1.99 | Sports balls (premium) | — |
| `skin_eyeballs` | 1.99 | Eyeballs (premium) | — |
| `color_candy` | 0.99 | Candy ball colours | 300 coins |
| `color_ocean` | 0.99 | Ocean ball colours | 300 coins |
| `color_forest` | 0.99 | Forest ball colours | 400 coins |
| `color_sunset` | 0.99 | Sunset ball colours | 400 coins |
| `color_mono` | 0.99 | Mono ball colours | 500 coins |
| `color_pastel` | 0.99 | Pastel ball colours | 500 coins |
| `stage_frosted` | 0.99 | Frosted stage | 300 coins |
| `stage_amber` | 0.99 | Amber stage | 300 coins |
| `stage_mint` | 0.99 | Mint stage | 400 coins |
| `stage_night` | 0.99 | Night stage | 500 coins |
| `face_sleepy` / `face_grumpy` / `face_derp` | 0.99 each | Face packs | 400 / 500 / 600 coins |

Neon balls: 3 rewarded videos, no product. One by one: $21.83 → Full Game $6.99. (More interactive premium sets to come.)

## Nah Blocks (13 products + 3 bundles)

| Product id | Price | Unlocks | Also by |
|---|---|---|---|
| `skin_retro_bricks` | 1.99 | Retro Bricks (premium) | — |
| `skin_toy_bricks` | 1.99 | Toy Bricks, pastel plastic (premium) | — |
| `skin_gems` | 1.99 | Gems (premium) | — |
| `color_candy` | 0.99 | Candy blocks | 300 coins |
| `color_ocean` | 0.99 | Ocean blocks | 300 coins |
| `color_retro` | 0.99 | Retro green blocks | 400 coins |
| `color_mono` | 0.99 | Mono blocks | 400 coins |
| `color_pastel` | 0.99 | Pastel blocks | 500 coins |
| `color_jelly` | 0.99 | Jelly blocks | 500 coins |
| `board_paper` | 0.99 | Paper board | 300 coins |
| `board_midnight` | 0.99 | Midnight board | 400 coins |
| `face_sleepy` / `face_grumpy` / `face_derp` | 0.99 each | Face packs | 400 / 500 / 600 coins |

Neon blocks: 3 rewarded videos. One by one: $19.85 → Full Game $6.99. (Pixel dropped.)

## Bruh Arrows (15 products + 3 bundles)

| Product id | Price | Unlocks | Also by |
|---|---|---|---|
| `theme_blueprint` | 1.99 | Blueprint theme (premium) | — |
| `theme_chalkboard` | 1.99 | Chalkboard theme (premium) | — |
| `theme_vector` | 1.99 | Vector CRT theme (premium) | — |
| `theme_hologram` | 1.99 | Hologram theme (premium) | — |
| `theme_neon` | 1.99 | Neon theme (premium) | — |
| `color_candy` | 0.99 | Candy arrows | 300 coins |
| `color_ocean` | 0.99 | Ocean arrows | 300 coins |
| `color_forest` | 0.99 | Forest arrows | 400 coins |
| `color_mono` | 0.99 | Mono arrows | 400 coins |
| `paper_grid` | 0.99 | Grid paper | 300 coins |
| `paper_kraft` | 0.99 | Kraft paper | 400 coins |
| `paper_night` | 0.99 | Night paper | 500 coins |
| `face_sleepy` / `face_grumpy` / `face_derp` | 0.99 each | Face packs | 400 / 500 / 600 coins |

Neon arrows: 3 rewarded videos. One by one: $22.84 → Full Game $6.99.

## Store texts (Play Console product name ≤55, description ≤200)

| Product id | Name EN | Description EN | Tên VI | Mô tả VI |
|---|---|---|---|---|
| `remove_ads` | Remove ads | No more ads between rounds. Optional reward videos stay available. | Gỡ quảng cáo | Không còn quảng cáo giữa các ván. Video có thưởng vẫn còn nếu bạn muốn xem. |
| `all_skins` | All Skins | Unlock every skin in the game, including future sets. | Trọn bộ skin | Mở mọi skin trong game, kể cả các bộ ra sau này. |
| `all_skins_noads` | Full Game | Every skin, future sets included, and no ads between rounds. One price, everything. | Trọn bộ game | Mọi skin, kể cả bộ sau này, và không còn quảng cáo giữa các ván. Một lần, có hết. |
| `skin_billiard` | Billiard set | Turn every ball into a pool ball. Still deadpan. | Bộ Bi-a | Biến mọi quả bóng thành bi-a. Vẫn mặt lạnh. |
| `skin_sports` | Sports set | From marble to beach ball: merge your way through the sports shelf. | Bộ Thể thao | Từ bi ve tới bóng bãi biển: gộp hết cả kệ thể thao. |
| `skin_eyeballs` | Eyeballs set | The balls are eyes now. They are watching you merge. | Bộ Nhãn cầu | Bóng giờ là con mắt. Tụi nó đang nhìn bạn gộp. |
| `skin_retro_bricks` | Retro Bricks set | Bevelled bricks and a black well, straight out of the 80s. | Bộ Gạch cổ điển | Gạch vát cạnh, giếng đen, đúng chất thập niên 80. |
| `skin_toy_bricks` | Toy Bricks set | Pastel plastic bricks with studs on top. | Bộ Khối đồ chơi | Khối nhựa pastel có núm. |
| `skin_gems` | Gems set | Cut gems instead of blocks. Still say nah. | Bộ Đá quý | Đá quý cắt giác thay cho khối. Vẫn nói nah. |
| `theme_blueprint` | Blueprint theme | White lines on engineer blue. | Theme Bản vẽ | Nét trắng trên nền xanh bản vẽ kỹ thuật. |
| `theme_chalkboard` | Chalkboard theme | Chalk arrows on a school board. | Theme Bảng phấn | Mũi tên phấn trên bảng đen. |
| `theme_vector` | Vector CRT theme | Glowing beams drawn on an old vector monitor. | Theme Màn vector | Tia sáng vẽ trên màn hình vector đời cũ. |
| `theme_hologram` | Hologram theme | See-through arrows with scanlines and a flicker. | Theme Hologram | Mũi tên trong suốt, có vạch quét và chớp nháy. |
| `theme_neon` | Neon theme | White-hot neon tubes on a dark wall. | Theme Neon | Ống neon sáng rực trên tường tối. |
| `color_<name>` | <Name> colours | Unlock the <Name> colour set now instead of saving coins. | Màu <Tên> | Mở bộ màu <Tên> ngay, khỏi gom xu. |
| `stage_<name>` / `board_<name>` / `paper_<name>` | <Name> stage / board / paper | Unlock it now instead of saving coins. | Sân khấu / Bàn / Giấy <Tên> | Mở ngay, khỏi gom xu. |
| `face_<name>` | <Name> faces | A new set of faces for every character. | Bộ mặt <Tên> | Bộ mặt mới cho mọi nhân vật. |

## Before the products can be created (Play Console order)

1. Payments profile / merchant account linked to the developer account (needed for any paid product).
2. Upload one build that contains the billing permission (Unity IAP adds `com.android.vending.BILLING`) to any testing track.
   Play does not allow creating in-app products until such a build exists.
3. Create the products above (non-consumable = "one-time product"), set prices, activate.
4. Add license testers (Setup → License testing) so test purchases are free and can be refunded.

## Code work still to do (part of the Shop build, FEATURE_SPEC SH2–SH5)

- `IapStore` must take the full list per game (from `SkinCatalog`), restore on start, and apply the bundle rules:
  `all_skins_noads` ⇒ owns `all_skins` + `remove_ads`; `all_skins` ⇒ owns every set present and future; a colour set is
  owned if bought with coins **or** with its product.
- Prices shown in the shop must come from Google Play (localized string), never hard-coded.
- Ownership is cached locally (SaveStore) but always re-checked against Play on start, so a refund removes the item.
- Build checks (`BuildSwitcher` release check): every catalog product id exists in this file's list for that game.
