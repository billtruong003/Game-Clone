using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// The loading bar, drawn as a tiny version of the game it loads: an arrow runs the bar and yeets off (Bruh Arrows),
    /// blocks drop into slots and the full row clears (Nah Blocks), a ball rolls along and merges at the end (Meh Merge).
    /// Built from the shared sprites every game's art library has (bar_fill, dot, block_fill, circle_fill). The bar
    /// sprite is 9-sliced and never narrower than it is tall, so its round ends never squash.
    /// </summary>
    public sealed class BootBar
    {
        private enum Kind { Arrow, Blocks, Ball }

        private const float Width = 760f, Height = 56f, Inset = 8f, Slots = 8;
        private static readonly Color Ink = UIKit.Hex("#1E2240");
        private static readonly Color[] BlockColors = { UIKit.Hex("#4EA8DE"), UIKit.Hex("#3DDC97"), UIKit.Hex("#FF5A5F"), UIKit.Hex("#FFD23F"), UIKit.Hex("#F15BB5") };

        private readonly Kind kind;
        private readonly RectTransform root, fill, runner;
        private readonly Image[] blocks;
        private int shownBlocks;

        public BootBar(RectTransform parent, string gameId, Color ground)
        {
            kind = gameId == "ArrowOut" ? Kind.Arrow : gameId == "EyeBlast" ? Kind.Blocks : Kind.Ball;
            var light = 0.2126f * ground.r + 0.7152f * ground.g + 0.0722f * ground.b > 0.5f;
            var accent = kind == Kind.Arrow ? UIKit.Hex("#35B09F") : UIKit.Hex("#FFD23F");

            root = UIKit.Place(UIKit.Rect("BootBar", parent), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, Height));
            var track = UIKit.Image(root, "bar_fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, Height), Ink);
            track.type = Image.Type.Sliced;
            var inner = UIKit.Image(root, "bar_fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width - 2 * Inset, Height - 2 * Inset),
                light ? UIKit.Hex("#E4DCCF") : UIKit.Hex("#141633"));
            inner.type = Image.Type.Sliced;

            if (kind == Kind.Blocks)
            {
                // eight slots instead of a fill: one block lands in each as the load advances
                blocks = new Image[(int)Slots];
                var step = (Width - 2 * Inset) / Slots;
                for (int i = 0; i < Slots; i++)
                {
                    var x = -Width / 2f + Inset + step * (i + 0.5f);
                    var b = UIKit.Image(root, "block_fill", new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(step - 8, step - 8), BlockColors[i % BlockColors.Length]);
                    UIKit.Image(b.transform, "block_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(step - 8, step - 8));
                    b.transform.localScale = Vector3.zero;
                    blocks[i] = b;
                }
                return;
            }

            var f = UIKit.Image(root, "bar_fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(Height - 2 * Inset, Height - 2 * Inset), accent);
            f.type = Image.Type.Sliced;
            fill = f.rectTransform;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(Inset, 0f);

            runner = UIKit.Rect("Runner", root);
            runner.anchorMin = runner.anchorMax = new Vector2(0f, 0.5f);
            runner.sizeDelta = Vector2.zero;
            if (kind == Kind.Arrow)
            {
                // a little arrow: round tail, body, chevron, all in the arrow teal; bigger than the bar so it reads
                runner.localScale = Vector3.one * 1.5f;
                UIKit.Image(runner, "dot", new Vector2(0.5f, 0.5f), new Vector2(-34, 0), new Vector2(46, 46), accent);
                UIKit.Image(runner, "bar_fill", new Vector2(0.5f, 0.5f), new Vector2(-6, 0), new Vector2(64, 22), accent).type = Image.Type.Sliced;
                foreach (var sign in new[] { 1f, -1f })
                {
                    var arm = UIKit.Image(runner, "bar_fill", new Vector2(0.5f, 0.5f), new Vector2(16, 9 * sign), new Vector2(40, 20), accent);
                    arm.type = Image.Type.Sliced;
                    arm.rectTransform.localEulerAngles = new Vector3(0, 0, -45f * sign);
                }
            }
            else
            {
                var ball = UIKit.Image(runner, "circle_fill", new Vector2(0.5f, 0.5f), new Vector2(0, 46), new Vector2(64, 64), UIKit.Hex("#F15BB5"));
                UIKit.Image(ball.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            }
        }

        public RectTransform Root => root;

        /// <summary>Shows the (already smoothed) progress 0..1.</summary>
        public void Set(float p)
        {
            p = Mathf.Clamp01(p);
            if (kind == Kind.Blocks)
            {
                var n = Mathf.Min((int)Slots, Mathf.FloorToInt(p * Slots + 0.001f));
                for (; shownBlocks < n; shownBlocks++)
                {
                    var t = blocks[shownBlocks].transform;
                    Tween.Scale(t, Vector3.one, 0.22f, Ease.OutBack);
                }
                return;
            }
            var inner = Width - 2 * Inset;
            var w = Mathf.Max(Height - 2 * Inset, inner * p); // a sliced pill never narrower than its height
            fill.sizeDelta = new Vector2(w, Height - 2 * Inset);
            runner.anchoredPosition = new Vector2(Inset + w - (kind == Kind.Arrow ? 6f : 30f), 0f);
            if (kind == Kind.Ball) runner.GetChild(0).localEulerAngles = new Vector3(0, 0, -p * 720f); // the ball rolls on the bar
        }

        /// <summary>The game's own little finish at 100 %, then returns.</summary>
        public IEnumerator Payoff()
        {
            switch (kind)
            {
                case Kind.Arrow:
                    GameAudio.Play("fly");
                    var from = runner.anchoredPosition;
                    Tween.Run(runner, 0.35f, k => runner.anchoredPosition = from + new Vector2(k * 900f, 0f), Ease.InQuad, 0f, null, true);
                    yield return new WaitForSecondsRealtime(0.35f);
                    break;
                case Kind.Blocks:
                    GameAudio.Play("clear");
                    for (int i = 0; i < blocks.Length; i++)
                    {
                        var t = blocks[i].transform;
                        Tween.Scale(t, Vector3.one * 1.18f, 0.08f, Ease.OutQuad, i * 0.03f, () => Tween.Scale(t, Vector3.zero, 0.16f, Ease.InBack));
                    }
                    yield return new WaitForSecondsRealtime(0.45f);
                    break;
                default:
                    // a second ball rolls in from the right, the two squash together into a bigger one
                    GameAudio.Play("merge");
                    var ball = runner.GetChild(0) as RectTransform;
                    var twin = Object.Instantiate(ball, runner);
                    twin.anchoredPosition = ball.anchoredPosition + new Vector2(140f, 0f);
                    Tween.Anchored(twin, ball.anchoredPosition + new Vector2(30f, 0f), 0.2f, Ease.InQuad);
                    yield return new WaitForSecondsRealtime(0.2f);
                    Object.Destroy(twin.gameObject);
                    ball.localScale = Vector3.one * 0.6f;
                    ball.GetComponent<Image>().color = UIKit.Hex("#FF5A5F");
                    Tween.Scale(ball, Vector3.one * 1.45f, 0.3f, Ease.OutBack, 0f, null);
                    yield return new WaitForSecondsRealtime(0.3f);
                    break;
            }
        }
    }
}
