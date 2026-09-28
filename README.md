# Game Clone — 3 casual games in one Unity project

Unity 6000.3.10f1 · URP 2D · portrait 1080×1920.

| Scene | Game |
|---|---|
| `Hub` | Dev menu to pick a game |
| `ArrowOut` | Arrow Out — brain puzzle, 100 levels + endless |
| `EyeBlast` | Eye Blast — 8×8 block puzzle |
| `EyeMerge` | Eye Merge — drop & merge (Suika-like) |
| `Sandbox/FxSandbox` | FX design bench (goo merge, jelly wobble, glass jar, impact frame, ink brush) |

Full docs (Vietnamese): [Docs/README.md](Docs/README.md), FX: [Docs/FX_SANDBOX.md](Docs/FX_SANDBOX.md).

## After cloning: import Epic Toon FX
The game effects are prefab variants of **Epic Toon FX** (paid, Unity Asset Store). Its license does not allow
redistribution, so the pack is **not in this repo**. Import it from the Asset Store (Package Manager → My Assets)
into `Assets/References/Epic Toon FX`, then run the bundled `Upgrade/ETFX URP Upgrade (6000.0.11f1).unitypackage`.
The variants in `Assets/_Game/Fx/Prefabs` reconnect by GUID. `Tools → Casual Game → FX → Build Epic Toon FX Picks`
rebuilds them.

Other third-party content is CC0 (Kenney, Brackeys VFX) or OFL (Baloo 2 font).
