import React from 'react';
import { AbsoluteFill, Audio, Img, Sequence, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from 'remotion';
import { fontFamily, INK, PAPER } from './theme';

// Google Play preview videos (30 s, 1080 x 1920, 30 fps), cut from editor-captured gameplay frames
// (PromoShots.BeginClip / Record -> public/frames/<clip>/00000.jpg). Shot list = the approved director storyboard.

export const FPS = 30;
export const LENGTH = 900;

const FRAME_COUNT: Record<string, number> = {
  m_classic: 420, m_danger: 100, m_hamster: 150, m_hungry: 150, m_eyeballs: 80, m_snow: 80, m_compass: 80,
  b_hook: 100, b_classic: 168, b_watchers: 110, b_city: 139, b_aquarium: 113, b_chrome: 67, b_pastel: 67, b_peak: 120,
  a_hook: 100, a_classic: 170, a_blocked: 64, a_zipper: 120, a_chalk: 50, a_blueprint: 50, a_vector: 50, a_holo: 50,
  a_neon: 50, a_win: 150, a_levels: 80,
};

const pad = (n: number) => String(n).padStart(5, '0');

const text = (px: number, color: string, stroke = true): React.CSSProperties => ({
  fontFamily, fontSize: px, fontWeight: 800, lineHeight: 1, color, margin: 0, letterSpacing: -1, whiteSpace: 'nowrap',
  ...(stroke ? { WebkitTextStroke: `${Math.round(px * 0.11)}px ${INK}`, paintOrder: 'stroke fill' } : {}),
});

/** Pop-in used by every caption: quick overshoot scale over the first frames of its sequence. */
const usePop = (delay = 0) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  return spring({ frame: f - delay, fps, config: { damping: 11, stiffness: 220, mass: 0.6 } });
};

type ShotProps = {
  clip: string;
  /** Source frame for the shot's local frame (speed ramps, slow-mo and freezes live here). */
  at?: (f: number) => number;
  /** Zoom for the local frame, around `origin` (fractions of the frame). */
  zoom?: (f: number) => number;
  origin?: [number, number];
  /** Pixel offset for the local frame (impact shakes). */
  shake?: (f: number) => [number, number];
};

const Shot: React.FC<ShotProps> = ({ clip, at = (f) => f, zoom, origin = [0.5, 0.5], shake }) => {
  const f = useCurrentFrame();
  const n = Math.max(0, Math.min(FRAME_COUNT[clip] - 1, Math.round(at(f))));
  const s = zoom ? zoom(f) : 1;
  const [dx, dy] = shake ? shake(f) : [0, 0];
  return (
    <AbsoluteFill style={{ overflow: 'hidden', background: '#000' }}>
      <Img
        src={staticFile(`frames/${clip}/${pad(n)}.jpg`)}
        style={{
          width: 1080, height: 1920, display: 'block',
          transform: `translate(${dx}px, ${dy}px) scale(${s})`,
          transformOrigin: `${origin[0] * 100}% ${origin[1] * 100}%`,
        }}
      />
    </AbsoluteFill>
  );
};

/** Few-frame jolt starting at local frame `at`. */
const jolt = (at: number, px = 18) => (f: number): [number, number] => {
  const k = f - at;
  if (k < 0 || k > 2) return [0, 0];
  return [[px, -px, px * 0.5][k], [-px * 0.6, px * 0.6, 0][k]];
};

const Flash: React.FC<{ at: number; color?: string }> = ({ at, color = '#FFFFFF' }) => {
  const f = useCurrentFrame();
  const o = f === at ? 0.92 : f === at + 1 ? 0.5 : 0;
  return o ? <AbsoluteFill style={{ background: color, opacity: o }} /> : null;
};

// ---------- caption styles (one per game, matching its store screenshots; storyboard frames x3) ----------

const MergeBand: React.FC<{ l1: string; l2?: string }> = ({ l1, l2 }) => {
  const p = usePop();
  return (
    <div style={{
      position: 'absolute', left: 0, top: 0, width: 1080, height: 330, background: 'rgba(46,37,82,0.94)',
      display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 10,
    }}>
      <div style={{ transform: `scale(${p})`, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10 }}>
        <p style={text(104, PAPER)}>{l1}</p>
        {l2 ? <p style={text(104, '#FFD23F')}>{l2}</p> : null}
      </div>
    </div>
  );
};

const BlocksRibbon: React.FC<{ word: string; color: string; rot?: number }> = ({ word, color, rot = -4 }) => {
  const p = usePop();
  const px = word.length < 9 ? 162 : 128;
  return (
    <div style={{
      position: 'absolute', left: -42, bottom: 162, width: 1164, height: 246, background: color, border: `12px solid ${INK}`,
      transform: `rotate(${rot}deg) scaleX(${interpolate(p, [0, 1], [0.2, 1])})`, display: 'flex', alignItems: 'center',
      justifyContent: 'center', boxShadow: `0 21px 0 ${INK}`, boxSizing: 'border-box',
    }}>
      <p style={{ ...text(px, PAPER), transform: `scale(${p})` }}>{word}</p>
    </div>
  );
};

const Tape: React.FC = () => (
  <div style={{
    position: 'absolute', left: -90, top: -42, width: 210, height: 78, transform: 'rotate(-12deg)',
    background: 'repeating-linear-gradient(45deg, #FFC2B8 0 24px, rgba(255,255,255,0.4) 24px 42px)',
  }} />
);

const ArrowsKraft: React.FC<{ l1: string }> = ({ l1 }) => {
  const p = usePop();
  return (
    <div style={{
      position: 'absolute', left: 66, top: 60, padding: '30px 54px', background: '#EBDCBC', borderBottom: '9px solid #C9B48F',
      transform: `rotate(-2deg) scale(${p})`, transformOrigin: '0 0', boxShadow: '0 18px 30px rgba(30,34,64,0.25)',
    }}>
      <Tape />
      <p style={text(108, '#FF5A5F')}>{l1}</p>
    </div>
  );
};

const ArrowsSticky: React.FC<{ l1: string }> = ({ l1 }) => {
  const p = usePop();
  return (
    <div style={{
      position: 'absolute', right: 54, top: 66, padding: '36px 42px', background: '#FFE57A',
      transform: `rotate(4deg) scale(${p})`, transformOrigin: '100% 0', boxShadow: '0 18px 30px rgba(30,34,64,0.3)',
    }}>
      <p style={text(84, INK, false)}>{l1}</p>
    </div>
  );
};

const Bruh: React.FC = () => {
  const p = usePop();
  return (
    <div style={{ position: 'absolute', left: 0, top: 690, width: 1080, textAlign: 'center', transform: `rotate(-6deg) scale(${interpolate(p, [0, 1], [2.2, 1])})`, opacity: Math.min(1, p * 2) }}>
      <p style={{ ...text(288, '#FF5A5F'), display: 'inline-block' }}>Bruh.</p>
    </div>
  );
};

const EndCard: React.FC<{ bg: string; logo: string; tagline: string; color?: string }> = ({ bg, logo, tagline, color = PAPER }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: f, fps, config: { damping: 9, stiffness: 160 } });
  const t = spring({ frame: f - 10, fps, config: { damping: 14, stiffness: 180 } });
  return (
    <AbsoluteFill style={{ background: bg, alignItems: 'center', justifyContent: 'center', gap: 78 }}>
      {/* logo PNGs are 1600 x 600 with ~45% transparent margin: draw wide, pull the empty bands in */}
      <Img src={staticFile(logo)} style={{ width: 1600, margin: '-150px 0 -110px', display: 'block', transform: `scale(${s}) translateY(${(1 - s) * 120}px)` }} />
      <p style={{ ...text(68, color), whiteSpace: 'normal', maxWidth: 1020, lineHeight: 1.1, textAlign: 'center', opacity: t, transform: `translateY(${(1 - t) * 40}px)` }}>{tagline}</p>
    </AbsoluteFill>
  );
};

const Music: React.FC<{ file: string }> = ({ file }) => (
  <Audio src={staticFile(`music/${file}`)} volume={(f) => interpolate(f, [0, LENGTH - 15, LENGTH], [0.9, 0.9, 0], { extrapolateRight: 'clamp' })} />
);

// ---------- Meh Merge ----------

// m_classic: one combo chain, x3..x8 merges at these source frames (score jumps in score_a/score_b.txt).
const CHAIN = [169, 181, 202, 214, 226, 238];

const mergeHookZoom = (f: number) => {
  const src = 160 + f;
  let s = 1;
  for (const m of CHAIN) {
    if (src < m) break;
    s += 0.08 / CHAIN.length;
    s += 0.035 * Math.exp(-(src - m) / 2.5); // punch, settles in ~5 frames
  }
  return s;
};

// Peak: same chain replayed closer; slow-mo 0.5x for 10 frames into the x8, then a 2-frame white flash.
const peakAt = (f: number) => (f < 37 ? 196 + f : f < 47 ? 233 + (f - 37) * 0.5 : 238 + (f - 47));

const PILE: [number, number] = [0.5, 0.84];

export const MergeVideo: React.FC = () => (
  <AbsoluteFill style={{ background: '#2E2552' }}>
    <Sequence durationInFrames={90}><Shot clip="m_classic" at={(f) => 160 + f} zoom={mergeHookZoom} origin={[0.28, 0.8]} /></Sequence>
    <Sequence from={90} durationInFrames={150}><Shot clip="m_classic" /></Sequence>
    <Sequence from={240} durationInFrames={90}>
      <Shot clip="m_danger" shake={jolt(84)} />
      <DangerPulse />
    </Sequence>
    <Sequence from={330} durationInFrames={90}><Shot clip="m_hamster" zoom={() => 1.45} origin={PILE} /></Sequence>
    <Sequence from={420} durationInFrames={90}><Shot clip="m_hungry" zoom={() => 1.35} origin={PILE} /></Sequence>
    <Sequence from={510} durationInFrames={60}><Shot clip="m_eyeballs" at={(f) => 16 + f} zoom={() => 1.4} origin={PILE} /></Sequence>
    <Sequence from={570} durationInFrames={60}><Shot clip="m_snow" at={(f) => 20 + f} zoom={() => 1.4} origin={PILE} /></Sequence>
    <Sequence from={630} durationInFrames={60}><Shot clip="m_compass" zoom={() => 1.4} origin={PILE} /></Sequence>
    <Sequence from={690} durationInFrames={120}>
      <Shot clip="m_classic" at={peakAt} zoom={(f) => interpolate(f, [0, 47, 60], [1.15, 1.3, 1.24], { extrapolateRight: 'clamp' })} origin={[0.22, 0.82]} />
      <Flash at={47} />
    </Sequence>
    <Sequence from={810}><EndCard bg="#2E2552" logo="logos/meh-merge-logo.png" tagline="Two mehs make a bigger meh." /></Sequence>

    <Sequence durationInFrames={90}><MergeBand l1="One chain." l2="Six merges." /></Sequence>
    <Sequence from={90} durationInFrames={150}><MergeBand l1="Drop. Merge." /></Sequence>
    <Sequence from={240} durationInFrames={90}><MergeBand l1="Don't overflow." /></Sequence>
    <Sequence from={330} durationInFrames={90}><MergeBand l1="Hamster balls" l2="They actually run" /></Sequence>
    <Sequence from={420} durationInFrames={90}><MergeBand l1="Hungry balls" l2="Twins = dinner" /></Sequence>
    <Sequence from={510} durationInFrames={180}><MergeBand l1="Wild new looks" /></Sequence>
    <Sequence from={690} durationInFrames={120}><MergeBand l1="x8 combo" /></Sequence>
    <Music file="bloom.wav" />
  </AbsoluteFill>
);

/** Red edge pulse while the jar is over the line. */
const DangerPulse: React.FC = () => {
  const f = useCurrentFrame();
  const o = 0.25 + 0.25 * Math.sin((f / 30) * Math.PI * 4);
  return <AbsoluteFill style={{ boxShadow: 'inset 0 0 160px 40px rgba(255,60,70,1)', opacity: o }} />;
};

// ---------- Nah Blocks ----------

const BOARD: [number, number] = [0.5, 0.5];
const zb = () => 1.47; // storyboard crop: board fills the frame

export const BlocksVideo: React.FC = () => (
  <AbsoluteFill style={{ background: '#2B2F55' }}>
    <Sequence durationInFrames={90}><Shot clip="b_hook" at={(f) => 2 + f} /></Sequence>
    <Sequence from={90} durationInFrames={150}><Shot clip="b_classic" /></Sequence>
    <Sequence from={240} durationInFrames={90}><Shot clip="b_watchers" at={(f) => 10 + f} zoom={zb} origin={BOARD} /></Sequence>
    <Sequence from={330} durationInFrames={120}><Shot clip="b_city" at={(f) => 14 + f} zoom={zb} origin={BOARD} /></Sequence>
    <Sequence from={450} durationInFrames={90}><Shot clip="b_aquarium" at={(f) => 18 + f} zoom={zb} origin={BOARD} /></Sequence>
    <Sequence from={540} durationInFrames={60}><Shot clip="b_chrome" zoom={zb} origin={BOARD} /></Sequence>
    <Sequence from={600} durationInFrames={60}><Shot clip="b_pastel" zoom={zb} origin={BOARD} /></Sequence>
    <Sequence from={660} durationInFrames={30}><Shot clip="b_classic" at={(f) => 120 + f} zoom={zb} origin={BOARD} /></Sequence>
    <Sequence from={690} durationInFrames={120}><Shot clip="b_peak" shake={jolt(36, 22)} /></Sequence>
    <Sequence from={810}><EndCard bg="#2B2F55" logo="logos/nah-blocks-logo.png" tagline="Line 'em up. They say nah." /></Sequence>

    <Sequence durationInFrames={90}><BlocksRibbon word="TRIPLE CLEAR" color="#3DDC97" /></Sequence>
    <Sequence from={90} durationInFrames={150}><BlocksRibbon word="SATISFYING" color="#3DDC97" rot={3} /></Sequence>
    <Sequence from={240} durationInFrames={90}><BlocksRibbon word="NOSY" color="#FFD23F" /></Sequence>
    <Sequence from={330} durationInFrames={120}><BlocksRibbon word="LIT" color="#1B1F45" rot={3} /></Sequence>
    <Sequence from={450} durationInFrames={90}><BlocksRibbon word="SPLASHY" color="#3FC6E8" /></Sequence>
    <Sequence from={540} durationInFrames={60}><BlocksRibbon word="SHINY" color="#C9D1E0" rot={3} /></Sequence>
    <Sequence from={600} durationInFrames={60}><BlocksRibbon word="COZY" color="#FFB3C7" rot={-3} /></Sequence>
    <Sequence from={660} durationInFrames={30}><BlocksRibbon word="CLASSIC" color="#3DDC97" rot={3} /></Sequence>
    <Sequence from={690} durationInFrames={120}><BlocksRibbon word="EXCELLENT" color="#FFD23F" /></Sequence>
    <Music file="shorter.wav" />
  </AbsoluteFill>
);

// ---------- Bruh Arrows ----------

const za = () => 1.32;
const THEMES = ['a_chalk', 'a_blueprint', 'a_vector', 'a_holo', 'a_neon'];

export const ArrowsVideo: React.FC = () => (
  <AbsoluteFill style={{ background: '#F5F1EA' }}>
    <Sequence durationInFrames={90}><Shot clip="a_hook" /></Sequence>
    {/* 2x speed ramp through the middle of the solve */}
    <Sequence from={90} durationInFrames={140}><Shot clip="a_classic" at={(f) => (f < 110 ? f : 110 + (f - 110) * 2)} /></Sequence>
    {/* freeze 4 frames on the blocked hit */}
    <Sequence from={230} durationInFrames={70}><Shot clip="a_blocked" at={(f) => (f < 18 ? f : f < 22 ? 18 : f - 4)} shake={jolt(18, 14)} /></Sequence>
    <Sequence from={300} durationInFrames={120}><Shot clip="a_zipper" /></Sequence>
    {THEMES.map((c, i) => (
      <Sequence key={c} from={420 + i * 48} durationInFrames={48}><Shot clip={c} zoom={za} origin={[0.5, 0.48]} /></Sequence>
    ))}
    <Sequence from={660} durationInFrames={96}><Shot clip="a_win" /></Sequence>
    <Sequence from={756} durationInFrames={54}><Shot clip="a_levels" at={(f) => 24 + f} /></Sequence>
    <Sequence from={810}><EndCard bg="#EBDCBC" logo="logos/bruh-arrows-logo.png" tagline="Tap. Yeet. Bruh." color="#FF5A5F" /></Sequence>

    <Sequence durationInFrames={90}><ArrowsKraft l1="Tap. Yeet." /></Sequence>
    <Sequence from={90} durationInFrames={140}><ArrowsSticky l1="Find the order" /></Sequence>
    <Sequence from={248} durationInFrames={52}><Bruh /></Sequence>
    <Sequence from={300} durationInFrames={120}><ArrowsKraft l1="Tap to unzip." /></Sequence>
    <Sequence from={420} durationInFrames={240}><ArrowsSticky l1="New world" /></Sequence>
    <Sequence from={660} durationInFrames={150}><ArrowsKraft l1="100 levels" /></Sequence>
    <Music file="daisies.wav" />
  </AbsoluteFill>
);
