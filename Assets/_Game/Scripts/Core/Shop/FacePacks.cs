using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The shop's face packs (Deadpan, Sleepy, Grumpy, Derp), shared by all three games on their third shop tab.
    /// A pack is another Texture2DArray with the same 9 expressions; the worn one is set as a global shader texture
    /// (FaceArray reads it), so every face on screen changes at once and the face material asset is never touched.
    /// </summary>
    public static class FacePacks
    {
        public const int Tab = 2;
        private static readonly int PackId = Shader.PropertyToID("_FacePack"), OnId = Shader.PropertyToID("_FacePackOn");
        private static bool hooked;

        /// <summary>Adds the faces tab's skins to a game's catalog (the catalog must have a third tab).</summary>
        public static void AddTo(SkinCatalog c)
        {
            c.Add(new SkinDef { Id = "face_deadpan", En = "Deadpan", Vi = "Mặt lạnh", Tab = Tab, Price = SkinPrice.Free });
            c.Add(new SkinDef { Id = "face_sleepy", En = "Sleepy", Vi = "Buồn ngủ", Tab = Tab, Price = SkinPrice.Coins, Cost = 400, FacePack = "sleepy" });
            c.Add(new SkinDef { Id = "face_grumpy", En = "Grumpy", Vi = "Cau có", Tab = Tab, Price = SkinPrice.Coins, Cost = 500, FacePack = "grumpy" });
            c.Add(new SkinDef { Id = "face_derp", En = "Derp", Vi = "Ngố", Tab = Tab, Price = SkinPrice.Coins, Cost = 600, FacePack = "derp" });
        }

        /// <summary>Wears the equipped pack (called when a game starts; follows every later equip).</summary>
        public static void Apply()
        {
            if (!hooked)
            {
                hooked = true;
                Skins.Changed += Apply;
            }
            Show(Skins.Equipped(Tab));
        }

        /// <summary>Shows a pack on every face right now (the shop's try-on); null or Deadpan = the default faces.</summary>
        public static void Show(SkinDef skin)
        {
            var pack = skin != null && skin.FacePack != null && ArtLibrary.Instance != null ? ArtLibrary.Instance.FacePack(skin.FacePack) : null;
            if (pack != null) Shader.SetGlobalTexture(PackId, pack);
            Shader.SetGlobalFloat(OnId, pack != null ? 1f : 0f);
        }

        private static readonly Dictionary<string, Material> cardMats = new();

        /// <summary>A face material showing one pack whatever is worn (shop cards).</summary>
        public static Material CardMaterial(SkinDef skin)
        {
            var key = skin?.FacePack ?? "deadpan";
            if (cardMats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(ArtLibrary.Instance.FaceMaterial) { name = "FaceCard." + key };
            m.SetFloat("_UseGlobalPack", 0f);
            var pack = skin?.FacePack != null ? ArtLibrary.Instance.FacePack(skin.FacePack) : null;
            if (pack != null) m.SetTexture("_Faces", pack);
            cardMats[key] = m;
            return m;
        }

        /// <summary>Puts a pack on a UI face for good (shop cards).</summary>
        public static void Wear(Face face, SkinDef skin)
        {
            var g = face != null ? face.GetComponent<FaceGraphic>() : null;
            if (g != null) g.material = CardMaterial(skin);
        }
    }
}
