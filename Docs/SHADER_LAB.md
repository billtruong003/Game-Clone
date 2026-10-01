# Shader & theme lab (2026-10-01)

**Why this exists.** The owner wants premium themes, Bruh Arrows above all, to be driven by shaders instead of
redrawn sprites. They named ink drawing, chalkboard, CRT pillow, eyes and holograms as examples. This lab tests which
shader mechanics work on the game's real arrow mesh before any theme is chosen.

**How to open it.** In Unity, go to Tools → Casual Game → Theme Lab (scene `Assets/_Game/Scenes/Lab/ThemeLab.unity`),
then press Play. Right/Left or a tap changes the theme. Each theme loops: the arrows draw themselves on, hold, then fly
out one by one.

Code locations:
- `Scripts/Lab/ThemeLab.cs`: the lab.
- `Fx/Shaders/Lab/*.shader` + `ArrowLabCommon.hlsl`: the shaders.
- `ArrowStroke`: now writes shader UVs.

## What the arrow mesh gives a shader

`ArrowStroke` builds one continuous mesh per arrow. It now also fills these UVs (the default look is unchanged,
because the default UI shader ignores them):

| channel | meaning | used for |
|---|---|---|
| uv0.x | distance from the tail along the line, in cells | dashes, ticks, bristle streaks, terminal blocks |
| uv0.y | across the line, ±1 at the line edge | soft / ragged edges, rims, outline-only, glow falloff |
| uv1.x | 0 at the tail → 1 at the head | draw-on reveal (`_Reveal`), dry tail, wet front |
| uv1.y | 1 on the chevron | keep dashes/blocks off the head, blinking cursor |
| `GlowPad` (cells) | widens the mesh past the line edge, fully opaque | halos for neon / hologram / phosphor |

The canvas needs `AdditionalCanvasShaderChannels.TexCoord1`. The lab sets it; the game canvas will need the same flag.

## Mechanics researched

The ideas come from the sources listed at the end.

1. **Draw-on reveal.** Uses uv1.x against `_Reveal`. This is the SVG "line animation" idea: distance along the path
   as a vertex attribute. It gives every theme a "being drawn" entrance for free and costs nothing.
2. **Ink bleed / brush.** Noise wobbles the edge radius along the line, and ink pools darker at the edge. A dry brush
   drops bristles toward the tail, and a wet bead sits at the brush front (after Northway's ink-bleed fade and pencil
   sketch shaders).
3. **Chalk grain.** Screen-space noise compared against a "pressure" that is high in the middle and low at the edges,
   plus an eroded border and a dust halo. Grain in screen space keeps the dust size the same at every zoom.
4. **Hologram.** The bright edge (fresnel on 3D) becomes `pow(|uv0.y|)` on a flat line. Screen-space scanlines,
   scrolling light bands like the striped-animal reference, random flicker, and a halo in the glow pad (Daniel Ilett
   and Unity Learn hologram tutorials).
5. **Neon tube.** White-hot core, saturated tube, exponential glow falloff, slow breathing.
6. **Blueprint / drafting.** Outline only (two thin lines at the edge), dashed centre line, a measure tick every cell,
   over a procedural drafting grid.
7. **Terminal / CRT.** Phosphor blocks along the line (like text cells), typing on in steps, scanlines, phosphor glow,
   a blinking block cursor at the head, and CRT glass on the ground (scanlines, vignette, rounded corners). The
   "CRT pillow" bricks for Nah Blocks can reuse the same ground pass.
8. **Procedural eye** (owner's reference). Iris = distance check, with the colour lerped over the distance; pupil
   round or slit; a highlight; lids closing with `_Blink`; the iris follows a look target. Built for the Meh Merge
   Eyeballs B "monster eyes" set.

## Evaluation (lab captures, phone portrait)

| theme | reads at a glance | look / wow | cost (fill) | verdict |
|---|---|---|---|---|
| Classic (now) | ★★★ | ★ | lowest | baseline |
| Sumi Ink | ★★★ | ★★ — nice brush edge and draw-on, but up close the dry-tail streaks look like white scratches | low | **good** once the dry streaks are toned down; best on paper |
| Chalkboard | ★★★ | ★★★ — grain sells it immediately | low | **strong**; the face caps should be chalky too |
| Blueprint | ★★ | ★★ — the hollow outline works, but the overlapping chevron outlines draw an X, and the grid is too busy | low | needs work: chevron as one shape, quieter grid |
| Terminal | ★★ | ★★ — the blocks typing on are fun; green on green loses contrast and the ground banding is too strong | medium (glow pad) | OK after tuning: brighter arrows, softer ground |
| Hologram | ★★★ | ★★★ — closest to the references: rims, scanlines and bands read well | medium (glow pad) | **strong** |
| Neon | ★★★ | ★★★ — cleanest and most premium-looking | medium (glow pad) | **strongest** |
| Monster eyes | ★★ | ★★ — look-at and blink work; the sclera is too big and the iris too small, so it reads as "googly" | low | promising; needs proportions plus a body shader (shine, squash) |

Problems that affect every theme:
- **Face caps are flat dots.** With a shader on the line, the round tail cap (an Image) should use the theme too:
  chalky, glowing, phosphor. Next step: let the stroke draw its own tail cap, so one material covers the whole arrow.
- **Overlapping head pieces.** The chevron is two capsules and a disc, so alpha or glow adds up where they meet: an X
  in Blueprint, hot spots in Neon and Hologram. Next step: build the chevron as one outline path.
- **Before the draw starts,** a sliver of each line's tail shows (reveal threshold). This is a small fix.
- **Phone cost.** Every effect is a single pass with no textures. The glow pad themes overdraw about 3× the line area,
  which is fine for 10–30 arrows. Endless boards should be profiled on a device before shipping.

## Round 2: faces, caps, Meh Merge and Nah Blocks (same day)

The owner asked for shaders on the faces, and for Merge and Blocks too. The idea is sprite shaders that take
textures, e.g. procedural eyes with a texture plugged in.

- **ThemeFace.** Samples the same face flipbook as `FaceArray` (slice in uv1.x, so faces still batch), then styles it
  like the theme: ink bleed, chalk grain, neon glow (8-tap halo), hologram scanlines, pixel grid, or phosphor. The
  ink takes the theme colour, while tears and sweat keep theirs.
- **ThemeSprite.** Styles any sprite the same way. Tail caps are drawn as a procedural circle in a bigger quad (no
  atlas sampling, room for the halo). Neon caps are a hollow tube ring with the face inside.
- **BallSkin** (Meh Merge). One sprite fakes a 3D ball: toon light, rim, gloss, ink outline. It rolls (`_Spin`) and
  has a surface per set:
  - Billiard: solids or stripes, numbers from a digit texture atlas (`Art/Lab/ball_digits.png`), so the number turns
    with the ball.
  - Sports: tennis seam, baseball stitches, basketball lines, soccer patches, beach segments, golf dimples, marble
    swirl.
  - Planets: noise rock with craters, Earth with land, sea and moving clouds, gas bands, Saturn's ring, the Sun's
    corona.
  - Monster body: toon sphere carrying 1–3 procedural eyes.

  `_Pattern` takes any equirectangular texture in place of the procedural surface.
- **BlockSkin** (Nah Blocks):
  - Retro bevel.
  - 8×8 pixel sprite, with pixelated faces.
  - Chunky toy brick seen from the side, with two studs (the quad is 1 : 1.25).
  - Cut gem: 8 facets, a table and a travelling glint.
  - CRT pillow, for comparison.

Round 2 verdict:
- Every one of these works from a single sprite and a single material.
- Caps and faces now belong to their theme. Neon and Hologram gained the most from this.
- Billiard: the face fights with the number disc. Choose either "number disc = face plate" or a smaller face under
  the disc.
- Monster eyes now read well: the iris is bigger and they look-at and blink.
- Blocks: Retro, Pixel and Toy Studs read immediately. Gems needs a quieter glint.
- For production, per-ball parameters (set, tier, colour) should go into vertex data instead of one material per ball,
  so a whole pile stays one batch.

## Round 3: owner feedback (same day)

| Owner said | Problem found | Change |
|---|---|---|
| Terminal: redesign, the blinking at the head is annoying | the blinking block cursor; block segments read as noise | **Vector CRT** (`ArrowVector`): a thin phosphor beam with bloom, like an arcade vector monitor. The beam spot draws the line on; there is no blink; ring caps |
| Neon / Hologram: the face is seen through, the tail looks cut | the line ran under the hollow cap and through the face | `_TailClip`: the line starts at the cap's edge. Neon rings are solid inside (`_Inner`) |
| Balls: white face on a white ball | billiard faces sat on the white stripe or plate with cream ink | billiard = a cream **face plate** with dark ink and the small number above it; the stripes roll, the plate stays in front |
| Monster eyes: the white highlight dot is ugly; a monster = horns + colour | the dot in the iris; procedural eyes on monsters | the dot is removed. **Monsters** = toon colour, a belly patch, two horns, the game's faces. The pile keeps room for horns only above each ball |
| Add slimes and real eyeballs | — | **Slimes**: a wobbling jelly drop, wider at the bottom, see-through, rising bubbles. **Eyeballs**: the whole ball is an eye, with a veined sclera, a fibrous iris, a limbal ring and a pupil that turns to look |
| Pixel blocks: ugly, drop | — | removed from the lab |
| Toy studs: ugly | studs were flat ellipses inside the block, too tall a quad | a rounded plastic brick in a square cell; two studs **stand up** above its top edge, with an ink outline, a lit cap and a shaded side |
| CRT blocks: ugly | — | removed (it was only there for comparison) |

## Recommendation

Use shaders for the themes:
- **Bruh Arrows.** The premium themes become Neon, Hologram and Chalkboard, with Sumi Ink as the free upgrade of the
  classic look (it suits the paper and the "drawn" feel). All of them get the draw-on entrance. Blueprint and Terminal
  stay only if the owner likes them after the fixes above.
- **Meh Merge Eyeballs.** Go with the procedural eye shader, after tuning the proportions.
- **Nah Blocks Retro.** Can share the CRT ground pass.

## Sources
- [Drawing Lines is Hard — Matt DesLauriers](https://mattdesl.svbtle.com/drawing-lines-is-hard)
- [How SVG Line Animation Works — CSS-Tricks](https://css-tricks.com/svg-line-animation-works/)
- [An Ink-Bleed Fade — Northway Games](https://northwaygames.com/an-ink-bleed-fade/)
- [A Pencil Sketch Effect — Kyle Halladay](https://kylehalladay.com/blog/tutorial/2017/02/21/Pencil-Sketch-Effect.html)
- [Holograms in Shader Graph and URP — Daniel Ilett](https://danielilett.com/2020-07-12-tut5-9-urp-hologram/)
- [Create a Hologram shader — Unity Learn](https://learn.unity.com/course/get-started-with-shader-graph/unit/hologram-and-vertex-displacement-shaders/tutorial/create-a-hologram-shader?version=6.3)
- [Chalk that looks like chalk: depositing particles — DEV](https://dev.to/tbds_2dadf2b626f315902eae/chalk-that-looks-like-chalk-depositing-particles-instead-of-stroking-paths-3ak7)
- Owner's references: procedural eye shader (Minions Art), hologram figures, and the striped-light animals.
