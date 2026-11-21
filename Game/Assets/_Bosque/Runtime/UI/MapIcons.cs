using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bosque.UI
{
    /// <summary>
    /// Iconos del mapa, el minimapa y la brújula, dibujados por código con campos de distancia (bordes suaves a
    /// cualquier tamaño): punto de reciclaje, residuo, tronco, puente y vado con símbolos de cartografía, la flecha de
    /// la Guardiana, la rosa de los vientos y el anillo del minimapa. Se generan una vez y se reutilizan.
    /// </summary>
    public static class MapIcons
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static readonly Color Ink = new Color(.24f, .17f, .1f);
        public static readonly Color WaterInk = new Color(.16f, .32f, .46f);
        public static readonly Color Paper = new Color(.94f, .9f, .8f);
        private static readonly Color Moss = new Color(.38f, .56f, .31f), MossDark = new Color(.17f, .25f, .13f);
        private static readonly Color Amber = new Color(.97f, .7f, .26f), AmberDark = new Color(.36f, .2f, .06f);

        public static Sprite For(MarkerKind kind) => kind switch
        {
            MarkerKind.Station => Station,
            MarkerKind.Litter => Litter,
            MarkerKind.Wood => Wood,
            MarkerKind.Bridge => Bridge,
            MarkerKind.Ford => Ford,
            _ => Dot,
        };

        public static Sprite Station => Get("Reciclaje", 128, p =>
        {
            var c = Layer(default, MossDark, Circle(p, Vector2.zero, .94f));
            c = Layer(c, Paper, Circle(p, Vector2.zero, .84f));
            c = Layer(c, Moss, Circle(p, Vector2.zero, .74f));
            // Contenedor con tapa y asa.
            c = Layer(c, Paper, Box(p, new Vector2(0, -.13f), new Vector2(.27f, .33f), .05f));
            c = Layer(c, Paper, Box(p, new Vector2(0, .29f), new Vector2(.36f, .05f), .03f));
            c = Layer(c, Paper, Box(p, new Vector2(0, .39f), new Vector2(.11f, .045f), .02f));
            foreach (var x in new[] { -.11f, .11f }) c = Layer(c, Moss, Segment(p, new Vector2(x, -.36f), new Vector2(x, .1f), .035f));
            return c;
        });

        public static Sprite Litter => Get("Residuo", 96, p =>
        {
            var c = Layer(default, AmberDark, Diamond(p, .92f));
            c = Layer(c, Amber, Diamond(p, .68f));
            return Layer(c, new Color(1f, .96f, .86f), Circle(p, Vector2.zero, .18f));
        });

        public static Sprite Wood => Get("Tronco", 96, p =>
        {
            var c = Layer(default, new Color(.22f, .13f, .06f), Circle(p, Vector2.zero, .92f));
            c = Layer(c, new Color(.5f, .32f, .17f), Circle(p, Vector2.zero, .8f));
            c = Layer(c, new Color(.88f, .7f, .45f), Circle(p, Vector2.zero, .62f));
            foreach (var r in new[] { .42f, .22f }) c = Layer(c, new Color(.62f, .43f, .24f), Mathf.Abs(Circle(p, Vector2.zero, r)) - .045f);
            return c;
        });

        /// <summary>Símbolo de puente de cartografía: dos trazos paralelos con los extremos abiertos (eje X = tablero).</summary>
        public static Sprite Bridge => Get("Puente", 128, p =>
        {
            var c = Layer(default, Paper, Box(p, Vector2.zero, new Vector2(.58f, .27f), 0));
            foreach (var y in new[] { -.27f, .27f })
            {
                c = Layer(c, Ink, Segment(p, new Vector2(-.58f, y), new Vector2(.58f, y), .07f));
                foreach (var x in new[] { -.58f, .58f }) c = Layer(c, Ink, Segment(p, new Vector2(x, y), new Vector2(x * 1.4f, y * 1.95f), .06f));
            }
            return c;
        });

        /// <summary>Vado: hilera de piedras (eje X = de orilla a orilla).</summary>
        public static Sprite Ford => Get("Vado", 128, p =>
        {
            Color c = default;
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector2(-.66f + i * .44f, i % 2 == 0 ? .06f : -.06f);
                c = Layer(c, Paper, Circle(p, at, .25f));
            }
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector2(-.66f + i * .44f, i % 2 == 0 ? .06f : -.06f);
                c = Layer(c, new Color(.27f, .31f, .33f), Circle(p, at, .17f));
            }
            return c;
        });

        public static Sprite Dot => Get("Punto", 64, p => Layer(Layer(default, Ink, Circle(p, Vector2.zero, .95f)), Color.white, Circle(p, Vector2.zero, .72f)));

        /// <summary>Flecha de la Guardiana (apunta hacia arriba).</summary>
        public static Sprite Player => Get("Guardiana", 128, p =>
        {
            Vector2[] arrow = { new Vector2(0, .86f), new Vector2(.6f, -.72f), new Vector2(0, -.4f), new Vector2(-.6f, -.72f) };
            var c = Layer(default, new Color(.12f, .07f, .03f), Polygon(p, arrow) - .13f);
            c = Layer(c, Amber, Polygon(p, arrow));
            Vector2[] lit = { new Vector2(0, .86f), new Vector2(0, -.4f), new Vector2(-.6f, -.72f) };
            return Layer(c, new Color(1f, .88f, .6f), Polygon(p, lit) + .02f);
        });

        /// <summary>Anillo suave (pulso de la posición).</summary>
        public static Sprite Ring => Get("Anillo", 128, p => Layer(default, Color.white, Mathf.Abs(Circle(p, Vector2.zero, .86f)) - .07f));

        /// <summary>Marco del minimapa: borde oscuro con filete crema (oculta el borde dentado de la máscara).</summary>
        public static Sprite MiniFrame => Get("MarcoMinimapa", 256, p =>
        {
            float r = p.magnitude;
            var c = Layer(default, new Color(.07f, .09f, .07f), Mathf.Abs(r - .955f) - .045f);
            return Layer(c, new Color(.86f, .8f, .64f), Mathf.Abs(r - .9f) - .011f);
        });

        /// <summary>Disco liso de 256 px para la máscara del minimapa (más suave que el círculo de 128 px).</summary>
        public static Sprite Disc => Get("Disco", 256, p => Layer(default, Color.white, Circle(p, Vector2.zero, .985f)));

        /// <summary>Rosa de los vientos: cuatro puntas principales (mitad tinta, mitad papel) y cuatro diagonales.</summary>
        public static Sprite Rose => Get("Rosa", 256, p =>
        {
            Color c = default;
            c = Layer(c, new Color(Paper.r, Paper.g, Paper.b, .85f), Circle(p, Vector2.zero, .66f));
            c = Layer(c, Ink, Mathf.Abs(Circle(p, Vector2.zero, .66f)) - .012f);
            c = Layer(c, Ink, Mathf.Abs(Circle(p, Vector2.zero, .58f)) - .006f);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var s = new Vector2(-d.y, d.x);
                c = Layer(c, Ink, Polygon(p, new[] { d * .62f, s * .08f, Vector2.zero }));
                c = Layer(c, Paper, Polygon(p, new[] { d * .62f, Vector2.zero, -s * .08f }));
                c = Layer(c, Ink, Mathf.Max(Polygon(p, new[] { d * .62f, Vector2.zero, -s * .08f }), -(Polygon(p, new[] { d * .62f, Vector2.zero, -s * .08f }) + .02f)));
            }
            for (int i = 0; i < 4; i++)
            {
                float a = (90f + i * 90f) * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var s = new Vector2(-d.y, d.x);
                var tipCol = i == 0 ? new Color(.55f, .16f, .1f) : Ink;
                var right = new[] { d * .97f, Vector2.zero, -s * .14f };
                c = Layer(c, tipCol, Polygon(p, new[] { d * .97f, s * .14f, Vector2.zero }));
                c = Layer(c, Paper, Polygon(p, right));
                c = Layer(c, tipCol, Mathf.Max(Polygon(p, right), -(Polygon(p, right) + .022f)));
            }
            return Layer(Layer(c, Ink, Circle(p, Vector2.zero, .07f)), Paper, Circle(p, Vector2.zero, .035f));
        });

        // ------------------------------------------------------------------ dibujo
        private static Sprite Get(string name, int n, Func<Vector2, Color> paint)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "UI_Mapa_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[n * n];
            pixel = 2f / n;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = paint(new Vector2((x + .5f) / n * 2f - 1f, (y + .5f) / n * 2f - 1f));
            tex.SetPixels(px); tex.Apply(true, true);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
            s.name = tex.name; cache[name] = s;
            return s;
        }

        private static float pixel = 1f / 64f;

        /// <summary>Pinta <paramref name="col"/> sobre <paramref name="dst"/> donde la distancia con signo es negativa (borde suave de un píxel).</summary>
        private static Color Layer(Color dst, Color col, float sdf)
        {
            float a = Mathf.Clamp01(.5f - sdf / pixel) * col.a;
            if (a <= 0f) return dst;
            float outA = a + dst.a * (1 - a);
            var rgb = (new Vector3(col.r, col.g, col.b) * a + new Vector3(dst.r, dst.g, dst.b) * dst.a * (1 - a)) / Mathf.Max(outA, 1e-5f);
            return new Color(rgb.x, rgb.y, rgb.z, outA);
        }

        private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        private static float Box(Vector2 p, Vector2 c, Vector2 h, float r)
        {
            var d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - h + Vector2.one * r;
            return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0) - r;
        }

        private static float Diamond(Vector2 p, float r) => (Mathf.Abs(p.x) + Mathf.Abs(p.y) - r) * .7071f;

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(ba.sqrMagnitude, 1e-6f));
            return (pa - ba * h).magnitude - r;
        }

        /// <summary>Distancia con signo a un polígono (exacta fuera y dentro; vértices en cualquier orden).</summary>
        private static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = (p - v[0]).sqrMagnitude, s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i]; var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(e.sqrMagnitude, 1e-6f));
                d = Mathf.Min(d, b.sqrMagnitude);
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
    }
}
