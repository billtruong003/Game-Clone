# Game Clone — 3 standalone casual games in one Unity project

Unity 6000.3.10f1 · URP 2D · portrait 1080×1920.

| Scene | Game |
|---|---|
| `ArrowOut` | Arrow Out — brain puzzle, 100 levels + endless |
| `EyeBlast` | Eye Blast — 8×8 block puzzle |
| `EyeMerge` | Eye Merge — drop & merge (Suika-like) |
| `Sandbox/FxSandbox` | FX design bench (goo merge, jelly wobble, glass jar, impact frame, ink brush) |

Full docs (Vietnamese): [Docs/README.md](Docs/README.md), FX: [Docs/FX_SANDBOX.md](Docs/FX_SANDBOX.md).
Next phase (glass fixes, asset regen, UI mockups, per-game themes): [Docs/NEXT_PHASE.md](Docs/NEXT_PHASE.md).

## Building a game (Build Switcher)
Each game is its own app. `Tools → Casual Game → Build Switcher` (Ctrl+Shift+B) holds one **GameProfile** per game
(`Assets/_Game/Build/Profiles`): product name, package name, version + versionCode, icon, scenes, art sheets, sounds,
AppLovin MAX ad unit ids, IAP product id, privacy URL and the build history. **Studio settings** hold what all games
share (company, package prefix, target/min API, upload keystore path, MAX SDK key).

- **Apply**: rewires Build Settings, Player Settings (IL2CPP, ARM64, target API, keystore), defines, the game's
  libraries and its runtime `GameConfig` (what the game reads at runtime: ad ids, privacy URL…).
- **Test APK**: development build with fake ads. **Release AAB**: signed bundle for Play with real ads; blocked until
  validation passes; a successful release is logged in the profile's history and bumps versionCode.
- **New game**: creates a profile + scene with its own package name and define.
- Keystore passwords are typed in the window each session and never saved.
- From code: `BuildSwitcher.BuildById("ArrowOut", release: false)`.

## After cloning: import Epic Toon FX
The game effects are prefab variants of **Epic Toon FX** (paid, Unity Asset Store). Its license does not allow
redistribution, so the pack is **not in this repo**. Import it from the Asset Store (Package Manager → My Assets)
into `Assets/References/Epic Toon FX`, then run the bundled `Upgrade/ETFX URP Upgrade (6000.0.11f1).unitypackage`.
The variants in `Assets/_Game/Fx/Prefabs` reconnect by GUID. `Tools → Casual Game → FX → Build Epic Toon FX Picks`
rebuilds them.

Other third-party content is CC0 (Kenney, Brackeys VFX) or OFL (Baloo 2 font).
