using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>
    /// Piezas visuales de la interfaz V14 ("cuaderno de campo"): vidrio oscuro con filete crema, teclas dibujadas,
    /// degradados para leer texto sobre el juego sin cajas pesadas y el rombo de acento. Sprites generados por código
    /// una vez (bordes suaves, 9-slice) y reutilizados.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Glass = new Color(.035f, .06f, .045f, .78f);
        public static readonly Color GlassSolid = new Color(.04f, .065f, .05f, .95f);
        public static readonly Color Hairline = new Color(.96f, .92f, .8f, .16f);
        public static readonly Color Shade = new Color(0, 0, 0, .55f);

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // ------------------------------------------------------------------ sprites
        /// <summary>Rectángulo redondeado lleno (radio 12 px), 9-slice.</summary>
        public static Sprite Fill => Rounded("Relleno", 64, 12, 0);
        /// <summary>Solo el contorno (1,5 px) del mismo rectángulo, para el filete.</summary>
        public static Sprite Frame => Rounded("Filete", 64, 12, 1.5f);
        /// <summary>Tecla: cuadrado redondeado con cara clara y canto inferior más oscuro.</summary>
        public static Sprite Key => Get("Tecla", 64, 64, 16, (x, y) =>
        {
            float a = RoundedAlpha(x, y, 64, 64, 12);
            float face = RoundedAlpha(x, y - 5, 64, 59, 11);      // la cara está 5 px más arriba que el canto
            float v = Mathf.Lerp(.62f, 1f, face);
            return new Color(v, v, v, a);
        });
        /// <summary>Degradado horizontal: opaco a la izquierda, transparente a la derecha.</summary>
        public static Sprite FadeRight => Get("DegradadoDer", 256, 4, 0, (x, y) => new Color(1, 1, 1, Ease(1 - x / 255f)));
        /// <summary>Degradado vertical: opaco abajo, transparente arriba.</summary>
        public static Sprite FadeUp => Get("DegradadoArr", 4, 256, 0, (x, y) => new Color(1, 1, 1, Ease(1 - y / 255f)));
        /// <summary>Banda que se desvanece a ambos lados (fondo de avisos y de la brújula).</summary>
        public static Sprite Band => Get("Banda", 256, 4, 0, (x, y) => { float t = Mathf.Abs(x / 255f * 2 - 1); return new Color(1, 1, 1, Ease(1 - t)); });
        /// <summary>Sombra suave de esquina (oscurece detrás del texto del HUD).</summary>
        public static Sprite Corner => Get("Esquina", 256, 256, 0, (x, y) =>
        {
            float d = new Vector2(x / 255f, 1 - y / 255f).magnitude;   // 0 en la esquina superior izquierda
            return new Color(1, 1, 1, Ease(Mathf.Clamp01(1 - d)));
        });
        /// <summary>Rombo de acento.</summary>
        public static Sprite Diamond => Get("Rombo", 32, 32, 0, (x, y) =>
        {
            float d = Mathf.Abs(x + .5f - 16) + Mathf.Abs(y + .5f - 16);
            return new Color(1, 1, 1, Mathf.Clamp01(15f - d));
        });
        /// <summary>Círculo suave (asa de deslizadores, interruptores).</summary>
        public static Sprite Dot => Get("Punto", 64, 64, 0, (x, y) => new Color(1, 1, 1, Mathf.Clamp01(31.5f - new Vector2(x - 31.5f, y - 31.5f).magnitude)));

        private static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        private static Sprite Rounded(string name, int n, float r, float stroke) => Get(name, n, n, (int)r + 4, (x, y) =>
        {
            float outer = RoundedAlpha(x, y, n, n, r);
            if (stroke <= 0) return new Color(1, 1, 1, outer);
            float inner = RoundedAlpha(x - (int)stroke, y - (int)stroke, n - 2 * (int)stroke, n - 2 * (int)stroke, r - stroke);
            if (x < stroke || y < stroke || x >= n - stroke || y >= n - stroke) inner = 0;
            return new Color(1, 1, 1, Mathf.Clamp01(outer - inner));
        });

        private static float RoundedAlpha(float x, float y, float w, float h, float r)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return 0;
            float dx = Mathf.Max(r - x - .5f, x + .5f - (w - r), 0), dy = Mathf.Max(r - y - .5f, y + .5f - (h - r), 0);
            return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
        }

        private static Sprite Get(string name, int w, int h, int border, Func<int, int, Color> px)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "UI_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var c = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) c[y * w + x] = px(x, y);
            tex.SetPixels(c); tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.name = tex.name; cache[name] = s;
            return s;
        }

        // ------------------------------------------------------------------ componentes
        /// <summary>Panel de vidrio oscuro con filete crema (sustituye a las cajas macizas).</summary>
        public static Image GlassPanel(Transform parent, string name, Color? color = null, bool raycast = true)
        {
            var img = Ui.Image(parent, name, color ?? Glass, Fill, raycast); img.type = Image.Type.Sliced;
            var f = Ui.Image(img.transform, "Filete", Hairline, Frame); f.type = Image.Type.Sliced; f.rectTransform.Stretch();
            return img;
        }

        /// <summary>Sombra suave detrás del texto (subyacente de TextMeshPro): se lee sobre el juego sin caja.</summary>
        public static T Shadowed<T>(T t, float alpha = .75f) where T : TMP_Text
        {
            var m = t.fontMaterial;
            m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, alpha));
            m.SetFloat(ShaderUtilities.ID_UnderlayDilate, .2f); m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .55f);
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -.35f);
            return t;
        }

        /// <summary>Tecla dibujada con su letra (E, T, Esc…). Devuelve su ancho.</summary>
        public static RectTransform KeyCap(Transform parent, string key, float height = 40f, Color? tint = null)
        {
            float w = Mathf.Max(height, 18 + key.Length * height * .36f);
            var img = Ui.Image(parent, "Tecla_" + key, tint ?? new Color(.93f, .9f, .8f), Key); img.type = Image.Type.Sliced;
            img.rectTransform.sizeDelta = new Vector2(w, height);
            var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = height;
            var t = Ui.Text(img.transform, "Letra", key, height * (key.Length > 2 ? .42f : .55f), Ui.Theme?.bold, new Color(.1f, .12f, .09f), TextAlignmentOptions.Center);
            t.rectTransform.Stretch(2, 2, 0, height * .12f);
            return img.rectTransform;
        }
    }

    /// <summary>
    /// Fila de atajos "T: rastrear · M: mapa": cada tramo "tecla: acción" se dibuja como tecla + texto.
    /// Lo que no tenga ese formato se muestra como texto. Solo se reconstruye cuando cambia el texto.
    /// </summary>
    public sealed class KeyHintRow
    {
        private readonly RectTransform row;
        private readonly float size;
        private readonly Color textColor;
        private string current;
        public string Text => current;

        public KeyHintRow(Transform parent, string name, TextAnchor align, float size = 21f, Color? color = null)
        {
            this.size = size; textColor = color ?? new Color(1, 1, 1, .78f);
            row = Ui.Rect(name, parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10; h.childAlignment = align; h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
        }

        public RectTransform Rect => row;

        public void Set(string text)
        {
            if (text == current) return;
            current = text;
            for (int i = row.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(row.GetChild(i).gameObject);
            if (string.IsNullOrEmpty(text)) return;
            var parts = text.Split(new[] { " · ", "  ·  ", "   ·   " }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i].Trim();
                int colon = p.IndexOf(':');
                if (colon > 0 && colon <= 6)
                {
                    UiKit.KeyCap(row, p.Substring(0, colon).Trim(), size * 1.45f);
                    Label(p.Substring(colon + 1).Trim(), i < parts.Length - 1 ? 22f : 0f);
                }
                else Label(p, i < parts.Length - 1 ? 22f : 0f);
            }
        }

        private void Label(string s, float gapAfter)
        {
            var t = UiKit.Shadowed(Ui.Text(row, "Texto", s, size, Ui.Theme?.body, textColor, TextAlignmentOptions.MidlineLeft));
            t.textWrappingMode = TextWrappingModes.NoWrap;
            float w = t.GetPreferredValues(s).x + 2 + gapAfter;
            t.rectTransform.sizeDelta = new Vector2(w, size * 1.6f);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = w; le.preferredHeight = size * 1.6f;
        }
    }

    /// <summary>
    /// Efecto de los elementos de menú: al pasar el ratón o seleccionar con teclado o mando, entra un velo con
    /// degradado, el rombo de acento crece y el texto se desliza y se ilumina. Animado con tiempo real (funciona en pausa).
    /// </summary>
    public sealed class MenuItemFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image glow, marker;
        public TextMeshProUGUI label;
        public Color idleText = new Color(.86f, .85f, .78f), activeText = Color.white;
        private bool selected, hover;
        private float k;
        private Vector2 labelBase;

        private void Awake() { if (label != null) labelBase = label.rectTransform.anchoredPosition; }
        private void OnEnable() { k = 0; Apply(); }
        public void OnSelect(BaseEventData e) => selected = true;
        public void OnDeselect(BaseEventData e) => selected = false;
        public void OnPointerEnter(PointerEventData e) { hover = true; GetComponent<Selectable>()?.Select(); }
        public void OnPointerExit(PointerEventData e) => hover = false;

        private void Update()
        {
            float target = selected || hover ? 1 : 0;
            if (Mathf.Approximately(k, target)) return;
            k = Mathf.MoveTowards(k, target, Time.unscaledDeltaTime * 7f);
            Apply();
        }

        private void Apply()
        {
            float e = k * k * (3 - 2 * k);
            if (glow != null) glow.color = new Color(UiTheme.Moss.r, UiTheme.Moss.g, UiTheme.Moss.b, .28f * e);
            if (marker != null) { marker.color = Color.Lerp(new Color(1, 1, 1, .25f), UiTheme.Amber, e); marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(.55f, 1f, e); }
            if (label != null) { label.color = Color.Lerp(idleText, activeText, e); label.rectTransform.anchoredPosition = labelBase + new Vector2(10f * e, 0); }
        }
    }
}
