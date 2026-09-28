using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>Dev launcher for the three games living in this project (each ships as its own app later).</summary>
    public class HubMenu : MonoBehaviour
    {
        private void Start()
        {
            var cam = Camera.main;
            cam.backgroundColor = UIKit.Hex("#2B2F55");
            cam.clearFlags = CameraClearFlags.SolidColor;
            var canvas = UIKit.CreateCameraCanvas("HubUI", cam);
            var root = UIKit.Stretch(UIKit.Rect("Safe", canvas.transform));
            root.gameObject.AddComponent<SafeArea>();

            var top = new Vector2(0.5f, 1f);
            UIKit.Label(root, "Casual Game", 120, top, new Vector2(0, -300), new Vector2(1000, 160), UIKit.Paper);
            UIKit.Label(root, "Chọn game để chơi thử", 52, top, new Vector2(0, -400), new Vector2(1000, 80), UIKit.Hex("#AEB3D9"));

            var mid = new Vector2(0.5f, 0.5f);
            Entry(root, "ArrowOut", "Arrow Out", "btn_white", "icon_next", 200);
            Entry(root, "EyeBlast", "Eye Blast", "btn_green", "icon_levels", 0);
            Entry(root, "EyeMerge", "Eye Merge", "btn_yellow", "icon_star", -200);

            var face = UIKit.Image(root, "circle_fill", new Vector2(0.5f, 0f), new Vector2(0, 330), new Vector2(260, 260), UIKit.Hex("#FFD23F"));
            UIKit.Image(face.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 260));
            var f = UIKit.Image(face.transform, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(212, 212));
            f.gameObject.AddComponent<EmojiFace>().SetRandom();
        }

        private static void Entry(Transform parent, string scene, string label, string sprite, string icon, float y)
        {
            var b = UIKit.Button(parent, sprite, label, () => SceneFlow.Load(scene), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(700, 160), icon, 64);
            UIKit.SetInteractable(b, Application.CanStreamedLevelBeLoaded(scene));
        }
    }
}
