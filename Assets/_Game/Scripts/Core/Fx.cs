using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    public enum FxKind { Sparkle, Pop, Ring, Confetti, Stars }

    /// <summary>
    /// Pooled particle bursts built from the fx sprite sheet (white sprites, tinted per burst).
    /// One ParticleSystem per kind per scene, driven by Emit(), so there is no Instantiate/Destroy per effect.
    /// </summary>
    public static class Fx
    {
        public const int SortingOrder = 500;
        private static readonly Dictionary<FxKind, ParticleSystem> systems = new();
        private static Transform root;
        private static Material material;

        public static void Burst(FxKind kind, Vector3 worldPos, Color color, int count = 14, float scale = 1f)
        {
            var ps = Get(kind);
            if (ps == null) return;
            var p = new ParticleSystem.EmitParams
            {
                position = new Vector3(worldPos.x, worldPos.y, 0f),
                applyShapeToPosition = true,
                startColor = color,
            };
            if (kind == FxKind.Ring) p.startSize = 1.2f * scale;
            else if (scale != 1f) p.startSize = ps.main.startSize.constantMax * scale;
            ps.Emit(p, count);
        }

        private static ParticleSystem Get(FxKind kind)
        {
            if (root == null)
            {
                systems.Clear();
                root = new GameObject("Fx").transform;
            }
            if (systems.TryGetValue(kind, out var existing) && existing != null) return existing;
            if (material == null) material = Resources.Load<Material>("FxMaterial");
            if (material == null) return null;

            var go = new GameObject(kind.ToString());
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true; // stays alive between bursts; emission is off so it only spawns on Emit()
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            string sprite;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var alpha = new Gradient();
            alpha.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = alpha;

            switch (kind)
            {
                case FxKind.Sparkle:
                    sprite = "fx_spark";
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
                    break;
                case FxKind.Pop:
                    sprite = "fx_circle";
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                    break;
                case FxKind.Ring:
                    sprite = "fx_ring";
                    main.startLifetime = 0.35f;
                    main.startSpeed = 0f;
                    main.startSize = 1.2f;
                    shape.radius = 0.0001f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1.4f));
                    break;
                case FxKind.Confetti:
                    sprite = "fx_confetti";
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
                    main.gravityModifier = 1.6f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 35f;
                    shape.rotation = new Vector3(-90f, 0f, 0f);
                    var spin = ps.rotationOverLifetime;
                    spin.enabled = true;
                    spin.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
                    size.enabled = false;
                    break;
                default:
                    sprite = "fx_star";
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
                    main.gravityModifier = 0.6f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
                    break;
            }

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(ArtLibrary.Instance.Get(sprite));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            systems[kind] = ps;
            return ps;
        }
    }
}
