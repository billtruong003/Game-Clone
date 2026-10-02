# Google Play: publish checklist for a personal (individual) account

Bruh Arrows, Nah Blocks, Meh Merge · Bill The Dev · reviewed 2026-10-02.
Each item says what to set in Play Console and why. ⚠️ = a decision or a risk to look at before submitting.

## 1. Vietnam

- Vietnam needs a G1 licence for online games (Nghị định 147/2024, in force 25/12/2024). An individual cannot get one, and
  stores must block unlicensed games for Vietnamese users. **Exclude Vietnam** in every track:
  Production → Countries/regions, and the same list on the closed testing track.
- Publish only the **en-US** listing. The vi-VN texts in `listings/*.md` are kept for reference, marked "Không đăng".
- "Available in English and Vietnamese" was removed from the descriptions.
- ⚠️ **Testers in Vietnam:** a closed test limited to non-Vietnam countries cannot be installed by testers in Vietnam.
  Either recruit the 12 testers outside Vietnam, or allow Vietnam on the closed track only (not public) and remove it
  before production. The second option is common, but it is still distribution to Vietnamese users: it is your call.
- The game still switches to Vietnamese on a Vietnamese phone. A language is not a market, so this is allowed. Remove it in
  a later update if you want zero Vietnam signals.

## 2. The account (most terminations start here)

- ⚠️ **One real person behind everything.** The account's identity verification, the payments profile and the legal name
  must be the same person. The payments profile is in the name **TRƯƠNG NGỌC CHÂU**: that person is the legal developer.
  A mismatch with the verified identity is a classic reason for a suspended account.
- ⚠️ **Your address becomes public.** A personal account that sells in-app purchases shows its full address on Google Play
  (taken from the payments profile). Because the games earn from ads and IAP you are a **trader** under the EU Digital
  Services Act: answer "trader", and EU users see name, address, phone and email.
- Never open a second account to get around a problem. Google links accounts by identity, payment method and device;
  one termination takes every linked account down.
- Keep the account email and phone reachable: policy warnings go there with deadlines.

## 3. 12 testers × 14 days (new personal account)

- Applies **per app**: each of the 3 games needs its own closed test with ≥ 12 testers opted in **continuously** for
  14 days. The same 12 people can join all 3 tests. Someone who leaves and rejoins restarts their own 14 days.
- Testers must really play (Google looks at engagement). Ask them for feedback and keep it: the production application
  asks how you recruited, whether testers used all features, what feedback you got and what you changed.
- Answer the production application honestly and specifically (target audience: teens and adults who like short puzzle
  sessions; what is unique: reacting deadpan faces, skins that change the look only, no timers).
- Suggestion: start all 3 closed tests together, apply for production one game at a time a few days apart.

## 4. Spam, repetitive content, "AI-looking" apps

Google does not ban AI-assisted development. What gets accounts removed is **low-effort, repetitive or template spam**:
"multiple apps with highly similar functionality, content, and user experience", copied content, or apps with very
little content.

- ✅ The 3 games are different genres (physics merge, block placement, arrow logic) with their own content (100 levels,
  endless, daily; 11 tiers; combos), so they are not reskins of each other. Sharing a shop and settings shell is normal.
- ✅ No webview wrappers, no keyword spam, no fake reviews.
- Don't publish more variants of the same games, and don't release many apps in a short burst from a new account.
- Store listing must look made by a person: real gameplay screenshots of the current build, own icon (done), a short
  honest description in your own voice (done; no "#1", "best", "top", no emoji in the title, no ALL CAPS).
- ⚠️ Screenshots in `screenshots/` are from 29 Sep; the UI changed since (shop, faces). Use the `v2/` ones or recapture
  from the 1.0.0 build so what players see matches what they get.
- Reply to early reviews yourself. An active developer page is a strong "real person" signal.
- The public GitHub repo contains AI prompt notes (Docs/*PROMPT*.md). Google does not scan GitHub, but if you want no
  trace, make the repo private.

## 5. Store listing (metadata policy)

- Titles ≤ 30 chars, no emoji/caps: `Meh Merge: Drop & Merge`, `Nah Blocks: Block Puzzle`, `Bruh Arrows: Arrow Puzzle`. OK.
- Descriptions now list the premium sets that exist: Merge Billiard / Sports / Eyeballs; Blocks Retro Bricks / Toy
  Bricks / Gems; Arrows Blueprint / Chalkboard / Vector CRT / Hologram / Neon. Update them whenever the shop changes.
- No prices or promos in screenshots or the feature graphic ("free", "50% off"), no rankings, no other apps' names.
- Category: Games → Puzzle. Contact email = the one in the privacy policy.

## 6. App content forms

| Form | Answer |
|---|---|
| Privacy policy | `https://billtruong003.github.io/billthedev-legal/<game>.html` (all 3 return 200, contact email inside) |
| App access | All functionality available without special access (no login) |
| Ads | Yes, the app contains ads |
| Advertising ID | Yes, used for Advertising (AdMob adds the AD_ID permission) |
| Content rating (IARC) | All content questions No; digital purchases Yes; expected PEGI 3 / Everyone + In-game purchases |
| Target audience | 13–15, 16–17, 18+ (not under 13) |
| Appeals to children? | ⚠️ see 7 |
| Data safety | Exactly as `DATA_SAFETY.md` (ad SDK data declared as collected + shared) |
| News / government / financial / health | No |

## 7. Children (Families policy)

- Declaring 13+ is not enough if the store listing or the game looks made for kids: Google can review and reclassify.
  The cute faces are a risk factor; the humour and wording (Meh, Nah, Bruh, deadpan) are teen/adult, which helps.
- Keep the listing free of kid language ("for kids", "toddler", "learn") and keep the wording dry.
- If Google decides the game also appeals to children: AdMob and Unity Ads are Families Self-Certified SDKs, but
  personalised ads must be turned off for users of unknown age (then add an age question at first launch). Do this only
  if Google asks.

## 8. Ads and IAP (checked in code)

- Ads: consent (UMP) before any ad request; Privacy options button in Settings when required; interstitials only at
  breaks after the player taps Next / Play again, none in the first 2 breaks, max one per 90 s, none after a rewarded ad;
  rewarded always player-initiated; Remove ads stops interstitials (the listing says "the ads between games", which is
  accurate because optional videos remain).
- IAP: Google Play Billing only (Unity IAP 5.4.3, Billing 8), all non-consumable, acknowledged at once, restored and
  refund-revoked automatically; product ids match `IAP_PRODUCTS.md` exactly.
- No coin packs for real money, no loot boxes: keep it that way (gambling-like mechanics change the rating and the review).
- Unity Ads is inside through AdMob mediation: either set it up in AdMob or leave it (no fill, harmless). It is already
  declared in the privacy policy and Data safety.
- app-ads.txt is not published: AdMob shows a warning and some buyers skip the inventory. Not a policy violation.

## 9. Assets and licences

- Sound effects in the repo: Kenney (CC0). Music and extra SFX: Unity Asset Store packs ("Casual Game Sounds",
  "Season Cycle Casual Gaming Music Pack") and FX from Epic Toon FX: the Asset Store EULA allows them inside a game;
  they are git-ignored, never pushed to the public repo. Font: Baloo 2 (OFL). Art: drawn by us (Tools/art).
- Keep the Asset Store invoices/emails: they are your proof of licence if anyone files a copyright complaint.

Sources: Play Console Help (Spam policy, Metadata, Families, App testing requirements for new personal accounts,
developer account information), Nghị định 147/2024/NĐ-CP coverage (MST/MIC, Tuổi Trẻ), EU DSA trader status guidance.
