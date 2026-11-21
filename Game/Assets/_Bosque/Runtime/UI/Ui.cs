using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Bosque.UI
{
    /// <summary>Fábrica de interfaz uGUI + TextMeshPro con el estilo del juego (lienzo de referencia 1920×1080).</summary>
    public static class Ui
    {
        private static UiTheme theme;
        public static UiTheme Theme => theme != null ? theme : (theme = Resources.Load<UiTheme>("BosqueUiTheme"));

        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = .5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem(Transform parent)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Coloca un rectángulo: ancla (0..1), pivote, posición y tamaño en píxeles de referencia.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = sprite; img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name, Color? color = null) =>
            Image(parent, name, color ?? UiTheme.Panel, Theme != null ? Theme.panel : null, true);

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, TMP_FontAsset font = null,
            Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font == null && Theme != null) font = Theme.body;
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color ?? UiTheme.Cream; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false;
            return t;
        }

        public static RawImage Icon(Transform parent, string name, Texture2D tex, Color tint)
        {
            var rt = Rect(name, parent);
            var r = rt.gameObject.AddComponent<RawImage>(); r.texture = tex; r.color = tint; r.raycastTarget = false;
            return r;
        }

        /// <summary>Botón con fondo redondeado, estados de color y texto centrado.</summary>
        public static Button Button(Transform parent, string label, Action onClick, float width = 420, float height = 64, float fontSize = 30)
        {
            var img = Image(parent, "Boton_" + label, Color.white, Theme != null ? Theme.pill : null, true);
            var rt = img.rectTransform; rt.sizeDelta = new Vector2(width, height);
            var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = width; le.preferredHeight = height;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = new Color(.1f, .17f, .13f, .9f); cb.highlightedColor = new Color(.27f, .38f, .2f, .97f);
            cb.selectedColor = new Color(.27f, .38f, .2f, .97f); cb.pressedColor = new Color(.42f, .55f, .3f, 1f);
            cb.disabledColor = new Color(.1f, .12f, .11f, .5f); cb.fadeDuration = .08f;
            b.colors = cb;
            var t = Text(rt, "Texto", label, fontSize, Theme != null ? Theme.bold : null, UiTheme.Cream, TextAlignmentOptions.Center);
            t.rectTransform.Stretch(16, 16, 0, 0);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static Slider Slider(Transform parent, string label, float min, float max, float value, Action<float> onChange, Func<float, string> format)
        {
            var row = Rect("Fila_" + label, parent); row.sizeDelta = new Vector2(720, 58);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 58; le.preferredWidth = 720;
            var name = Text(row, "Etiqueta", label, 27, null, UiTheme.Cream, TextAlignmentOptions.MidlineLeft);
            name.rectTransform.anchorMin = new Vector2(0, 0); name.rectTransform.anchorMax = new Vector2(.42f, 1); name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
            var valueText = Text(row, "Valor", format(value), 25, null, UiTheme.CreamDim, TextAlignmentOptions.MidlineRight);
            valueText.rectTransform.anchorMin = new Vector2(.86f, 0); valueText.rectTransform.anchorMax = Vector2.one; valueText.rectTransform.offsetMin = valueText.rectTransform.offsetMax = Vector2.zero;

            var area = Rect("Control", row); area.anchorMin = new Vector2(.44f, .5f); area.anchorMax = new Vector2(.84f, .5f);
            area.sizeDelta = new Vector2(0, 26); area.anchoredPosition = Vector2.zero;
            var bg = Image(area, "Fondo", new Color(1, 1, 1, .14f), Theme != null ? Theme.pill : null, true);
            bg.rectTransform.Stretch(0, 0, 8, 8);
            var fillArea = Rect("AreaRelleno", area).Stretch(0, 0, 8, 8);
            var fill = Image(fillArea, "Relleno", UiTheme.Moss, Theme != null ? Theme.pill : null);
            fill.rectTransform.Stretch();
            var handleArea = Rect("AreaAsa", area).Stretch(10, 10, 0, 0);
            var handle = Image(handleArea, "Asa", UiTheme.Cream, Theme != null ? Theme.pill : null, true);
            handle.rectTransform.anchorMin = Vector2.zero; handle.rectTransform.anchorMax = new Vector2(0, 1);
            handle.rectTransform.sizeDelta = new Vector2(26, 6);
            var s = area.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.SetValueWithoutNotify(value);
            s.onValueChanged.AddListener(v => { valueText.text = format(v); onChange(v); });
            return s;
        }

        public static Toggle Toggle(Transform parent, string label, bool value, Action<bool> onChange)
        {
            var row = Rect("Fila_" + label, parent); row.sizeDelta = new Vector2(720, 58);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 58; le.preferredWidth = 720;
            var name = Text(row, "Etiqueta", label, 27, null, UiTheme.Cream, TextAlignmentOptions.MidlineLeft);
            name.rectTransform.anchorMin = Vector2.zero; name.rectTransform.anchorMax = new Vector2(.8f, 1); name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
            var box = Image(row, "Casilla", new Color(1, 1, 1, .14f), Theme != null ? Theme.pill : null, true);
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(.44f, .5f); box.rectTransform.pivot = new Vector2(0, .5f);
            box.rectTransform.sizeDelta = new Vector2(64, 32); box.rectTransform.anchoredPosition = Vector2.zero;
            var mark = Image(box.transform, "Marca", UiTheme.Moss, Theme != null ? Theme.pill : null);
            mark.rectTransform.Stretch(4, 4, 4, 4);
            var t = row.gameObject.AddComponent<Toggle>(); t.targetGraphic = box; t.graphic = mark; t.SetIsOnWithoutNotify(value);
            t.onValueChanged.AddListener(v => onChange(v));
            return t;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, TextAnchor align = TextAnchor.UpperCenter, int padding = 0)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.childAlignment = align; v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlWidth = v.childControlHeight = false; v.childForceExpandWidth = v.childForceExpandHeight = false;
            return v;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
