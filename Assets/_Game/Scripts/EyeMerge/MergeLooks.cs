using System;
using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.EyeMerge
{
    /// <summary>
    /// Meh Merge's premium ball sets, drawn by shaders instead of the flat circle sprite (Docs/SHADER_LAB.md):
    /// billiard, sports, planets, monsters (BallSkin) and slimes (SlimeSkin), eyeballs (BallSkin set 5). One shared
    /// material per set; each ball's tier data goes in a MaterialPropertyBlock (world balls) or its own UI material
    /// (shop). The ball quad is bigger than the ball where the set needs room (horns, Saturn's ring, the Sun's corona).
    /// </summary>
    public static class MergeLooks
    {
        public sealed class Look
        {
            public string Shader = "BallSkin";
            public int Set;
            public bool Face = true;        // eyeballs: the ball is the eye, no face
            public bool FacePlate;          // billiard: the face sits on the cream plate, dark ink
        }

        private static readonly Dictionary<string, Look> Looks = new()
        {
            ["skin_billiard"] = new Look { Set = 0, FacePlate = true },
            ["skin_sports"] = new Look { Set = 1 },
            ["skin_planets"] = new Look { Set = 2 },
            ["skin_monsters"] = new Look { Set = 3 },
            ["skin_slimes"] = new Look { Shader = "SlimeSkin", Set = 4 },
            ["skin_eyeballs"] = new Look { Set = 5, Face = false },
        };

        public static Look Of(SkinDef skin) => skin != null && Looks.TryGetValue(skin.Id, out var l) ? l : null;
        public static Look Current => Of(Skins.Equipped(0));

        private static Color H(string hex) => UIKit.Hex(hex);
        public static Color[] P(params string[] hex) => Array.ConvertAll(hex, UIKit.Hex);

        // per-tier data, tier 1..11 (the lab's sets)
        public static readonly Color[] Billiard = P("#F2C230", "#1F5FBF", "#C0392B", "#6A2C91", "#F77F00", "#1E8C45", "#8A2432", "#1A1A1A", "#F2C230", "#1F5FBF", "#C0392B");
        // marble, ping-pong, golf, tennis, baseball, volleyball, bowling, soccer, basketball, medicine ball, beach ball
        private static readonly (int v, string a, string b)[] Sports =
        {
            (6, "#7FD1FF", "#FFFFFF"), (6, "#FF9F1C", "#FFB347"), (5, "#FFFFFF", "#FFFFFF"), (0, "#D4E157", "#FFFFFF"), (1, "#FFFFFF", "#FFFFFF"),
            (4, "#FFFFFF", "#FFFFFF"), (6, "#2A2A40", "#5A3A8A"), (3, "#FFFFFF", "#FFFFFF"), (2, "#E8792A", "#E8792A"), (6, "#7A4A2A", "#5A3418"), (4, "#FFFFFF", "#FFFFFF"),
        };
        // Pluto, Moon, Mercury, Mars, Venus, Earth, Neptune, Uranus, Saturn, Jupiter, Sun
        private static readonly (int v, string a, string b)[] Planets =
        {
            (0, "#D9C7B0", "#9C8670"), (0, "#D8D8DC", "#8E8E96"), (0, "#B7A89A", "#6E6258"), (0, "#E0663A", "#8A2E16"), (2, "#F2D79A", "#D9A85A"),
            (1, "#2E6FD9", "#3FAE5A"), (2, "#3A5BD9", "#7FA0FF"), (2, "#8FE3E8", "#C8F7F7"), (3, "#E8C98A", "#C9A060"), (2, "#E0A060", "#F5E2C0"), (4, "#FFB84D", "#FFE38A"),
        };
        public static readonly Color[] Monsters = P("#F15BB5", "#4EA8DE", "#3DDC97", "#FFD23F", "#9B5DE5", "#FF9F1C", "#F15BB5", "#4EA8DE", "#3DDC97", "#FFD23F", "#9B5DE5");
        public static readonly Color[] Slimes = P("#7CE38B", "#6FC3F7", "#FF8FB1", "#C3A3FF", "#FFD166", "#FF9F6B", "#7CE38B", "#6FC3F7", "#FF8FB1", "#C3A3FF", "#FFD166");
        private static readonly (string outer, string inner)[] Irises =
        {
            ("#1B4F8A", "#7FD1FF"), ("#1B6E3A", "#9BFF7A"), ("#6B3A12", "#E0A050"), ("#4A1A7A", "#C38BFF"), ("#7A1020", "#FF8A6B"),
        };

        /// <summary>The colours the HUD strip, the "next" bubble and the merge splash use for each tier.</summary>
        public static Color[] TierColors(string skinId) => skinId switch
        {
            "skin_billiard" => Billiard,
            "skin_sports" => Array.ConvertAll(Sports, s => s.v == 6 || s.v == 2 || s.v == 0 ? H(s.a) : H("#F2F2F2")),
            "skin_planets" => Array.ConvertAll(Planets, s => H(s.a)),
            "skin_monsters" => Monsters,
            "skin_slimes" => Slimes,
            "skin_eyeballs" => EyeColors(),
            _ => null,
        };

        // each tier's iris colour (Configure picks iris = Irises[tier % 5])
        private static Color[] EyeColors()
        {
            var colors = new Color[11];
            for (int tier = 1; tier <= 11; tier++) colors[tier - 1] = H(Irises[tier % Irises.Length].inner);
            return colors;
        }

        /// <summary>The ball's radius inside its quad (the rest is room for horns, a ring, a corona).</summary>
        public static float SphereR(Look look, int tier) => look.Set switch
        {
            2 => Planets[tier - 1].v == 3 ? 0.45f : Planets[tier - 1].v == 4 ? 0.7f : 0.94f,
            3 => 0.6f,
            4 => 0.72f,
            _ => 0.94f,
        };

        /// <summary>Does this tier's ball read as dark (the face then needs cream ink)?</summary>
        public static bool Dark(Look look, int tier) => look.Set switch
        {
            0 => false,                                  // the face sits on the cream plate
            2 => Planets[tier - 1].v != 4 && H(Planets[tier - 1].a).grayscale < 0.45f,
            _ => false,
        };

        private static Texture2D digits;

        private static Texture2D Digits
        {
            get
            {
                if (digits != null) return digits;
                digits = Resources.Load<Texture2D>("ball_digits");
#if UNITY_EDITOR
                // TODO(device builds): ship the digits atlas with the game (GameConfig / ArtLibrary) instead of the lab folder
                if (digits == null) digits = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Lab/ball_digits.png");
#endif
                return digits;
            }
        }

        /// <summary>Writes a tier's values through the given setters (a property block or a material).</summary>
        public static void Configure(Look look, int tier, Action<string, float> f, Action<string, Color> c, Action<string, Texture> t)
        {
            f("_Set", look.Set);
            f("_R", SphereR(look, tier));
            switch (look.Set)
            {
                case 0:
                    c("_ColA", Billiard[tier - 1]);
                    c("_ColB", H("#FFF8EC"));
                    f("_Number", tier);
                    f("_Stripe", tier >= 9 ? 1 : 0);
                    if (Digits != null) t("_Digits", Digits);
                    break;
                case 1:
                    var s = Sports[tier - 1];
                    f("_Variant", s.v);
                    c("_ColA", H(s.a));
                    c("_ColB", H(s.b));
                    break;
                case 2:
                    var p = Planets[tier - 1];
                    f("_Variant", p.v);
                    c("_ColA", H(p.a));
                    c("_ColB", H(p.b));
                    break;
                case 3:
                    c("_ColA", Monsters[tier - 1]);
                    c("_ColB", H("#F3E3C3"));
                    break;
                case 4:
                    c("_Col", Slimes[tier - 1]);
                    f("_Seed", tier * 1.7f);
                    break;
                default:
                    var iris = Irises[tier % Irises.Length];
                    c("_ColA", H(iris.outer));
                    c("_ColB", H(iris.inner));
                    f("_Variant", (tier % 3) / 2f);
                    break;
            }
        }

        private static readonly Dictionary<string, Material> shared = new();

        /// <summary>The set's shared material for world balls (per-ball values go in a property block).</summary>
        public static Material Shared(Look look)
        {
            if (shared.TryGetValue(look.Shader, out var m) && m != null) return m;
            m = new Material(Shader.Find("CasualGame/Lab/" + look.Shader)) { name = look.Shader };
            shared[look.Shader] = m;
            return m;
        }

        /// <summary>A UI Image showing one tier of a set (shop cards and previews); its own material.</summary>
        public static Image UIBall(Transform parent, Look look, int tier, Vector2 pos, float ballSize)
        {
            var size = ballSize / SphereR(look, tier);
            var img = UIKit.Image(parent, null, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), Color.white);
            var m = new Material(Shader.Find("CasualGame/Lab/" + look.Shader));
            Configure(look, tier, m.SetFloat, m.SetColor, m.SetTexture);
            img.material = m;
            img.gameObject.AddComponent<OnDestroyed>().Action = () => UnityEngine.Object.Destroy(m);
            return img;
        }

        private static Sprite unitQuad;

        /// <summary>A 1 x 1 unit quad with 0..1 UVs for the world ball shaders (the atlas circle would hand them its sub-rect).</summary>
        public static Sprite UnitQuad => unitQuad != null ? unitQuad
            : unitQuad = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
    }
}
