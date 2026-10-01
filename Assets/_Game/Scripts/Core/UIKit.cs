using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// Code-first UI builder. All screens are assembled from sliced sprites in <see cref="ArtLibrary"/>, so swapping
    /// the art (e.g. ChatGPT versions) restyles every game without touching layouts.
    /// Reference resolution is 1080×1920 portrait.
    /// </summary>
    public static class UIKit
    {
        public static readonly Vector2 Reference = new(1080, 1920);
        public static readonly Color Ink = Hex("#1E2240");
        public static readonly Color Paper = Hex("#FFF8EC");
        public static readonly Color Muted = Hex("#6B7090");

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : UnityEngine.Color.magenta;

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            SetupScaler(go);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Screen-space-camera canvas: particles with a higher sorting order draw on top of it.</summary>
        public static Canvas CreateCameraCanvas(string name, Camera camera, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = sortingOrder;
            SetupScaler(go);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void SetupScaler(GameObject go)
        {
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            go.AddComponent<CanvasFit>();
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Anchors at a normalized point of the parent (0..1) and places the element at an offset from it.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            return rt;
        }

        public static UnityEngine.UI.Image AddImage(RectTransform rt, string spriteName, Color? color = null) =>
            AddImage(rt, spriteName != null ? ArtLibrary.Instance.Get(spriteName) : null, color);

        public static UnityEngine.UI.Image AddImage(RectTransform rt, Sprite sprite, Color? color = null)
        {
            var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color ?? UnityEngine.Color.white;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static UnityEngine.UI.Image Image(Transform parent, string spriteName, Vector2 anchor, Vector2 offset, Vector2 size, Color? color = null)
        {
            var rt = Place(Rect(spriteName ?? "Image", parent), anchor, offset, size);
            return AddImage(rt, spriteName, color);
        }

        // Where the index fingertip sits in the hand sprites (Tools/art/make-hands.py), as a pivot (bottom-left origin).
        private static readonly Vector2 HandTip = new(0.16f, 0.81f);

        /// <summary>The pointing hand (painted art, Art/Sheets/hands) with its fingertip exactly on <paramref name="tip"/>.</summary>
        /// <param name="mirror">Point up-right instead of up-left (the hand then comes from the lower-left).</param>
        public static RectTransform Hand(Transform parent, Vector2 anchor, Vector2 tip, float size = 190f, bool mirror = false)
        {
            var img = Image(parent, "hand_glove", anchor, tip, new Vector2(size, size));
            var rt = img.rectTransform;
            rt.pivot = HandTip;
            if (mirror) rt.localScale = new Vector3(-1f, 1f, 1f);
            rt.anchoredPosition = tip;
            img.preserveAspect = true;
            return rt;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color? color = null)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color ?? Ink;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = true;
            t.raycastTarget = false;
            return t;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 anchor, Vector2 offset, Vector2 box, Color? color = null)
        {
            var t = Label(parent, text, size, color);
            Place(t.rectTransform, anchor, offset, box);
            return t;
        }

        /// <summary>Pill button (9-sliced) with an optional icon to the left of the label.</summary>
        public static Button Button(Transform parent, string sprite, string label, Action onClick, Vector2 anchor, Vector2 offset,
            Vector2 size, string icon = null, float fontSize = 60f)
        {
            var rt = Place(Rect("Button", parent), anchor, offset, size);
            var bg = AddImage(rt, sprite);
            bg.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { GameAudio.Play("ui_click"); onClick?.Invoke(); });
            rt.gameObject.AddComponent<PressScale>();

            var text = Label(rt, label, fontSize);
            Stretch(text.rectTransform);
            // G11: a long label (Vietnamese, "Revive · clear 3 lines") shrinks to fit instead of spilling out
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * 0.6f;
            text.rectTransform.offsetMin = new Vector2(icon != null ? 90 : 20, 14);
            text.rectTransform.offsetMax = new Vector2(-20, 0);
            if (icon != null)
            {
                var ic = Image(rt, icon, new Vector2(0f, 0.5f), new Vector2(70, 4), new Vector2(72, 72));
                ic.preserveAspect = true;
            }
            return btn;
        }

        /// <summary>Round button with a centered icon, e.g. pause/settings/hint.</summary>
        public static Button IconButton(Transform parent, string bg, string icon, Action onClick, Vector2 anchor, Vector2 offset, float size = 132f)
        {
            var rt = Place(Rect(icon, parent), anchor, offset, new Vector2(size, size));
            var img = AddImage(rt, bg);
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { GameAudio.Play("ui_click"); onClick?.Invoke(); });
            rt.gameObject.AddComponent<PressScale>();
            var ic = Image(rt, icon, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(size * 0.56f, size * 0.56f));
            ic.preserveAspect = true;
            return btn;
        }

        public static void SetInteractable(Button b, bool on)
        {
            b.interactable = on;
            foreach (var g in b.GetComponentsInChildren<Graphic>()) g.color = new Color(g.color.r, g.color.g, g.color.b, on ? 1f : 0.35f);
        }
    }

    /// <summary>Squash on press, spring back on release.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 baseScale = Vector3.one;
        private bool pressed;

        private void Awake() => baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            pressed = true;
            Tween.Kill(transform);
            Tween.Scale(transform, baseScale * 0.92f, 0.06f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (!pressed) return;
            pressed = false;
            Tween.Kill(transform);
            Tween.Scale(transform, baseScale, 0.18f, Ease.OutBack);
        }
    }

    /// <summary>Portrait phones fit width; wider screens (tablets, foldables) fit height so nothing is cut off.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasFit : MonoBehaviour
    {
        private CanvasScaler scaler;
        private Vector2Int last;

        private void Awake() => scaler = GetComponent<CanvasScaler>();

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == last) return;
            last = size;
            var aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            scaler.matchWidthOrHeight = aspect > UIKit.Reference.x / UIKit.Reference.y ? 1f : 0f;
        }
    }

    /// <summary>Keeps a full-screen container inside the device safe area (notches, home indicator).</summary>
    public class SafeArea : MonoBehaviour
    {
        private Rect applied;

        private void Update()
        {
            var safe = Screen.safeArea;
            if (safe == applied || Screen.width == 0) return;
            applied = safe;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
