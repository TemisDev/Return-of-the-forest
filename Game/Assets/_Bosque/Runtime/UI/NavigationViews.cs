using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>
    /// Orientación en zonas grandes (las que implementan <see cref="IZoneMap"/>): brújula arriba, con los puntos
    /// cardinales, los puntos de reciclaje y lo que ya se ha ubicado; y minimapa redondo que gira con la cámara.
    /// </summary>
    public sealed class NavigationView
    {
        private const float CompassWidth = 580f, HalfFov = 90f, MiniSize = 300f, MiniRange = 55f;
        private const int CompassPending = 3;

        private readonly CanvasGroup group;
        private readonly RectTransform strip, mini, rotor, playerArrow, north;
        private readonly RawImage miniMap;
        private readonly TextMeshProUGUI caption;
        private readonly List<(TextMeshProUGUI text, float heading)> cardinals = new List<(TextMeshProUGUI, float)>();
        private readonly List<(RectTransform rt, float heading)> ticks = new List<(RectTransform, float)>();
        private readonly List<(RectTransform rt, Image icon, TextMeshProUGUI label)> compassMarks = new List<(RectTransform, Image, TextMeshProUGUI)>();
        private readonly List<Image> miniMarks = new List<Image>();
        private readonly List<MapMarker> buffer = new List<MapMarker>();
        private static Sprite circle;

        public bool Visible => group.alpha > .5f;
        /// <summary>Rumbo de la cámara en grados (0 norte, 90 este).</summary>
        public float Heading { get; private set; }
        public string HeadingName => Cardinal(Heading);
        public int CompassMarkersShown { get; private set; }
        public int MinimapMarkersShown { get; private set; }
        public string CaptionText => caption.text;

        public NavigationView(Transform root)
        {
            var t = Ui.Theme;
            var rt = Ui.Rect("Orientacion", root).Stretch();
            group = rt.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false; group.alpha = 0;

            // Brújula.
            var bar = Ui.Image(rt, "Brujula", new Color(0, 0, 0, .55f), UiKit.Band).rectTransform;
            bar.Place(new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(CompassWidth + 220, 78));
            Ui.Image(bar, "Linea", new Color(.96f, .92f, .8f, .28f), UiKit.Band).rectTransform.Place(new Vector2(.5f, 0), new Vector2(.5f, .5f), new Vector2(0, 2), new Vector2(CompassWidth, 1.5f));
            strip = Ui.Rect("Franja", bar); strip.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(CompassWidth, 78));
            strip.gameObject.AddComponent<RectMask2D>();
            for (int h = 0; h < 360; h += 15)
            {
                if (h % 45 == 0) continue;
                var tick = Ui.Image(strip, "Marca", new Color(1, 1, 1, .35f)).rectTransform;
                tick.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 16), new Vector2(2, 12));
                ticks.Add((tick, h));
            }
            string[] names = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            for (int i = 0; i < 8; i++)
            {
                bool major = i % 2 == 0;
                var c = Ui.Text(strip, "Punto_" + names[i], names[i], major ? 32 : 22, t?.bold, i == 0 ? UiTheme.Amber : major ? UiTheme.Cream : UiTheme.CreamDim, TextAlignmentOptions.Center);
                c.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 14), new Vector2(60, 40));
                cardinals.Add((c, i * 45f));
            }
            var needle = Ui.Image(bar, "Aguja", UiTheme.Amber, t?.pill).rectTransform;
            needle.Place(new Vector2(.5f, 0), new Vector2(.5f, .5f), new Vector2(0, 2), new Vector2(28, 6));

            // Minimapa.
            var ring = Ui.Image(rt, "Minimapa", new Color(0, 0, 0, .45f), MapIcons.Disc).rectTransform;
            ring.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-34, 80), new Vector2(MiniSize + 16, MiniSize + 16));
            // Fondo de papel: fuera del mapa se ve papel, no un hueco oscuro.
            var maskImg = Ui.Image(ring, "Mascara", MapIcons.Paper, MapIcons.Disc);
            mini = maskImg.rectTransform; mini.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(MiniSize, MiniSize));
            mini.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            rotor = Ui.Rect("Giro", mini); rotor.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            var mapRt = Ui.Rect("Mapa", rotor); mapRt.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 100));
            miniMap = mapRt.gameObject.AddComponent<RawImage>(); miniMap.raycastTarget = false;
            playerArrow = Ui.Image(mini, "Guardiana", Color.white, MapIcons.Player).rectTransform;
            playerArrow.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30));
            // Marco: borde oscuro con filete crema, encima del mapa (tapa el borde dentado de la máscara).
            Ui.Image(ring, "Marco", Color.white, MapIcons.MiniFrame).rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(MiniSize + 16, MiniSize + 16));
            var nBg = Ui.Image(ring, "Norte", new Color(.07f, .09f, .07f), MapIcons.Disc); north = nBg.rectTransform;
            north.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(34, 34));
            var nTxt = Ui.Text(north, "N", "N", 24, t?.bold, UiTheme.Amber, TextAlignmentOptions.Center); nTxt.rectTransform.Stretch();
            caption = Ui.Text(rt, "Ubicados", "", 22, t?.body, UiTheme.CreamDim, TextAlignmentOptions.Bottom);
            caption.rectTransform.Place(new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(-34 - (MiniSize + 16) * .5f, 80 + MiniSize + 22), new Vector2(420, 32));
            caption.outlineWidth = .2f; caption.outlineColor = Color.black;
        }

        /// <summary>Sprite circular generado una vez (máscara y fondo del minimapa).</summary>
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "UI_Circulo", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n * .5f, n * .5f));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(n * .5f - d) * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
            return circle;
        }

        public static string Cardinal(float heading)
        {
            string[] names = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            return names[Mathf.RoundToInt(Mathf.Repeat(heading, 360f) / 45f) % 8];
        }

        public void Tick(IZoneMap map, bool show)
        {
            var player = map?.MapPlayer;
            bool on = show && player != null && (map as Object) != null;
            group.alpha = Mathf.MoveTowards(group.alpha, on ? 1 : 0, Time.unscaledDeltaTime * 4f);
            if (!on) return;
            var cam = Camera.main;
            Heading = cam != null ? cam.transform.eulerAngles.y : player.eulerAngles.y;
            var p = player.position;

            // Brújula: puntos cardinales y marcas cada 15°.
            foreach (var (text, h) in cardinals) Place(text.rectTransform, h, 14, true);
            foreach (var (rt, h) in ticks) Place(rt, h, 16, true);

            buffer.Clear(); buffer.AddRange(map.MapMarkers);
            // Puntos de reciclaje (el más cercano resaltado) y los pendientes ya ubicados más cercanos.
            int used = 0;
            float nearestStation = float.MaxValue;
            foreach (var m in buffer) if (m.kind == MarkerKind.Station) nearestStation = Mathf.Min(nearestStation, Flat(m.position - p));
            foreach (var m in buffer)
            {
                if (m.kind != MarkerKind.Station) continue;
                float d = Flat(m.position - p);
                bool nearest = Mathf.Approximately(d, nearestStation);
                SetCompassMark(used++, m, nearest ? $"{Short(m.label)} · {Mathf.RoundToInt(d)} m" : $"{Mathf.RoundToInt(d)} m", nearest ? 1f : .7f, p);
            }
            buffer.Sort((a, b) => Flat(a.position - p).CompareTo(Flat(b.position - p)));
            int pending = 0, totalPending = 0;
            foreach (var m in buffer)
            {
                if (m.kind != MarkerKind.Litter && m.kind != MarkerKind.Wood) continue;
                totalPending++;
                if (pending >= CompassPending) continue;
                pending++;
                SetCompassMark(used++, m, $"{Mathf.RoundToInt(Flat(m.position - p))} m", 1f, p);
            }
            for (int i = used; i < compassMarks.Count; i++) compassMarks[i].rt.gameObject.SetActive(false);
            CompassMarkersShown = used;
            caption.text = totalPending == 0 ? "Explora para ubicar lo que falta  ·  T: rastrear" : totalPending == 1 ? "1 ubicado por limpiar" : $"{totalPending} ubicados por limpiar";

            // Minimapa: gira con la cámara; la Guardiana en el centro.
            float ppm = MiniSize / (2f * MiniRange);
            var w = map.MapWorldRect;
            rotor.localRotation = Quaternion.Euler(0, 0, Heading);
            miniMap.texture = map.MapTexture;
            miniMap.rectTransform.sizeDelta = new Vector2(w.width * ppm, w.height * ppm);
            miniMap.rectTransform.anchoredPosition = new Vector2((w.center.x - p.x) * ppm, (w.center.y - p.z) * ppm);
            playerArrow.localRotation = Quaternion.Euler(0, 0, Heading - player.eulerAngles.y);
            float a = Heading * Mathf.Deg2Rad, r = MiniSize * .5f + 2f;
            north.anchoredPosition = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * r;
            int k = 0;
            foreach (var m in buffer)
            {
                if (m.kind == MarkerKind.Label || m.kind == MarkerKind.Point || m.kind == MarkerKind.Region) continue;
                var off = new Vector2(m.position.x - p.x, m.position.z - p.z);
                if (off.magnitude > MiniRange * 1.05f) continue;
                if (k >= miniMarks.Count) { var img = Ui.Image(rotor, "Marca", Color.white, Circle()); miniMarks.Add(img); }
                var mk = miniMarks[k++];
                mk.gameObject.SetActive(true);
                mk.sprite = MapIcons.For(m.kind); mk.color = Color.white;
                mk.rectTransform.sizeDelta = Vector2.one * (m.kind == MarkerKind.Station ? 26f : m.kind == MarkerKind.Bridge ? 24f : m.kind == MarkerKind.Ford ? 22f : 16f);
                mk.rectTransform.anchoredPosition = off * ppm;
                // Puentes y vados giran con el mapa (marcan la dirección del cruce); el resto queda derecho.
                bool aligned = m.kind == MarkerKind.Bridge || m.kind == MarkerKind.Ford;
                mk.rectTransform.localRotation = Quaternion.Euler(0, 0, aligned ? m.angle : -Heading);
            }
            for (int i = k; i < miniMarks.Count; i++) miniMarks[i].gameObject.SetActive(false);
            MinimapMarkersShown = k;
            playerArrow.SetAsLastSibling();
        }

        public static Color MarkerColor(MapMarker m) => m.kind switch
        {
            MarkerKind.Station => UiTheme.Moss,
            MarkerKind.Litter => UiTheme.Amber,
            MarkerKind.Wood => new Color(.78f, .55f, .32f),
            MarkerKind.Bridge => new Color(.62f, .45f, .28f),
            MarkerKind.Ford => new Color(.62f, .85f, 1f),
            _ => m.color,
        };

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
        private static string Short(string station) => string.IsNullOrEmpty(station) ? "" : station.Replace("Puesto de la ", "").Replace("Puesto del ", "");

        private void SetCompassMark(int index, MapMarker m, string text, float alpha, Vector3 from)
        {
            while (compassMarks.Count <= index)
            {
                var root = Ui.Rect("Objetivo", strip);
                root.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(10, 10));
                var icon = Ui.Image(root, "Icono", Color.white, Circle());
                icon.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -6), new Vector2(16, 16));
                var label = Ui.Text(root, "Texto", "", 18, Ui.Theme?.bold, UiTheme.Cream, TextAlignmentOptions.Center);
                label.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -25), new Vector2(240, 22));
                label.outlineWidth = .2f; label.outlineColor = Color.black;
                compassMarks.Add((root, icon, label));
            }
            var (rt, ic, lb) = compassMarks[index];
            rt.gameObject.SetActive(true);
            float bearing = Mathf.Atan2(m.position.x - from.x, m.position.z - from.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(Heading, bearing);
            bool edge = Mathf.Abs(delta) > HalfFov;
            float x = Mathf.Clamp(delta / HalfFov, -1f, 1f) * (CompassWidth * .5f - (edge ? 14f : 0f));
            rt.anchoredPosition = new Vector2(x, 0);
            ic.sprite = MapIcons.For(m.kind);
            ic.color = new Color(1, 1, 1, edge ? .5f : alpha);
            ic.rectTransform.sizeDelta = Vector2.one * (m.kind == MarkerKind.Station ? 22f : 16f);
            lb.text = edge ? "" : text; lb.color = new Color(1, 1, 1, alpha);
        }

        private void Place(RectTransform rt, float heading, float y, bool hideOutside)
        {
            float delta = Mathf.DeltaAngle(Heading, heading);
            bool visible = Mathf.Abs(delta) <= HalfFov + 6f;
            if (rt.gameObject.activeSelf != visible) rt.gameObject.SetActive(visible);
            if (visible) rt.anchoredPosition = new Vector2(delta / HalfFov * CompassWidth * .5f, y);
        }
    }
}
