using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>Placeholder characters (fill + outline + face + white flash layer) built from the current sheets.</summary>
    public static class SandboxArt
    {
        public static readonly Color[] Palette =
        {
            UIKit.Hex("#FF5A5F"), UIKit.Hex("#FF9F1C"), UIKit.Hex("#FFD23F"), UIKit.Hex("#3DDC97"),
            UIKit.Hex("#4EA8DE"), UIKit.Hex("#9B5DE5"), UIKit.Hex("#F15BB5"),
        };

        /// <summary>Radius of the circle_fill body as a fraction of the sprite size (122 of 256 px).</summary>
        public const float FillRadius = 122f / 256f;

        /// <summary>Shows/hides a character's body layers (fill, outline, flash), keeping the face.</summary>
        public static void SetBodyVisible(Transform character, bool visible)
        {
            foreach (Transform c in character)
                if (c.name != "face") c.gameObject.SetActive(visible);
        }

        /// <summary>Nearest color family among the effect variants we ship (Pink / Blue / Green / Yellow).</summary>
        public static string Family(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out _);
            if (s < 0.2f) return "Blue";
            float deg = h * 360f;
            if (deg < 25f || deg >= 270f) return "Pink";   // red, pink, purple
            if (deg < 75f) return "Yellow";                 // orange, yellow
            if (deg < 170f) return "Green";
            return "Blue";
        }

        /// <summary>Replaces "{color}" in an effect name with the color family.</summary>
        public static string Colored(string effect, Color c) => effect.Replace("{color}", Family(c));

        public static SpriteRenderer Sprite(Transform parent, string spriteName, Color color, float size, int order)
        {
            var sprite = ArtLibrary.Instance.Get(spriteName);
            var go = new GameObject(spriteName);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);
            return sr;
        }

        /// <summary>shape = "circle" or "block". Children: fill, line, face, flash (white, alpha 0).</summary>
        public static Transform Character(Transform parent, string shape, Color color, float size, string face, Vector3 position, int order = 10)
        {
            var root = new GameObject(shape).transform;
            root.SetParent(parent, false);
            root.position = position;
            Sprite(root, shape + "_fill", color, size, order);
            Sprite(root, shape + "_line", Color.white, size, order + 1);
            if (!string.IsNullOrEmpty(face)) Sprite(root, face, Color.white, size * 0.81f, order + 2).name = "face";
            Sprite(root, shape + "_fill", new Color(1f, 1f, 1f, 0f), size, order + 3).name = "flash";
            return root;
        }

        public static void SetFace(Transform character, string face)
        {
            var t = character.Find("face");
            if (t != null) t.GetComponent<SpriteRenderer>().sprite = ArtLibrary.Instance.Get(face);
        }

        public static void SetFlash(Transform character, float amount)
        {
            var t = character.Find("flash");
            if (t != null) t.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, amount);
        }
    }
}
