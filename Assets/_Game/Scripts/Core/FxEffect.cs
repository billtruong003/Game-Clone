using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Root of one designed effect prefab (a hierarchy of ParticleSystems authored in the inspector / FX sandbox).
    /// Play() places, tints and scales it: tintable systems multiply their authored start color by the tint, the rest
    /// (white cores, flashes, shadows) keep their own colors. Sandbox and games play the same prefabs.
    /// </summary>
    public sealed class FxEffect : MonoBehaviour
    {
        [Tooltip("Systems whose start color is multiplied by the tint passed to Play (e.g. smoke, droplets). Others keep their authored color.")]
        [SerializeField] private ParticleSystem[] tintable = System.Array.Empty<ParticleSystem>();
        [Tooltip("Uniform size multiplier applied before the scale passed to Play.")]
        [SerializeField] private float baseScale = 1f;

        private ParticleSystem[] all;
        private ParticleSystem.MinMaxGradient[] authored;

        /// <summary>Longest duration + lifetime of all child systems: how long the effect stays visible.</summary>
        public float Duration { get; private set; }

        private void Awake()
        {
            all = GetComponentsInChildren<ParticleSystem>(true);
            authored = new ParticleSystem.MinMaxGradient[tintable.Length];
            for (int i = 0; i < tintable.Length; i++)
                if (tintable[i] != null) authored[i] = tintable[i].main.startColor;
            foreach (var ps in all)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy; // transform scale drives size, speed and shape
                Duration = Mathf.Max(Duration, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
            }
        }

        public void Play(Vector3 position, Color tint, float scale = 1f)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            transform.localScale = Vector3.one * (baseScale * scale);
            for (int i = 0; i < tintable.Length; i++)
            {
                if (tintable[i] == null) continue;
                var main = tintable[i].main;
                main.startColor = Multiply(authored[i], tint);
            }
            foreach (var ps in all) { ps.Clear(false); ps.Play(false); }
        }

        private static ParticleSystem.MinMaxGradient Multiply(ParticleSystem.MinMaxGradient g, Color tint)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(g.colorMin * tint, g.colorMax * tint);
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(g.color * tint);
                default:
                    return new ParticleSystem.MinMaxGradient(tint); // gradient modes: tint wins (keep authored alpha via color over lifetime)
            }
        }
    }
}
