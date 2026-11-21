using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bosque.ArtV6;
using Bosque.Audio;
using Bosque.Character;
using Bosque.CharacterEditor;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B = Bosque.ArtV6.Editor.ArroyoArtV6Builder;
using Object = UnityEngine.Object;

namespace Bosque.LevelEditor
{
    /// <summary>
    /// Zona 4, "El valle olvidado": un valle andino grande (unos 210 × 210 m jugables) sobre un Terrain de Unity
    /// rodeado de montañas, con un río seco que cruza el noreste y un cerro al sureste. Cuatro sectores con 40 residuos
    /// (modelos CC0 de Poly Haven) y 16 troncos/ramas caídas, tres puntos de reciclaje con cuatro contenedores
    /// (norma peruana de colores), salientes de roca para saltar, rastreo (T), mapa (M), agua y fauna al restaurar.
    /// Idempotente: reescribe solo sus salidas (Art/Valle y Scenes/Zonas/ValleOlvidado).
    /// </summary>
    public static class ValleBuilder
    {
        public const string ScenePath = "Assets/_Bosque/Scenes/Zonas/ValleOlvidado.unity";
        public const string SceneName = "ValleOlvidado";
        public const string Root = "Assets/_Bosque/Art/Valle";
        private const float TerrainSize = 320f, TerrainHeight = 70f, Playable = 106f;
        private static System.Random rng;
        private static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // Red de agua inspirada en un mapa de ríos continental: un río principal de norte a sur (Mayu), un afluente
        // que llega del oeste (Chico), una cadena de tres lagunas al noreste alimentada por una vertiente y que desagua
        // al este (río de la Salida) y, al suroeste, un río que nace en un bofedal (río del Sur).
        private static readonly (string id, string label, Vector2 labelAt, Vector2[] pts, float depth, float flat, float bank)[] StreamDefs =
        {
            ("Mayu", "Río Mayu", new Vector2(10, 62), new[] { new Vector2(6, 140), new Vector2(8, 112), new Vector2(16, 92), new Vector2(28, 74), new Vector2(34, 54), new Vector2(30, 36),
                new Vector2(34, 18), new Vector2(30, 0), new Vector2(22, -18), new Vector2(20, -36), new Vector2(24, -56), new Vector2(16, -76), new Vector2(12, -96), new Vector2(10, -116), new Vector2(8, -140) }, 1.5f, 2.5f, 6.5f),
            ("Chico", "Río Chico", new Vector2(-60, 3), new[] { new Vector2(-140, 34), new Vector2(-112, 26), new Vector2(-90, 12), new Vector2(-70, -4), new Vector2(-48, -16), new Vector2(-26, -20),
                new Vector2(-6, -26), new Vector2(10, -25), new Vector2(21, -27) }, .6f, 1.5f, 4.5f),
            ("Vertiente", "", Vector2.zero, new[] { new Vector2(70, 140), new Vector2(66, 110), new Vector2(62, 94), new Vector2(58, 82) }, .45f, 1f, 3.5f),
            ("CanalAM", "", Vector2.zero, new[] { new Vector2(58, 82), new Vector2(80, 62) }, .55f, 1.5f, 4f),
            ("CanalMB", "", Vector2.zero, new[] { new Vector2(80, 62), new Vector2(90, 38) }, .55f, 1.5f, 4f),
            ("Salida", "Río de la Salida", new Vector2(98, 20), new[] { new Vector2(90, 38), new Vector2(104, 32), new Vector2(140, 26) }, .7f, 2f, 5f),
            ("Sur", "Río del Sur", new Vector2(-70, -94), new[] { new Vector2(-30, -40), new Vector2(-44, -60), new Vector2(-64, -74), new Vector2(-86, -84), new Vector2(-110, -100), new Vector2(-140, -118) }, .45f, 1f, 3.5f),
        };
        private static readonly (string label, Vector2 c, float r, float depth)[] LakeDefs =
        {
            ("Laguna Alta", new Vector2(58, 82), 10f, 2.4f), ("Laguna del Medio", new Vector2(80, 62), 9f, 2f), ("Laguna Baja", new Vector2(90, 38), 7.5f, 1.8f), ("Bofedal", new Vector2(-30, -40), 5f, .5f),
        };
        /// <summary>Vados del Mayu (agua por la rodilla) y puentes (sobre el agua honda).</summary>
        private static readonly Vector2[] Fords = { new Vector2(17, 90), new Vector2(31, -8), new Vector2(21, -45), new Vector2(13, -90) };
        private static readonly (string stream, Vector2 at)[] Bridges = { ("Mayu", new Vector2(32, 9)), ("Mayu", new Vector2(32, 47)), ("Chico", new Vector2(-16, -23)) };
        private static WaterStream[] wStreams = new WaterStream[0];
        private static WaterLake[] wLakes = new WaterLake[0];
        private static Vector2[] streamMin, streamMax;

        private static readonly Vector2 Hill = new Vector2(58, -58);
        private static readonly Vector2[] QuebradaAnchors =
        {
            new Vector2(16, 92), new Vector2(28, 74), new Vector2(34, 54), new Vector2(30, 36), new Vector2(58, 82), new Vector2(80, 62), new Vector2(90, 38), new Vector2(66, 104),
        };
        private static readonly (string name, Vector2 pos, float yaw)[] Stations =
        {
            ("Campamento", new Vector2(0, 8), 180f), ("Puesto de la quebrada", new Vector2(50, 60), 230f), ("Puesto de la pampa", new Vector2(-62, -32), 120f),
        };
        /// <summary>El zorro baja del bosque y termina bebiendo en la orilla del Mayu.</summary>
        private static readonly Vector2[] FoxRoute =
        {
            new Vector2(-48, 42), new Vector2(-30, 30), new Vector2(-12, 20), new Vector2(4, 18), new Vector2(14, 22), new Vector2(23, 25),
        };

        [MenuItem("Tools/Bosque/Zonas/Construir El valle olvidado (zona 4)")]
        public static void BuildAll()
        {
            var mats = ImportModels();
            var prefabs = ItemPrefabs(mats);
            var icons = ReadIcons();
            BuildScene(prefabs, mats);
            IntegrateRefugio();
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Valle] Zona construida e integrada.");
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try { BuildAll(); } catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static Texture2D[] ReadIcons()
        {
            var theme = AssetDatabase.LoadAssetAtPath<Bosque.UI.UiTheme>("Assets/_Bosque/UI/Resources/BosqueUiTheme.asset");
            return new[] { theme.interactionIcon, theme.progressIcon, theme.restorationIcon, theme.exitIcon };
        }

        // ------------------------------------------------------------------ modelos y materiales
        private static readonly string[] ModelNames =
        {
            "BidonPlastico", "BotellaLejia", "GaloneraPlastico", "BotellaLimpiador", "BotellaVidrio", "LataOxidada", "LataAceite", "BidonMetal",
            "Aerosol", "CajaCarton", "BolsaPapel", "BolsaBasura", "Llanta", "TroncoDelgado", "TroncoGrueso", "Ramas", "Contenedor",
        };

        private static Dictionary<string, Material> ImportModels()
        {
            Directory.CreateDirectory(Root + "/Materials");
            var mats = new Dictionary<string, Material>();
            foreach (var n in ModelNames)
            {
                foreach (var (suffix, normal) in new[] { ("BaseColor", false), ("Normal", true), ("MetalSmooth", false) })
                {
                    var path = $"{Root}/Textures/T_Valle_{n}_{suffix}" + (suffix == "MetalSmooth" ? ".png" : ".jpg");
                    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                    if (ti == null) continue;
                    bool changed = false;
                    var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    if (ti.textureType != type) { ti.textureType = type; changed = true; }
                    bool srgb = suffix == "BaseColor";
                    if (ti.sRGBTexture != srgb) { ti.sRGBTexture = srgb; changed = true; }
                    if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; changed = true; }
                    if (changed) ti.SaveAndReimport();
                }
                string mp = $"{Root}/Materials/M_Valle_{n}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Valle_" + n }; AssetDatabase.CreateAsset(m, mp); }
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/T_Valle_{n}_BaseColor.jpg"));
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/T_Valle_{n}_Normal.jpg")); m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/T_Valle_{n}_MetalSmooth.png")); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
                m.SetColor("_BaseColor", n == "BotellaVidrio" ? new Color(.55f, .78f, .5f) : Color.white);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                mats[n] = m;
                var mi = (ModelImporter)AssetImporter.GetAtPath($"{Root}/Models/SM_Valle_{n}.fbx");
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_Valle_" + n), m);
                mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.importCameras = mi.importLights = false;
                mi.SaveAndReimport();
            }
            foreach (var k in WasteKinds.All)
            {
                string mp = $"{Root}/Materials/M_Valle_Contenedor_{k}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(mats["Contenedor"]) { name = "M_Valle_Contenedor_" + k }; AssetDatabase.CreateAsset(m, mp); }
                else m.CopyPropertiesFromMaterial(mats["Contenedor"]);
                m.SetColor("_BaseColor", WasteKinds.BinColor(k) * 1.15f);
                EditorUtility.SetDirty(m); mats["Contenedor_" + k] = m;
            }
            return mats;
        }

        private static Dictionary<string, GameObject> ItemPrefabs(Dictionary<string, Material> mats)
        {
            Directory.CreateDirectory(Root + "/Prefabs");
            var dict = new Dictionary<string, GameObject>();
            foreach (var n in ModelNames)
                dict[n] = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/SM_Valle_{n}.fbx");
            // Baliza del rastreo: columna de luz ámbar.
            var beaconMatPath = Root + "/Materials/M_Valle_Baliza.mat";
            var bm = AssetDatabase.LoadAssetAtPath<Material>(beaconMatPath);
            if (bm == null) { bm = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(bm, beaconMatPath); }
            bm.SetFloat("_Surface", 1); bm.SetFloat("_Blend", 2); bm.SetFloat("_ZWrite", 0);
            bm.SetOverrideTag("RenderType", "Transparent"); bm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            bm.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); bm.SetFloat("_DstBlend", (float)BlendMode.One);
            bm.renderQueue = 3100; bm.SetColor("_BaseColor", new Color(1f, .78f, .35f, .55f)); EditorUtility.SetDirty(bm);
            var beacon = new GameObject("PF_Valle_Baliza");
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(col.GetComponent<Collider>());
            col.transform.SetParent(beacon.transform, false); col.transform.localPosition = new Vector3(0, 4.5f, 0); col.transform.localScale = new Vector3(.18f, 4.5f, .18f);
            var r = col.GetComponent<MeshRenderer>(); r.sharedMaterial = bm; r.shadowCastingMode = ShadowCastingMode.Off;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(beacon.transform, false); ring.transform.localPosition = new Vector3(0, .05f, 0); ring.transform.localScale = new Vector3(1.4f, .01f, 1.4f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = bm; ring.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            dict["Baliza"] = PrefabUtility.SaveAsPrefabAsset(beacon, Root + "/Prefabs/PF_Valle_Baliza.prefab");
            Object.DestroyImmediate(beacon);
            return dict;
        }

        // ------------------------------------------------------------------ terreno
        private static float Fbm(float x, float z, int oct)
        {
            float a = 1, f = 1, s = 0, n = 0;
            for (int i = 0; i < oct; i++) { s += (Mathf.PerlinNoise(x * f + 31.7f * i, z * f - 17.3f * i) * 2 - 1) * a; n += a; a *= .5f; f *= 2.03f; }
            return s / n;
        }

        /// <summary>Distancia horizontal a la orilla más cercana (negativa dentro de un cauce o laguna).</summary>
        public static float DistanceToWater(Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < wStreams.Length; i++)
            {
                var s = wStreams[i];
                if (p.x < streamMin[i].x - 40 || p.y < streamMin[i].y - 40 || p.x > streamMax[i].x + 40 || p.y > streamMax[i].y + 40) continue;
                ValleWater.Nearest(s, p, out float d, out _, out _, out _, out _);
                best = Mathf.Min(best, d - s.bank);
            }
            foreach (var l in wLakes) best = Mathf.Min(best, Vector2.Distance(p, new Vector2(l.center.x, l.center.z)) - (l.radius + 3f));
            return best;
        }

        // ------------------------------------------------------------------ agua: perfiles y modelo
        private static List<Vector2> Resample(Vector2[] pts, float step)
        {
            var o = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Length; i++)
            {
                float len = Vector2.Distance(pts[i - 1], pts[i]); int n = Mathf.Max(1, Mathf.RoundToInt(len / step));
                for (int k = 1; k <= n; k++) o.Add(Vector2.Lerp(pts[i - 1], pts[i], k / (float)n));
            }
            return o;
        }

        /// <summary>
        /// Calcula la superficie de cada río (siempre baja aguas abajo, sigue el terreno natural), el nivel de las lagunas,
        /// la profundidad por punto (vados más bajos) y deja el modelo listo para tallar el terreno.
        /// </summary>
        private static void BuildWaterModel()
        {
            var lakes = new List<WaterLake>();
            float prev = float.MaxValue;
            foreach (var (label, c, r, depth) in LakeDefs)
            {
                float level = Natural(c.x, c.y) - .5f;
                if (label != "Bofedal") { level = Mathf.Min(level, prev - .5f); prev = level; } // la cadena baja escalón a escalón
                lakes.Add(new WaterLake { name = label, center = new Vector3(c.x, level, c.y), radius = r, depth = depth });
            }
            WaterLake Lake(string n) => lakes.First(l => l.name == n);
            var streams = new List<WaterStream>();
            foreach (var def in StreamDefs)
            {
                var pts = Resample(def.pts, 2f);
                var s = new float[pts.Count]; var along = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++) { s[i] = Natural(pts[i].x, pts[i].y) - .6f; along[i] = i == 0 ? 0 : along[i - 1] + Vector2.Distance(pts[i], pts[i - 1]); }
                float total = along[pts.Count - 1];
                WaterLake from = null, to = null;
                if (def.id == "CanalAM") { from = Lake("Laguna Alta"); to = Lake("Laguna del Medio"); }
                if (def.id == "CanalMB") { from = Lake("Laguna del Medio"); to = Lake("Laguna Baja"); }
                if (def.id == "Salida") from = Lake("Laguna Baja");
                if (def.id == "Sur") from = Lake("Bofedal");
                if (def.id == "Vertiente") to = Lake("Laguna Alta");
                if (from != null) for (int i = 0; i < pts.Count && along[i] <= from.radius + 1f; i++) s[i] = from.center.y;
                // Siempre cuesta abajo: mínimo acumulado y suavizado (sigue siendo monótono).
                for (int i = 1; i < s.Length; i++) s[i] = Mathf.Min(s[i], s[i - 1]);
                for (int pass = 0; pass < 3; pass++)
                {
                    var c2 = (float[])s.Clone();
                    for (int i = 1; i < s.Length - 1; i++) s[i] = (c2[i - 1] + c2[i] + c2[i + 1]) / 3f;
                }
                if (to != null)
                    for (int i = 0; i < s.Length; i++)
                    {
                        float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(total - to.radius - 14f, total - to.radius, along[i]));
                        s[i] = Mathf.Lerp(Mathf.Max(s[i], to.center.y), to.center.y, k);
                    }
                if (def.id == "CanalAM" || def.id == "CanalMB")
                    for (int i = 0; i < s.Length; i++)
                        s[i] = Mathf.Lerp(from.center.y, to.center.y, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from.radius, total - to.radius, along[i])));
                var depth = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++)
                {
                    depth[i] = def.depth;
                    if (def.id == "Mayu")
                    {
                        float ford = Fords.Min(f => Vector2.Distance(f, pts[i]));
                        depth[i] = Mathf.Lerp(.45f, def.depth, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5f, 11f, ford)));
                    }
                }
                streams.Add(new WaterStream
                {
                    name = def.label, flat = def.flat, bank = def.bank, depth = depth,
                    points = pts.Select((p, i) => new Vector3(p.x, s[i], p.y)).ToArray(),
                    labelPosition = new Vector3(def.labelAt.x, 0, def.labelAt.y),
                });
            }
            // El afluente desemboca al nivel del Mayu.
            var mayu = streams[0]; var chico = streams[1];
            var junction = chico.points[chico.points.Length - 1];
            ValleWater.Nearest(mayu, new Vector2(junction.x, junction.z), out _, out float sj, out _, out _, out _);
            float chTotal = 0; var chAlong = new float[chico.points.Length];
            for (int i = 1; i < chico.points.Length; i++) chAlong[i] = chTotal += Vector3.Distance(Flat(chico.points[i]), Flat(chico.points[i - 1]));
            for (int i = 0; i < chico.points.Length; i++)
            {
                float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(chTotal - 16f, chTotal, chAlong[i]));
                chico.points[i].y = Mathf.Lerp(Mathf.Max(chico.points[i].y, sj), sj, k);
            }
            wStreams = streams.ToArray(); wLakes = lakes.ToArray();
            streamMin = wStreams.Select(st => new Vector2(st.points.Min(q => q.x), st.points.Min(q => q.z))).ToArray();
            streamMax = wStreams.Select(st => new Vector2(st.points.Max(q => q.x), st.points.Max(q => q.z))).ToArray();
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        /// <summary>
        /// Terreno final: el natural con los cauces y lagunas tallados. Dentro del cauce manda el perfil del lecho;
        /// fuera, la orilla baja suave hacia el agua y, si el terreno quedaba más bajo que el agua, se levanta un bordo.
        /// </summary>
        private static float Height(float x, float z)
        {
            float n = Natural(x, z);
            var p = new Vector2(x, z);
            float inner = float.MaxValue, carve = float.MaxValue, levee = float.MinValue;
            bool inside = false;
            for (int i = 0; i < wStreams.Length; i++)
            {
                var s = wStreams[i];
                if (p.x < streamMin[i].x - 40 || p.y < streamMin[i].y - 40 || p.x > streamMax[i].x + 40 || p.y > streamMax[i].y + 40) continue;
                ValleWater.Nearest(s, p, out float d, out float sy, out float dep, out _, out _);
                if (d < s.bank) { inside = true; inner = Mathf.Min(inner, sy + ValleWater.StreamBed(d, dep, s.flat, s.bank)); }
                else if (d < s.bank + 24f)
                {
                    carve = Mathf.Min(carve, Mathf.Lerp(sy + ValleWater.BankRise, n, Mathf.SmoothStep(0, 1, (d - s.bank) / Mathf.Clamp((n - sy) * 2.2f, 8f, 22f))));
                    levee = Mathf.Max(levee, sy + ValleWater.BankRise - .25f * (d - s.bank));
                }
            }
            foreach (var l in wLakes)
            {
                float r = Vector2.Distance(p, new Vector2(l.center.x, l.center.z)), reach = l.radius + 3f;
                if (r < reach) { inside = true; inner = Mathf.Min(inner, l.center.y + ValleWater.LakeBed(r, l.radius, l.depth)); }
                else if (r < reach + 24f)
                {
                    carve = Mathf.Min(carve, Mathf.Lerp(l.center.y + ValleWater.BankRise, n, Mathf.SmoothStep(0, 1, (r - reach) / Mathf.Clamp((n - l.center.y) * 2.2f, 8f, 22f))));
                    levee = Mathf.Max(levee, l.center.y + ValleWater.BankRise - .25f * (r - reach));
                }
            }
            if (inside) return Mathf.Max(.5f, inner);
            return Mathf.Max(.5f, Mathf.Max(Mathf.Min(n, carve), levee));
        }

        /// <summary>Terreno sin agua: relieve, pampa, cerro, campamento y puestos llanos, montañas del borde.</summary>
        private static float Natural(float x, float z)
        {
            var p = new Vector2(x, z);
            float h = 8f + Fbm(x / 60f, z / 60f, 4) * 3.2f + Fbm(x / 15f, z / 15f, 2) * .55f;
            // La pampa (suroeste) es más llana.
            float pampa = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10, -40, x)) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10, -40, z));
            h = Mathf.Lerp(h, 7.5f + Fbm(x / 80f, z / 80f, 2) * 1.2f, pampa * .8f);
            // Cerro del mirador (sureste).
            float dh = Vector2.Distance(p, Hill);
            h += 13f * Mathf.Exp(-dh * dh / (2 * 24f * 24f));
            // Campamento llano.
            float dc = p.magnitude;
            h = Mathf.Lerp(8.2f, h, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(12, 22, dc)));
            foreach (var st in Stations.Skip(1))
            {
                float ds = Vector2.Distance(p, st.pos);
                float flat = Height0(st.pos);
                h = Mathf.Lerp(flat, h, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 10, ds)));
            }
            // Montañas del borde: imposibles de escalar (pendiente > 45°).
            float e = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float m = Mathf.Pow(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Playable - 6, 150, e)), 1.5f);
            h += m * (52f + Fbm(x / 30f, z / 30f, 3) * 12f);
            return Mathf.Max(.5f, h);
        }

        private static float Height0(Vector2 p) => 8f + Fbm(p.x / 60f, p.y / 60f, 4) * 3.2f;

        private static TerrainLayer Layer(string name, string tex, float tile)
        {
            string path = $"{Root}/Terreno/TL_Valle_{name}.terrainlayer";
            Directory.CreateDirectory(Root + "/Terreno");
            var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (l == null) { l = new TerrainLayer(); AssetDatabase.CreateAsset(l, path); }
            l.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{B.Root}/Textures/Ground/{tex}_BaseMap.jpg");
            l.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{B.Root}/Textures/Ground/{tex}_Normal.jpg");
            l.maskMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{B.Root}/Textures/Ground/{tex}_Mask.png");
            l.maskMapRemapMin = Vector4.zero; l.maskMapRemapMax = new Vector4(0, 1, 1, 1); // sin metal
            l.tileSize = new Vector2(tile, tile); l.normalScale = 1f;
            EditorUtility.SetDirty(l);
            return l;
        }

        private static GameObject TreePrefab(string v6, bool green)
        {
            Directory.CreateDirectory(Root + "/Prefabs/Arboles");
            string path = $"{Root}/Prefabs/Arboles/PF_Valle_{v6.Replace("VIS_V6_", "")}.prefab";
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(v6));
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var cap = go.AddComponent<CapsuleCollider>(); cap.radius = .32f; cap.height = 4f; cap.center = new Vector3(0, 2f, 0);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static Material IchuMaterial()
        {
            string path = Root + "/Materials/M_Valle_Ichu.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(B.M("M_GrassA_Dry")) { name = "M_Valle_Ichu" }; AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(B.M("M_GrassA_Dry"));
            m.SetColor("_BaseColor", new Color(1.55f, 1.35f, .95f)); m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject DetailPrefab(string model, string mat, string name)
        {
            Directory.CreateDirectory(Root + "/Prefabs/Detalles");
            string path = $"{Root}/Prefabs/Detalles/{name}.prefab";
            var mesh = AssetDatabase.LoadAllAssetsAtPath($"{B.Root}/Models/{model}.fbx").OfType<Mesh>().First();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = mat == "Ichu" ? IchuMaterial() : B.M(mat); r.shadowCastingMode = ShadowCastingMode.Off;
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static Terrain BuildTerrain(Transform parent, List<Vector2> treeSpots)
        {
            Directory.CreateDirectory(Root + "/Terreno");
            BuildWaterModel();
            string dataPath = Root + "/Terreno/TD_ValleOlvidado.asset";
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if (td == null) { td = new TerrainData(); AssetDatabase.CreateAsset(td, dataPath); }
            td.heightmapResolution = 513;
            td.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            td.alphamapResolution = 512;
            td.SetDetailResolution(512, 32);
            td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            int hr = td.heightmapResolution;
            var hs = new float[hr, hr];
            for (int y = 0; y < hr; y++)
                for (int x = 0; x < hr; x++)
                {
                    float wx = x / (float)(hr - 1) * TerrainSize - TerrainSize / 2, wz = y / (float)(hr - 1) * TerrainSize - TerrainSize / 2;
                    hs[y, x] = Height(wx, wz) / TerrainHeight;
                }
            td.SetHeights(0, 0, hs);

            td.terrainLayers = new[] { Layer("SueloSeco", "T_DryGroundRocks", 11f), Layer("Pasto", "T_LeafyGrass", 7f), Layer("Roca", "T_RockyTerrain", 7f), Layer("Cauce", "T_DryRiverbed", 5f) };
            int aw = td.alphamapWidth;
            var al = new float[aw, aw, 4];
            for (int y = 0; y < aw; y++)
                for (int x = 0; x < aw; x++)
                {
                    float u = (x + .5f) / aw, v = (y + .5f) / aw;
                    float wx = u * TerrainSize - TerrainSize / 2, wz = v * TerrainSize - TerrainSize / 2;
                    float steep = td.GetSteepness(u, v);
                    float rock = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(26f, 38f, steep));
                    // Manchas pedregosas en lo llano: rompen la repetición del suelo seco.
                    rock = Mathf.Max(rock, Mathf.Clamp01((Fbm(wx / 21f + 7f, wz / 21f - 3f, 3) - .2f) * 1.3f) * .45f);
                    float river = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2.5f, -1f, DistanceToWater(new Vector2(wx, wz)))) * (1 - rock);
                    float grass = Mathf.Clamp01((Fbm(wx / 18f, wz / 18f, 2) - .35f) * 2f) * .35f * (1 - rock - river);
                    float dry = Mathf.Max(0, 1 - rock - river - grass);
                    al[y, x, 0] = dry; al[y, x, 1] = grass; al[y, x, 2] = rock; al[y, x, 3] = river;
                }
            td.SetAlphamaps(0, 0, al);

            // Árboles: pares (seco, verde) para que el restaurador sume 1 al índice.
            var protos = new List<TreePrototype>();
            foreach (var (dead, green) in new[] { ("VIS_V6_TreeA_Dead", "VIS_V6_TreeA_Green"), ("VIS_V6_TreeB_Dead", "VIS_V6_TreeB_Green"), ("VIS_V6_TreeB_Tall_Dead", "VIS_V6_TreeB_Tall_Green") })
            {
                protos.Add(new TreePrototype { prefab = TreePrefab(dead, false) });
                protos.Add(new TreePrototype { prefab = TreePrefab(green, true) });
            }
            td.treePrototypes = protos.ToArray();
            var trees = new List<TreeInstance>();
            bool Free(Vector2 p, float spacing)
            {
                if (p.magnitude < 20f || DistanceToWater(p) < 3f) return false;
                foreach (var b in Bridges) if (Vector2.Distance(p, b.at) < 12f) return false;
                foreach (var st in Stations) if (Vector2.Distance(p, st.pos) < 10f) return false;
                for (int i = 1; i < FoxRoute.Length; i++)
                {
                    var a = FoxRoute[i - 1]; var ab = FoxRoute[i] - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    if (Vector2.Distance(p, a + ab * t) < 3f) return false;
                }
                foreach (var q in treeSpots) if ((q - p).sqrMagnitude < spacing * spacing) return false;
                return true;
            }
            void Plant(int count, Func<Vector2> pick, float spacing)
            {
                for (int k = 0, placed = 0; k < count * 12 && placed < count; k++)
                {
                    var p = pick();
                    if (!Free(p, spacing)) continue;
                    float steep = td.GetSteepness((p.x + TerrainSize / 2) / TerrainSize, (p.y + TerrainSize / 2) / TerrainSize);
                    if (steep > 32f) continue;
                    treeSpots.Add(p); placed++;
                    int kind = rng.Next(3) * 2;
                    float s = R(1.4f, 2.1f);
                    trees.Add(new TreeInstance
                    {
                        position = new Vector3((p.x + TerrainSize / 2) / TerrainSize, 0, (p.y + TerrainSize / 2) / TerrainSize),
                        prototypeIndex = kind, widthScale = s, heightScale = s, rotation = R(0, Mathf.PI * 2),
                        color = Color.white, lightmapColor = Color.white,
                    });
                }
            }
            Plant(190, () => new Vector2(R(-104, -12), R(12, 104)), 4.6f);       // bosque (NO)
            Plant(55, () => { var a = QuebradaAnchors[rng.Next(QuebradaAnchors.Length)]; return a + new Vector2(R(-20, 20), R(-20, 20)); }, 6f); // quebrada (NE)
            Plant(55, () => new Vector2(R(16, 104), R(-104, -12)), 7f);           // mirador (SE)
            Plant(22, () => new Vector2(R(-104, -14), R(-104, -14)), 12f);        // pampa (SO), dispersos
            Plant(70, () => { float a = R(0, Mathf.PI * 2); float d = R(112, 140); return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d); }, 7f); // laderas
            td.SetTreeInstances(trees.ToArray(), true);

            // Pasto: seco (0) y verde (1).
            td.detailPrototypes = new[]
            {
                new DetailPrototype { prototype = DetailPrefab("SM_GrassA_MidA", "Ichu", "DET_Valle_PastoSeco"), usePrototypeMesh = true, useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit, minWidth = 2f, maxWidth = 3.2f, minHeight = 2f, maxHeight = 3.4f, noiseSpread = .4f },
                new DetailPrototype { prototype = DetailPrefab("SM_GrassA_MidB", "M_GrassA", "DET_Valle_PastoVerde"), usePrototypeMesh = true, useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit, minWidth = 2.2f, maxWidth = 3.4f, minHeight = 2.2f, maxHeight = 3.6f, noiseSpread = .4f },
            };
            int dw = td.detailWidth;
            var dryLayer = new int[dw, dw]; var greenLayer = new int[dw, dw];
            for (int y = 0; y < dw; y++)
                for (int x = 0; x < dw; x++)
                {
                    float u = (x + .5f) / dw, v = (y + .5f) / dw;
                    float wx = u * TerrainSize - TerrainSize / 2, wz = v * TerrainSize - TerrainSize / 2;
                    if (Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) > Playable + 8 || DistanceToWater(new Vector2(wx, wz)) < 1.5f || td.GetSteepness(u, v) > 28f) continue;
                    float n = Fbm(wx / 9f, wz / 9f, 2);
                    if (n > .05f && rng.NextDouble() < .3) dryLayer[y, x] = 1;
                }
            td.SetDetailLayer(0, 0, 0, dryLayer); td.SetDetailLayer(0, 0, 1, greenLayer);
            EditorUtility.SetDirty(td);
            AssetDatabase.SaveAssets();

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terreno"; go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-TerrainSize / 2, 0, -TerrainSize / 2);
            var t = go.GetComponent<Terrain>();
            string tmPath = Root + "/Terreno/M_Valle_Terreno.mat";
            var tm = AssetDatabase.LoadAssetAtPath<Material>(tmPath);
            if (tm == null) { tm = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(tm, tmPath); }
            t.materialTemplate = tm;
            t.treeDistance = 220f; t.treeBillboardDistance = 400f; t.detailObjectDistance = 70f; t.detailObjectDensity = 1f;
            t.heightmapPixelError = 4f; t.basemapDistance = 120f; t.drawInstanced = true;
            t.shadowCastingMode = ShadowCastingMode.TwoSided;
            go.AddComponent<TerrainRestorer>();
            return t;
        }

        // ------------------------------------------------------------------ escena
        private static Vector3 OnGround(Terrain t, Vector2 p, float lift = 0f) => new Vector3(p.x, t.SampleHeight(new Vector3(p.x, 0, p.y)) + t.transform.position.y + lift, p.y);

        private static GameObject Put(string prefab, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static Bounds BoundsOf(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        private static (string model, string label, WasteKind kind, int slots)[] Waste =
        {
            ("BidonPlastico", "bidón de plástico", WasteKind.Plastico, 1), ("BotellaLejia", "botella de lejía", WasteKind.Plastico, 1),
            ("GaloneraPlastico", "galonera de plástico", WasteKind.Plastico, 1), ("BotellaLimpiador", "botella de limpiador", WasteKind.Plastico, 1),
            ("BotellaVidrio", "botella de vidrio", WasteKind.VidrioMetal, 1), ("LataOxidada", "lata oxidada", WasteKind.VidrioMetal, 1),
            ("LataAceite", "lata de aceite", WasteKind.VidrioMetal, 1), ("BidonMetal", "bidón de metal", WasteKind.VidrioMetal, 1),
            ("Aerosol", "lata de aerosol", WasteKind.VidrioMetal, 1), ("CajaCarton", "caja de cartón", WasteKind.PapelCarton, 1),
            ("BolsaPapel", "bolsa de papel", WasteKind.PapelCarton, 1), ("BolsaBasura", "bolsa de basura", WasteKind.NoAprovechable, 1),
            ("Llanta", "llanta", WasteKind.NoAprovechable, 3),
        };

        private static void BuildScene(Dictionary<string, GameObject> prefabs, Dictionary<string, Material> mats)
        {
            rng = new System.Random(20261126);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("ValleOlvidado").transform;
            var degraded = new GameObject("DegradedVisuals").transform; degraded.SetParent(level, false);
            var restored = new GameObject("RestoredVisuals").transform; restored.SetParent(level, false);
            var markers = new GameObject("Markers").transform; markers.SetParent(level, false);
            var treeSpots = new List<Vector2>();
            var terrain = BuildTerrain(level, treeSpots);

            // Agua: modelo compartido con el juego, puentes y vados.
            var waterGo = new GameObject("V6_Agua"); waterGo.transform.SetParent(level, false);
            var water = waterGo.AddComponent<ValleWater>();
            var crossings = new List<WaterCrossing>();
            foreach (var f in Fords) crossings.Add(new WaterCrossing { name = "Vado", position = SnapTo(wStreams[0], f, out _), bridge = false });
            foreach (var (id, at) in Bridges)
                crossings.Add(new WaterCrossing { name = "Puente", position = SnapTo(wStreams[Array.FindIndex(StreamDefs, d => d.id == id)], at, out _), bridge = true });
            water.Configure(wStreams, wLakes, crossings.ToArray());
            var crossRoot = new GameObject("Cruces").transform; crossRoot.SetParent(waterGo.transform, false);
            foreach (var (id, at) in Bridges) BuildBridge(crossRoot, terrain, wStreams[Array.FindIndex(StreamDefs, d => d.id == id)], at);
            foreach (var f in Fords) BuildFord(crossRoot, terrain, wStreams[0], f);

            // Muros invisibles del área jugable (las montañas ya lo impiden; esto es seguridad).
            foreach (var (c, s) in new[] { (new Vector3(Playable + 4, 20, 0), new Vector3(2, 60, 240)), (new Vector3(-Playable - 4, 20, 0), new Vector3(2, 60, 240)),
                                            (new Vector3(0, 20, Playable + 4), new Vector3(240, 60, 2)), (new Vector3(0, 20, -Playable - 4), new Vector3(240, 60, 2)) })
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube); w.name = "MuroInvisible"; w.transform.SetParent(level, false);
                w.transform.position = c; w.transform.localScale = s; w.GetComponent<MeshRenderer>().enabled = false;
            }

            // Rocas grandes de ambientación (con collider aproximado) en el cerro y la quebrada.
            var rocks = new GameObject("V6_Rocas").transform; rocks.SetParent(level, false);
            for (int k = 0; k < 60; k++)
            {
                Vector2 p = k < 25 ? Hill + new Vector2(R(-30, 30), R(-30, 30)) : k < 45 ? QuebradaAnchors[rng.Next(QuebradaAnchors.Length)] + new Vector2(R(-16, 16), R(-16, 16)) : new Vector2(R(-100, 100), R(-100, 100));
                if (p.magnitude < 16 || Stations.Any(s => Vector2.Distance(s.pos, p) < 8) || DistanceToWater(p) < 1f || Bridges.Any(b => Vector2.Distance(b.at, p) < 10f)) continue;
                var r = Put("VIS_V6_RockMoss_" + "ABCDEFG"[rng.Next(7)], rocks, OnGround(terrain, p, -.1f), R(0, 360), R(.4f, 1.1f));
                var b = BoundsOf(r); var bc = r.AddComponent<BoxCollider>();
                bc.center = r.transform.InverseTransformPoint(b.center); bc.size = Vector3.Scale(b.size, new Vector3(1 / r.transform.lossyScale.x, 1 / r.transform.lossyScale.y, 1 / r.transform.lossyScale.z)) * .85f;
            }

            // Salientes del mirador: escalones de roca para alcanzar residuos en lo alto (saltos de 1 m).
            var ledges = new GameObject("Salientes").transform; ledges.SetParent(level, false);
            var ledgeTops = new List<Vector3>();
            foreach (var (basePos, dir) in new[] { (new Vector2(82, -34), new Vector2(.6f, .8f)), (new Vector2(36, -86), new Vector2(-.7f, -.7f)) })
            {
                float g = OnGround(terrain, basePos).y;
                for (int i = 0; i < 4; i++)
                {
                    var p = basePos + dir * (i * 2.4f);
                    float top = g + .9f * (i + 1);
                    var step = GameObject.CreatePrimitive(PrimitiveType.Cube); step.name = "Escalon"; step.transform.SetParent(ledges, false);
                    float bottom = OnGround(terrain, p).y - 1.5f;
                    step.transform.position = new Vector3(p.x, (top + bottom) / 2, p.y); step.transform.localScale = new Vector3(2.2f, top - bottom, 2.2f);
                    step.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 0);
                    step.GetComponent<MeshRenderer>().enabled = false;
                    var rockVis = Put("VIS_V6_Cliff_" + "ABCDEF"[(i + (int)basePos.x) % 6] + "_Degraded", step.transform.parent, Vector3.zero, 0, 1f);
                    var rb = BoundsOf(rockVis);
                    var want = step.transform.localScale;
                    rockVis.transform.localScale = new Vector3(want.x * 1.12f / rb.size.x, want.y / rb.size.y, want.z * 1.12f / rb.size.z);
                    rockVis.transform.rotation = step.transform.rotation;
                    rb = BoundsOf(rockVis);
                    rockVis.transform.position += new Vector3(step.transform.position.x - rb.center.x, top - rb.max.y, step.transform.position.z - rb.center.z);
                    if (i == 3) ledgeTops.Add(new Vector3(p.x, top, p.y));
                }
            }

            // Residuos y troncos por sector.
            string[] sectorNames = { "La quebrada", "El bosque", "La pampa", "El mirador" };
            var sectorItems = new List<LitterItem>[4]; var sectorWood = new List<FallenWood>[4];
            for (int i = 0; i < 4; i++) { sectorItems[i] = new List<LitterItem>(); sectorWood[i] = new List<FallenWood>(); }
            var itemsRoot = new GameObject("Residuos").transform; itemsRoot.SetParent(level, false);
            var used = new List<Vector2>();
            int litterId = 0, woodId = 0;
            bool Free(Vector2 p, float r) => !used.Any(u => (u - p).sqrMagnitude < r * r) && !treeSpots.Any(t => (t - p).sqrMagnitude < 2.2f * 2.2f) &&
                                             p.magnitude > 14 && Stations.All(s => Vector2.Distance(s.pos, p) > 7) && Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) < Playable - 8 &&
                                             Bridges.All(b => Vector2.Distance(b.at, p) > 9f);
            bool Clear(Vector2 p, float r) => Free(p, r) && DistanceToWater(p) > 1f;
            // Flotando en agua poco profunda (se alcanza vadeando).
            Vector3 Floating(Func<Vector2> f)
            {
                for (int k = 0; k < 600; k++)
                {
                    var p = f();
                    if (!Free(p, 2.5f) || !water.SurfaceAt(new Vector3(p.x, 0, p.y), out float sy)) continue;
                    float dep = sy - OnGround(terrain, p).y;
                    if (dep < .22f || dep > .5f) continue;
                    return new Vector3(p.x, sy - .08f, p.y);
                }
                throw new Exception("No hay agua poco profunda para un residuo flotante");
            }
            Vector2 Around(Vector2 c, float r0, float r1) { float a = R(0, Mathf.PI * 2), d = R(r0, r1); return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d; }
            float Across(Vector2 a, Vector2 b) { var d = (b - a).normalized; return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; } // el eje largo (X local) queda atravesado

            LitterItem Litter(Vector2 p, int sector, int wasteIndex, Vector3? exact = null)
            {
                var w = Waste[wasteIndex];
                var go = new GameObject($"Residuo_{++litterId:00}_{w.model}"); go.transform.SetParent(itemsRoot, false);
                var pos = exact ?? OnGround(terrain, p);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, R(0, 360), 0));
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[w.model], go.transform);
                vis.name = "Visual";
                bool lie = (w.model.StartsWith("Botella") || w.model.StartsWith("Lata") || w.model == "Aerosol" || w.model == "BidonPlastico") && rng.NextDouble() < .6;
                if (w.model == "Llanta") lie = true;
                if (lie)
                {
                    var b0 = BoundsOf(vis);
                    vis.transform.localRotation = Quaternion.Euler(w.model == "Llanta" ? 90 : 88, 0, R(-10, 10));
                    var b1 = BoundsOf(vis); vis.transform.position += Vector3.up * (pos.y - b1.min.y);
                }
                var b = BoundsOf(vis);
                var col = go.AddComponent<BoxCollider>();
                col.center = go.transform.InverseTransformPoint(b.center);
                var sz = Quaternion.Inverse(go.transform.rotation) * b.size;
                col.size = new Vector3(Mathf.Max(.4f, Mathf.Abs(sz.x)), Mathf.Max(.3f, Mathf.Abs(sz.y)), Mathf.Max(.4f, Mathf.Abs(sz.z)));
                var item = go.AddComponent<LitterItem>();
                item.Configure($"valle.residuo.{litterId:00}", w.label, w.kind, w.slots, sector, col);
                sectorItems[sector].Add(item); used.Add(new Vector2(pos.x, pos.z));
                return item;
            }

            FallenWood Wood(Vector2 p, int sector, string model, float yaw, float seconds)
            {
                var go = new GameObject($"Madera_{++woodId:00}_{model}"); go.transform.SetParent(itemsRoot, false);
                var pos = OnGround(terrain, p);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[model], go.transform); vis.name = "Visual";
                var b = BoundsOf(vis);
                vis.transform.position += Vector3.up * (pos.y - b.min.y - .08f);
                b = BoundsOf(vis);
                var col = go.AddComponent<BoxCollider>();
                col.center = go.transform.InverseTransformPoint(b.center);
                var sz = Quaternion.Inverse(go.transform.rotation) * b.size;
                col.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z)) * .92f;
                var wood = go.AddComponent<FallenWood>();
                string label = model == "TroncoGrueso" ? "el tronco grueso" : model == "TroncoDelgado" ? "el tronco" : "las ramas";
                wood.Configure($"valle.madera.{woodId:00}", label, model == "TroncoGrueso" ? 3.6f : model == "TroncoDelgado" ? 2.4f : 1.6f, sector, col, vis.transform);
                sectorWood[sector].Add(wood); used.Add(p);
                return wood;
            }

            Vector2 Pick(Func<Vector2> f, float r) { for (int k = 0; k < 200; k++) { var p = f(); if (Clear(p, r)) return p; } return f(); }
            int[] plastic = { 0, 1, 2, 3 }, glassMetal = { 4, 5, 6, 7, 8 }, paper = { 9, 10 }, other = { 11 };
            int RandomWaste(int[] a) => a[rng.Next(a.Length)];
            int Mix(int k) => k % 4 == 0 ? RandomWaste(plastic) : k % 4 == 1 ? RandomWaste(glassMetal) : k % 4 == 2 ? RandomWaste(paper) : RandomWaste(other);

            // 0 · La quebrada: residuos flotando en las lagunas y el Mayu, otros en las orillas; troncos que tapan los canales.
            var alta = new Vector2(58, 82); var medio = new Vector2(80, 62); var baja = new Vector2(90, 38);
            Litter(Vector2.zero, 0, Mix(0), Floating(() => Around(alta, 6f, 11f)));
            Litter(Vector2.zero, 0, Mix(1), Floating(() => Around(alta, 6f, 11f)));
            Litter(Vector2.zero, 0, 12, Floating(() => Around(baja, 4f, 9f)));
            Litter(Vector2.zero, 0, Mix(3), Floating(() => Around(medio, 5f, 10f)));
            Litter(Vector2.zero, 0, Mix(4), Floating(() => Vector2.Lerp(new Vector2(28, 74), new Vector2(34, 54), R(.1f, .9f)) + new Vector2(R(-6, 6), R(-6, 6))));
            for (int k = 5; k < 10; k++)
                Litter(Pick(() => { var p = Around(QuebradaAnchors[rng.Next(QuebradaAnchors.Length)], 8f, 18f); return new Vector2(Mathf.Max(p.x, 5f), Mathf.Max(p.y, 5f)); }, 3f), 0, Mix(k));
            Wood(Vector2.Lerp(alta, medio, .5f), 0, "TroncoGrueso", Across(alta, medio), 0);
            Wood(Vector2.Lerp(medio, baja, .5f), 0, "TroncoGrueso", Across(medio, baja), 0);
            Wood(new Vector2(17.6f, 89f), 0, "TroncoDelgado", Across(new Vector2(16, 92), new Vector2(28, 74)), 0);
            Wood(new Vector2(62.8f, 97f), 0, "Ramas", Across(new Vector2(66, 110), new Vector2(62, 94)), 0);
            // 1 · El bosque: residuos escondidos entre árboles, dos en la orilla del Chico; troncos en los claros y uno en el río.
            for (int k = 0; k < 8; k++) Litter(Pick(() => new Vector2(R(-98, -18), R(18, 98)), 3f), 1, Mix(k + 1));
            for (int k = 0; k < 2; k++) Litter(Pick(() => Vector2.Lerp(new Vector2(-98, 17), new Vector2(-80, 4), R(0, 1)) + new Vector2(R(-2, 2), R(6, 10)), 3f), 1, Mix(k + 2));
            for (int k = 0; k < 3; k++) Wood(Pick(() => new Vector2(R(-90, -20), R(20, 90)), 6f), 1, k < 1 ? "TroncoGrueso" : "TroncoDelgado", R(0, 180), 0);
            Wood(new Vector2(-101, 19), 1, "TroncoGrueso", Across(new Vector2(-112, 26), new Vector2(-90, 12)), 0);
            // 2 · La pampa: un basural (llantas, bolsas, latas), un residuo en el bofedal y otros sueltos; un tronco en el río del Sur.
            var dump = new Vector2(-72, -52);
            for (int k = 0; k < 6; k++) Litter(Pick(() => dump + new Vector2(R(-6, 6), R(-6, 6)), 1.4f), 2, k < 2 ? 12 : k < 4 ? 11 : RandomWaste(glassMetal));
            Litter(Vector2.zero, 2, Mix(2), Floating(() => Around(new Vector2(-30, -40), 1f, 6f)));
            Litter(Pick(() => Vector2.Lerp(new Vector2(-64, -74), new Vector2(-86, -84), R(0, 1)) + new Vector2(R(-3, 3), R(5, 9)), 3f), 2, Mix(3));
            for (int k = 0; k < 2; k++) Litter(Pick(() => new Vector2(R(-100, -20), R(-100, -20)), 14f), 2, Mix(k + 4));
            Wood(new Vector2(-54, -67), 2, "TroncoDelgado", Across(new Vector2(-44, -60), new Vector2(-64, -74)), 0);
            for (int k = 0; k < 3; k++) Wood(Pick(() => new Vector2(R(-95, -20), R(-95, -20)), 8f), 2, "Ramas", R(0, 180), 0);
            // 3 · El mirador: en el cerro y dos en lo alto de las salientes (hay que saltar).
            foreach (var top in ledgeTops) Litter(Vector2.zero, 3, RandomWaste(glassMetal), top);
            for (int k = 0; k < 8; k++) Litter(Pick(() => Hill + new Vector2(R(-30, 30), R(-30, 30)), 4f), 3, Mix(k + 3));
            for (int k = 0; k < 4; k++) Wood(Pick(() => Hill + new Vector2(R(-36, 36), R(-36, 36)), 7f), 3, k == 0 ? "TroncoGrueso" : k == 1 ? "TroncoDelgado" : "Ramas", R(0, 180), 0);

            // Sectores con decoración que aparece al limpiarlos (flores y mariposas).
            var sectors = new ValleSector[4];
            Vector2[] centers = { new Vector2(50, 50), new Vector2(-50, 50), new Vector2(-50, -50), new Vector2(50, -50) };
            for (int i = 0; i < 4; i++)
            {
                var sgo = new GameObject("Sector_" + sectorNames[i]); sgo.transform.SetParent(level, false);
                var decor = new GameObject("Recuperado").transform; decor.SetParent(sgo.transform, false);
                for (int k = 0; k < 26; k++)
                {
                    var p = centers[i] + new Vector2(R(-42, 42), R(-42, 42));
                    if (DistanceToWater(p) < 2f || p.magnitude < 14) continue;
                    var name = new[] { "VIS_V6_FlowerYellow_A", "VIS_V6_FlowerYellow_C", "VIS_V6_Dandelion_A", "VIS_V6_Fern_A", "VIS_V6_Fern_C", "VIS_V6_Shrub_A", "VIS_V6_Shrub_C" }[rng.Next(7)];
                    var g = Put(name, decor, OnGround(terrain, p), R(0, 360), name.Contains("Shrub") ? R(3f, 4f) : R(1.6f, 2.3f));
                    foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                }
                Fx("PFX_V6_Butterflies", decor, OnGround(terrain, centers[i], 1f));
                var sector = sgo.AddComponent<ValleSector>();
                sector.Configure(i, sectorNames[i], sectorItems[i].ToArray(), sectorWood[i].ToArray(), decor.gameObject);
                sectors[i] = sector;
            }

            // Puntos de reciclaje: cuatro contenedores con su color y rótulo.
            var bins = new List<RecycleBin>();
            var stRoot = new GameObject("PuntosReciclaje").transform; stRoot.SetParent(level, false);
            foreach (var (name, pos, yaw) in Stations)
            {
                var st = new GameObject(name).transform; st.SetParent(stRoot, false);
                var basePos = OnGround(terrain, pos);
                st.SetPositionAndRotation(basePos, Quaternion.Euler(0, yaw, 0));
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(pad.GetComponent<Collider>());
                pad.name = "V6_Explanada"; pad.transform.SetParent(st, false); pad.transform.localPosition = new Vector3(0, .02f, 0); pad.transform.localScale = new Vector3(7f, .02f, 5f);
                pad.GetComponent<MeshRenderer>().sharedMaterial = B.M("M_Ground_Path_Restored");
                for (int i = 0; i < 4; i++)
                {
                    var k = WasteKinds.All[i];
                    var bgo = new GameObject("Contenedor_" + k); bgo.transform.SetParent(st, false);
                    bgo.transform.localPosition = new Vector3((i - 1.5f) * 1.25f, 0, 0);
                    var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Contenedor"], bgo.transform); vis.name = "Visual";
                    vis.transform.localScale = Vector3.one * 1.25f;
                    foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats["Contenedor_" + k];
                    var col = bgo.AddComponent<BoxCollider>(); col.center = new Vector3(0, .57f, 0); col.size = new Vector3(1f, 1.15f, .8f);
                    var label = new GameObject("Rotulo"); label.transform.SetParent(bgo.transform, false);
                    label.transform.localPosition = new Vector3(0, 1.45f, -.05f); label.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    var tmp = label.AddComponent<TextMeshPro>(); tmp.text = k == WasteKind.NoAprovechable ? "NO\nAPROVECHABLE" : WasteKinds.Name(k).ToUpperInvariant().Replace(" Y ", "\nY ");
                    tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Bosque/UI/Fonts/TMP_AlegreyaSans_ExtraBold.asset");
                    tmp.enableAutoSizing = true; tmp.fontSizeMin = .9f; tmp.fontSizeMax = 2.2f; tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
                    tmp.outlineWidth = .25f; tmp.outlineColor = Color.black;
                    tmp.rectTransform.sizeDelta = new Vector2(1.15f, .62f);
                    var bin = bgo.AddComponent<RecycleBin>(); bin.Configure(k, col, name);
                    bins.Add(bin);
                }
                var sign = new GameObject("Cartel"); sign.transform.SetParent(st, false);
                sign.transform.localPosition = new Vector3(0, 2.4f, .3f); sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var st3 = sign.AddComponent<TextMeshPro>(); st3.text = "PUNTO DE RECICLAJE\n<size=70%>" + name + "</size>";
                st3.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Bosque/UI/Fonts/TMP_AlegreyaSans_ExtraBold.asset");
                st3.fontSize = 3f; st3.alignment = TextAlignmentOptions.Center; st3.color = new Color(.96f, .94f, .87f); st3.outlineWidth = .25f; st3.outlineColor = Color.black;
                st3.rectTransform.sizeDelta = new Vector2(6f, 1.4f);
                foreach (var x in new[] { -2.6f, 2.6f })
                {
                    var post = Put("VIS_V6_DeadLog_A", st, Vector3.zero, 0, 1f);
                    post.transform.localPosition = new Vector3(x, 0, .3f); post.transform.localRotation = Quaternion.Euler(0, 0, 90); post.transform.localScale = new Vector3(.9f, .25f, .25f);
                }
            }

            // Agua del río (solo restaurado) y polvo/hojas secas (solo degradado).
            var waterMeshes = WaterMeshes(terrain, waterGo.transform);
            Fx("PFX_V6_DustGusts", degraded, OnGround(terrain, new Vector2(-50, -50), .2f)); Fx("PFX_V6_DustMotes", degraded, OnGround(terrain, Vector2.zero, 1.3f));
            Fx("PFX_V6_FallingLeaves_Dry", degraded, OnGround(terrain, new Vector2(-50, 50), 7f));
            Fx("PFX_V6_Pollen", restored, OnGround(terrain, Vector2.zero, 1.6f)); Fx("PFX_V6_FallingLeaves", restored, OnGround(terrain, new Vector2(-50, 50), 7f));
            restored.gameObject.SetActive(false);

            // Fauna: el zorro baja del bosque al río cuando el valle revive.
            var faunaRoot = new GameObject("Fauna").transform; faunaRoot.SetParent(level, false);
            var wps = FoxRoute.Select((p, i) => { var tr = new GameObject($"Paso_{i:00}").transform; tr.SetParent(faunaRoot, false); tr.position = OnGround(terrain, p); return tr; }).ToArray();
            var fox = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FaunaBuilder.PrefabPath), faunaRoot); fox.name = "ZorroNeblina";
            var route = faunaRoot.gameObject.AddComponent<FaunaRoute>(); route.Configure(fox, fox.GetComponentInChildren<Animator>(), wps, FaunaBuilder.WalkClipSpeed);
            var vantage = new GameObject("VistaFauna").transform; vantage.SetParent(markers, false); vantage.position = OnGround(terrain, new Vector2(2, 4), 4.5f);

            // Jugador.
            var spawn = new GameObject("PlayerSpawn").transform; spawn.SetParent(markers, false); spawn.SetPositionAndRotation(OnGround(terrain, new Vector2(0, -6)), Quaternion.identity);
            var safe = new GameObject("SafeRespawn").transform; safe.SetParent(markers, false); safe.position = spawn.position;
            var gameplay = new GameObject("Gameplay").transform;
            var player = new GameObject("Player"); player.transform.SetParent(gameplay, false); player.transform.position = spawn.position;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.center = Vector3.up * .9f; cc.radius = .35f; cc.stepOffset = .4f; cc.skinWidth = .03f; cc.slopeLimit = 45f;
            var motor = player.AddComponent<ThirdPersonMotor>(); motor.SetRespawn(safe);
            var interactor = player.AddComponent<PlayerInteractor>();
            player.AddComponent<LitterBag>().Configure(8);
            player.AddComponent<TrackingPulse>().Configure(prefabs["Baliza"], null);
            var ripples = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/PFX_V6_WaterRipples.prefab"), gameplay);
            ripples.name = "OndasVadeo";
            player.AddComponent<WaterWading>().Configure(water, ripples);
            var guardiana = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GuardianaBuilder.PrefabPath), player.transform);
            guardiana.name = "Guardiana"; guardiana.transform.localPosition = new Vector3(0, -cc.skinWidth, 0);
            var camGo = new GameObject("GameplayCamera"); camGo.transform.SetParent(gameplay, false); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = .05f; cam.farClipPlane = 420f; cam.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
            var rig = camGo.AddComponent<ThirdPersonCamera>();
            var cso = new SerializedObject(rig); cso.FindProperty("focusHeight").floatValue = 1.45f; cso.FindProperty("distance").floatValue = 4.6f; cso.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms(); rig.Configure(player.transform);
            player.AddComponent<ThirdPersonInput>().Configure(rig);

            var sessionGo = new GameObject("ValleSession"); sessionGo.transform.SetParent(gameplay, false);
            var cinematic = sessionGo.AddComponent<FaunaCinematic>(); cinematic.Configure(route, rig, vantage, player.transform);
            var session = sessionGo.AddComponent<ValleSession>();
            session.Configure(sectors, bins.ToArray(), terrain.GetComponent<TerrainRestorer>(), degraded.gameObject, restored.gameObject, route, cinematic, motor, spawn);
            waterGo.AddComponent<ValleWaterLook>().Configure(session, waterMeshes.Select(m => m.renderer).ToArray(), waterMeshes.Select(m => m.sector).ToArray(),
                waterMeshes.Select(m => m.flow).ToArray(), waterMeshes.Select(m => m.still).ToArray());

            // Luz y ambiente V6 con niebla más ligera (el valle es grande).
            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft; sunGo.transform.rotation = Quaternion.Euler(38f, 310f, 0f);
            var amb = new GameObject("V6_Ambience").transform; amb.SetParent(level, false);
            Volume Vol(string name, string profile, float w)
            {
                var go = new GameObject(name); go.transform.SetParent(amb, false);
                var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 10; v.weight = w;
                v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{B.Root}/PostFX/{profile}.asset"); return v;
            }
            var ambience = amb.gameObject.AddComponent<BosqueV6Ambience>();
            ambience.restoredVisuals = restored.gameObject; ambience.sun = sun; ambience.skyBlend = B.M("M_Sky_Blend");
            ambience.degradedVolume = Vol("V6_PostFX_Degraded", "VP_V6_Degraded", 1); ambience.restoredVolume = Vol("V6_PostFX_Restored", "VP_V6_Restored", 0);
            ambience.restorationBurst = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/PFX_V6_RestorationWave.prefab");
            ambience.burstPosition = OnGround(terrain, Vector2.zero);
            ambience.degradedFogDensity = .0085f; ambience.restoredFogDensity = .0055f;
            ambience.ApplyImmediate(false);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;

            // Mapa ilustrado del valle (tecla M).
            amb.gameObject.AddComponent<Bosque.Levels.ValleAmbienceLink>().Configure(ambience, session);
            var mapTex = RenderMap(terrain, treeSpots, water);
            sessionGo.AddComponent<ValleHud>().Configure(session, interactor, mapTex, new Rect(-Playable - 4, -Playable - 4, 2 * Playable + 8, 2 * Playable + 8), water);

            // Audio.
            var audio = new GameObject("Audio_Valle");
            var sfx = audio.AddComponent<SfxPlayer>(); sfx.Configure(AssetDatabase.LoadAssetAtPath<SfxLibrary>(GuardianaBuilder.SfxLibraryPath));
            var ambGo = new GameObject("Ambiente"); ambGo.transform.SetParent(audio.transform, false);
            var ac = ambGo.AddComponent<AmbienceController>();
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Bosque/Audio/Ambience/{n}.ogg");
            var riverAnchor = new GameObject("Rio").transform; riverAnchor.SetParent(ambGo.transform, false); riverAnchor.position = wStreams[0].points[40];
            riverAnchor.gameObject.AddComponent<WaterSoundAnchor>().Configure(water);
            ac.loops = new List<AmbienceController.LoopLayer>
            {
                new AmbienceController.LoopLayer { name = "Viento seco", clip = A("AMB_Viento_Seco"), volume = .55f, weightDegradedRestored = new Vector2(1f, .2f) },
                new AmbienceController.LoopLayer { name = "Brisa en hojas", clip = A("AMB_Brisa_Hojas"), volume = .5f, weightDegradedRestored = new Vector2(.15f, 1f) },
                new AmbienceController.LoopLayer { name = "Aves", clip = A("AMB_Aves"), volume = .5f, weightDegradedRestored = new Vector2(.05f, 1f) },
                new AmbienceController.LoopLayer { name = "Río", clip = A("AMB_Rio"), volume = .55f, weightDegradedRestored = new Vector2(.45f, 1f), anchor = riverAnchor, spatial = true, minDistance = 8f, maxDistance = 70f },
            };
            ac.oneShots = new List<AmbienceController.RandomOneShots>
            {
                new AmbienceController.RandomOneShots { name = "Ramas secas", clips = new[] { A("AMB_Rama_Seca_01"), A("AMB_Rama_Seca_02"), A("AMB_Rama_Seca_03") }, volume = .45f, weightDegradedRestored = new Vector2(1f, 0f), intervalSeconds = new Vector2(6f, 14f) },
                new AmbienceController.RandomOneShots { name = "Aves cercanas", clips = new[] { A("AMB_Ave_01"), A("AMB_Ave_03"), A("AMB_Ave_04"), A("AMB_Ave_05"), A("AMB_Ave_06") }, volume = .55f, weightDegradedRestored = new Vector2(.1f, 1f), intervalSeconds = new Vector2(3f, 8f) },
            };
            var musGo = new GameObject("Musica"); musGo.transform.SetParent(audio.transform, false);
            musGo.AddComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Bosque/Audio/Music/MUS_Bosque_Hermoso.ogg");
            var music = musGo.AddComponent<MusicPlayer>();
            audio.AddComponent<ValleAudioBridge>().Configure(session, sfx, ac, music, guardiana.GetComponent<CharacterAnimationDriver>());

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("No se pudo guardar " + ScenePath);
            int lit = sectorItems.Sum(s => s.Count), wd = sectorWood.Sum(s => s.Count);
            Debug.Log($"[Valle] Escena guardada: {lit} residuos, {wd} maderas, {bins.Count} contenedores, {treeSpots.Count} árboles, " +
                      $"por sector: {string.Join(", ", Enumerable.Range(0, 4).Select(i => sectorNames[i] + " " + (sectorItems[i].Count + sectorWood[i].Count)))}.");
        }

        // ------------------------------------------------------------------ agua: cruces y malla
        /// <summary>Punto del eje de un río más cercano a <paramref name="p"/> (Y = superficie) y dirección de la corriente.</summary>
        private static Vector3 SnapTo(WaterStream s, Vector2 p, out Vector2 flow)
        {
            float best = float.MaxValue; var result = Vector3.zero; flow = Vector2.up;
            for (int i = 1; i < s.points.Length; i++)
            {
                var a = s.points[i - 1]; var b = s.points[i];
                var a2 = new Vector2(a.x, a.z); var ab = new Vector2(b.x, b.z) - a2;
                float t = Mathf.Clamp01(Vector2.Dot(p - a2, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                var q = Vector3.Lerp(a, b, t); float d = (new Vector2(q.x, q.z) - p).sqrMagnitude;
                if (d < best) { best = d; result = q; flow = ab.normalized; }
            }
            return result;
        }

        /// <summary>Tronco V6 escalado entre dos puntos locales de <paramref name="parent"/>.</summary>
        private static GameObject Log(Transform parent, Vector3 a, Vector3 b, float diameter)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab("VIS_V6_DeadLog_A"));
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); go.transform.localScale = Vector3.one;
            var bb = BoundsOf(go); var size = bb.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            var axisVec = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            float len = (b - a).magnitude;
            var sc = new Vector3(diameter / size.x, diameter / size.y, diameter / size.z); sc[axis] = len / size[axis];
            var rot = Quaternion.FromToRotation(axisVec, (b - a) / len) * Quaternion.AngleAxis(R(0, 360), axisVec);
            go.transform.SetParent(parent, false);
            go.transform.localScale = sc; go.transform.localRotation = rot;
            go.transform.localPosition = (a + b) * .5f - rot * Vector3.Scale(bb.center, sc);
            return go;
        }

        /// <summary>
        /// Puente de troncos sobre el agua honda: tablero de troncos atravesados sobre dos vigas, postes y barandas.
        /// Colisión: una caja para el tablero y dos barandas invisibles (no se cae al agua).
        /// </summary>
        private static void BuildBridge(Transform parent, Terrain terrain, WaterStream s, Vector2 at)
        {
            var c = SnapTo(s, at, out var flow); var across = new Vector2(-flow.y, flow.x);
            float half = s.bank + 3f; var c2 = new Vector2(c.x, c.z);
            float gA = OnGround(terrain, c2 - across * half).y, gB = OnGround(terrain, c2 + across * half).y;
            float deck = Mathf.Max(c.y + .75f, Mathf.Min(gA, gB) + .12f);
            var root = new GameObject("Puente_" + (s.name ?? "")).transform; root.SetParent(parent, false);
            root.SetPositionAndRotation(new Vector3(c.x, deck, c.z), Quaternion.LookRotation(new Vector3(across.x, 0, across.y)));
            var col = root.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, -.15f, 0); col.size = new Vector3(2.6f, .3f, half * 2f);
            foreach (var side in new[] { -1.4f, 1.4f })
            {
                var rail = new GameObject("Baranda").transform; rail.SetParent(root, false);
                var rc = rail.gameObject.AddComponent<BoxCollider>(); rc.center = new Vector3(side, .6f, 0); rc.size = new Vector3(.15f, 1.2f, half * 2f - 4f);
            }
            for (float z = -half + .2f; z <= half - .2f; z += .34f)
                Log(root, new Vector3(-1.35f, -.15f, z), new Vector3(1.35f, -.15f, z), .3f);
            foreach (var x in new[] { -.95f, .95f }) Log(root, new Vector3(x, -.45f, -half), new Vector3(x, -.45f, half), .38f);
            float span = half * 2f - 4f;
            int posts = Mathf.Max(2, Mathf.RoundToInt(span / 3f) + 1);
            foreach (var x in new[] { -1.4f, 1.4f })
            {
                for (int i = 0; i < posts; i++)
                {
                    float z = -span * .5f + span * i / (posts - 1);
                    Log(root, new Vector3(x, -.6f, z), new Vector3(x, 1.05f, z), .16f);
                    if (i > 0) Log(root, new Vector3(x, .95f, z - span / (posts - 1)), new Vector3(x, .95f, z), .12f);
                }
            }
        }

        /// <summary>Vado: piedras planas que asoman en el agua baja, de orilla a orilla (solo visuales).</summary>
        private static void BuildFord(Transform parent, Terrain terrain, WaterStream s, Vector2 at)
        {
            var c = SnapTo(s, at, out var flow); var across = new Vector2(-flow.y, flow.x); var c2 = new Vector2(c.x, c.z);
            var root = new GameObject("Vado").transform; root.SetParent(parent, false); root.position = c;
            for (int i = 0; i < 7; i++)
            {
                float t = Mathf.Lerp(-4.6f, 4.6f, i / 6f) + R(-.25f, .25f);
                var p = c2 + across * t + flow * R(-.7f, .7f);
                var g = OnGround(terrain, p);
                var r = Put("VIS_V6_RockMoss_" + "ABCDEFG"[rng.Next(7)], root, g, R(0, 360), R(.45f, .65f));
                var b = BoundsOf(r);
                float want = Mathf.Max(b.size.y, c.y + .07f - g.y + .25f);
                var ls = r.transform.localScale; ls.y *= want / b.size.y; r.transform.localScale = ls;
                b = BoundsOf(r); r.transform.position += Vector3.up * (c.y + .07f - b.max.y);
                foreach (var rr in r.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Superficie, trozo, corriente y tipo (laguna o río) del agua en un punto.</summary>
        private static bool WaterAt(Vector2 p, out float surface, out int key, out Vector2 flow, out bool still)
        {
            surface = 0; key = -1; flow = Vector2.up; still = false; float best = float.MaxValue;
            for (int i = 0; i < wLakes.Length; i++)
            {
                var l = wLakes[i]; float r = Vector2.Distance(p, new Vector2(l.center.x, l.center.z)), reach = l.radius + 3f;
                if (r < reach && r / reach < best) { best = r / reach; surface = l.center.y; key = 100000 + i; flow = new Vector2(.3f, 1f).normalized; still = true; }
            }
            for (int i = 0; i < wStreams.Length; i++)
            {
                var s = wStreams[i];
                if (p.x < streamMin[i].x - 10 || p.y < streamMin[i].y - 10 || p.x > streamMax[i].x + 10 || p.y > streamMax[i].y + 10) continue;
                ValleWater.Nearest(s, p, out float d, out float sy, out _, out var f, out int seg);
                if (d < s.bank && d / s.bank < best) { best = d / s.bank; surface = sy; key = i * 1000 + seg / 8; flow = f; still = false; }
            }
            return key >= 0;
        }

        /// <summary>
        /// Malla del agua: rejilla de 1 m donde la superficie queda por encima del terreno, en trozos (cada uno con su
        /// sector y su corriente) para que el agua se aclare por sectores y fluya en la dirección del cauce.
        /// </summary>
        private static List<(Renderer renderer, int sector, Vector2 flow, bool still)> WaterMeshes(Terrain terrain, Transform parent)
        {
            const float ext = 152f; const int n = 304; const float step = 2 * ext / n;
            float H(float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
            var vs = new float[n + 1, n + 1]; var has = new bool[n + 1, n + 1];
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                    has[j, i] = WaterAt(new Vector2(-ext + i * step, -ext + j * step), out vs[j, i], out _, out _, out _);
            var chunks = new Dictionary<int, (List<Vector3> v, List<int> t, Dictionary<int, int> map, Vector2 flow, bool still, Vector2 sum, int cells)>();
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = -ext + (i + .5f) * step, z = -ext + (j + .5f) * step;
                    if (!WaterAt(new Vector2(x, z), out float sy, out int key, out var flow, out bool still)) continue;
                    float lowest = Mathf.Min(Mathf.Min(H(x - step * .5f, z - step * .5f), H(x + step * .5f, z - step * .5f)), Mathf.Min(H(x - step * .5f, z + step * .5f), H(x + step * .5f, z + step * .5f)));
                    if (lowest > sy + .02f) continue;
                    if (!chunks.TryGetValue(key, out var ch)) ch = (new List<Vector3>(), new List<int>(), new Dictionary<int, int>(), flow, still, Vector2.zero, 0);
                    int V(int vi, int vj)
                    {
                        int id = vj * (n + 1) + vi;
                        if (ch.map.TryGetValue(id, out int idx)) return idx;
                        float y = has[vj, vi] ? vs[vj, vi] : sy;
                        ch.v.Add(new Vector3(-ext + vi * step, y, -ext + vj * step)); ch.map[id] = ch.v.Count - 1; return ch.v.Count - 1;
                    }
                    int a = V(i, j), b = V(i + 1, j), c = V(i, j + 1), d = V(i + 1, j + 1);
                    ch.t.AddRange(new[] { a, c, b, b, c, d });
                    ch.sum += new Vector2(x, z); ch.cells++;
                    chunks[key] = ch;
                }
            string path = Root + "/Terreno/SM_Valle_Agua.asset";
            AssetDatabase.DeleteAsset(path); AssetDatabase.DeleteAsset(Root + "/Terreno/SM_Valle_Rio.asset");
            var result = new List<(Renderer, int, Vector2, bool)>();
            bool first = true; int k = 0;
            foreach (var kv in chunks.OrderBy(x => x.Key))
            {
                var ch = kv.Value;
                var mesh = new Mesh { name = $"Agua_{k++:00}", indexFormat = ch.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(ch.v); mesh.SetUVs(0, ch.v.Select(p => new Vector2(p.x, p.z) / 6.8f).ToList()); mesh.SetTriangles(ch.t, 0);
                mesh.SetNormals(Enumerable.Repeat(Vector3.up, ch.v.Count).ToList()); mesh.RecalculateBounds();
                if (first) { AssetDatabase.CreateAsset(mesh, path); first = false; } else AssetDatabase.AddObjectToAsset(mesh, path);
                var go = new GameObject("V6_Agua_" + mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = B.M("M_Water_Stream"); r.shadowCastingMode = ShadowCastingMode.Off;
                var centroid = ch.sum / ch.cells;
                result.Add((r, TerrainRestorer.SectorOf(new Vector3(centroid.x, 0, centroid.y), Vector3.zero), ch.flow, ch.still));
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Valle] Agua: {result.Count} trozos, {chunks.Values.Sum(c => c.cells)} celdas de 1 m.");
            return result;
        }

        /// <summary>
        /// Mapa ilustrado a partir de los datos del terreno: relieve sombreado, suelo seco, roca, cauce, árboles y
        /// montañas. Más legible que una foto cenital (el suelo visto desde arriba es plano y repetitivo).
        /// </summary>
        private static Texture2D RenderMap(Terrain terrain, List<Vector2> trees, ValleWater waterModel)
        {
            const int N = 1024;
            float half = Playable + 4;
            var td = terrain.terrainData;
            var px = new Color[N * N];
            Color dry = new Color(.78f, .66f, .46f), rock = new Color(.56f, .53f, .5f), river = new Color(.45f, .66f, .8f), deepWater = new Color(.2f, .38f, .58f), mountain = new Color(.42f, .4f, .38f);
            var light = new Vector3(-.5f, .8f, .35f).normalized;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float wx = -half + (x + .5f) / N * 2 * half, wz = -half + (y + .5f) / N * 2 * half;
                    float u = (wx + TerrainSize / 2) / TerrainSize, v = (wz + TerrainSize / 2) / TerrainSize;
                    var n = td.GetInterpolatedNormal(u, v);
                    float shade = Mathf.Clamp01(Vector3.Dot(n, light)) * .55f + .55f;
                    float steep = td.GetSteepness(u, v), h = td.GetInterpolatedHeight(u, v);
                    var c = Color.Lerp(dry, rock, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(24f, 36f, steep)));
                    c = Color.Lerp(c, mountain, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14f, 30f, h)));
                    if (waterModel.SurfaceAt(new Vector3(wx, 0, wz), out float ws) && ws > h + .02f)
                        c = Color.Lerp(river, deepWater, Mathf.InverseLerp(.1f, 1.8f, ws - h)) / Mathf.Max(.6f, shade) * .95f;
                    // curvas de nivel cada 3 m
                    float contour = Mathf.Abs(Mathf.Repeat(h, 3f) - 1.5f);
                    if (contour > 1.42f && steep < 40f) c *= .9f;
                    px[y * N + x] = c * shade;
                }
            var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
            tex.SetPixels(px);
            // Árboles: puntos verde oscuro.
            foreach (var t in trees)
            {
                int cx = (int)((t.x + half) / (2 * half) * N), cy = (int)((t.y + half) / (2 * half) * N);
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        if (dx * dx + dy * dy > 9) continue;
                        int X = cx + dx, Y = cy + dy;
                        if (X < 0 || Y < 0 || X >= N || Y >= N) continue;
                        tex.SetPixel(X, Y, Color.Lerp(tex.GetPixel(X, Y), new Color(.22f, .3f, .17f), .75f));
                    }
            }
            tex.Apply();
            string path = Root + "/T_Valle_Mapa.png";
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Fx(string name, Transform parent, Vector3 pos)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent); go.transform.position = pos;
        }

        // ------------------------------------------------------------------ refugio → valle
        private static void IntegrateRefugio()
        {
            var scene = EditorSceneManager.OpenScene(RefugioBuilder.ScenePath, OpenSceneMode.Single);
            var session = Object.FindFirstObjectByType<RefugioSession>();
            var t = session.GetComponent<RefugioZoneTransition>() ?? session.gameObject.AddComponent<RefugioZoneTransition>();
            t.Configure(session, SceneName, 4.5f);
            EditorUtility.SetDirty(t); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("No se pudo guardar " + RefugioBuilder.ScenePath);
        }
    }
}
