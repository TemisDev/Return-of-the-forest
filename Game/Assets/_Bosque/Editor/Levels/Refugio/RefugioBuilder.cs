using System;
using System.Collections.Generic;
using System.Linq;
using Bosque.Abilities;
using Bosque.ArtV6;
using Bosque.Audio;
using Bosque.Character;
using Bosque.CharacterEditor;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
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
    /// Zona 3, "El refugio perdido". Construye la escena con el kit de arte V6, la Guardiana, el zorro de neblina y el
    /// audio de zona, y conecta la salida del claro con esta zona. Idempotente: reescribe solo sus salidas.
    ///
    /// Trazado (metros, suelo en y=0, 28 × 44 m): se llega por el puente de raíces del claro (sur, z −4).
    /// En la mitad sur están el tronco hueco y las piedras. En el norte, el paso del animal va del bosque del oeste
    /// (z ≈ 31) a la madriguera entre rocas del noreste (11; 34), marcado por huellas. Hay cuatro lugares indicados:
    /// dos en el paso, con sombra y cobertura (válidos), y dos expuestos en el centro (pista). Un lecho húmedo junto a
    /// la madriguera admite el matorral (Q) y otro seco en el centro da pista.
    /// </summary>
    public static class RefugioBuilder
    {
        public const string ScenePath = "Assets/_Bosque/Scenes/Zonas/RefugioPerdido.unity";
        public const string SceneName = "RefugioPerdido";
        public const string MaterialsDir = "Assets/_Bosque/Art/Zonas/Refugio/Materials";
        private static System.Random rng;
        private static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // Paso del animal (también es la ruta del zorro al recuperar la zona).
        public static readonly Vector3[] Route =
        {
            new Vector3(-13.2f, 0, 31f), new Vector3(-8.5f, 0, 27.8f), new Vector3(-3.4f, 0, 27f),
            new Vector3(2f, 0, 27.8f), new Vector3(6.6f, 0, 29.6f), new Vector3(9.6f, 0, 32.6f), new Vector3(11f, 0, 34f),
        };
        private static readonly Vector3 SpotTocon = new Vector3(-3.6f, 0, 25.3f), SpotRocas = new Vector3(9.4f, 0, 30.3f);
        private static readonly Vector3 SpotCentro = new Vector3(-1f, 0, 12.5f), SpotSol = new Vector3(5.5f, 0, 16.5f);
        private static readonly Vector3 BedHumid = new Vector3(6.4f, 0, 27f), BedDry = new Vector3(-4.5f, 0, 15.5f);
        private static readonly Vector3 LogHome = new Vector3(-6.5f, 0, 6f), RocksHome = new Vector3(7.5f, 0, 8f);
        private static readonly Vector3 Den = new Vector3(11.6f, 0, 35f);

        [MenuItem("Tools/Bosque/Zonas/Construir El refugio perdido e integrar la transición")]
        public static void BuildAll()
        {
            var fox = FaunaBuilder.Build();
            var icons = ReadArroyoIcons();
            BuildScene(icons, fox);
            IntegrateClaro();
            // Con el menú principal (Bloque 05) generado, va primero.
            var scenes = new List<EditorBuildSettingsScene>();
            if (System.IO.File.Exists("Assets/_Bosque/Scenes/MenuPrincipal.unity")) scenes.Add(new EditorBuildSettingsScene("Assets/_Bosque/Scenes/MenuPrincipal.unity", true));
            scenes.Add(new EditorBuildSettingsScene(ClaroBuilder.ArroyoScene, true)); scenes.Add(new EditorBuildSettingsScene(ClaroBuilder.ScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Refugio] Zona construida e integrada.");
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try { BuildAll(); } catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static Texture2D[] ReadArroyoIcons()
        {
            EditorSceneManager.OpenScene(ClaroBuilder.ArroyoScene, OpenSceneMode.Single);
            var hud = Object.FindFirstObjectByType<ArroyoPrototypeHud>();
            var so = new SerializedObject(hud);
            return new[] { "interactionIcon", "progressIcon", "restorationIcon", "exitIcon" }
                .Select(n => so.FindProperty(n).objectReferenceValue as Texture2D).ToArray();
        }

        // ------------------------------------------------------------------ utilidades
        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(parent, false);
            g.transform.position = center; g.transform.localScale = size;
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            var r = g.GetComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat; else r.enabled = false;
            return g;
        }

        private static GameObject Put(string prefab, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static Material Variant(string source, string name, Action<Material> tweak)
        {
            System.IO.Directory.CreateDirectory(MaterialsDir);
            string path = $"{MaterialsDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(B.M(source)) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(B.M(source));
            tweak(m); EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject Disc(string name, Transform parent, Vector3 pos, float sx, float sz, Material mat, float lift, float yaw)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(d.GetComponent<Collider>());
            d.name = name; d.transform.SetParent(parent, false);
            d.transform.SetPositionAndRotation(pos + new Vector3(0, lift, 0), Quaternion.Euler(0, yaw, 0));
            d.transform.localScale = new Vector3(sx, .004f, sz);
            var r = d.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return d;
        }

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }

        private static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static float DistanceToRoute(Vector3 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < Route.Length; i++)
            {
                var a = Route[i - 1]; var ab = Route[i] - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector3.Distance(p, a + ab * t));
            }
            return best;
        }

        private static bool Reserved(Vector3 p)
        {
            if (DistanceToRoute(p) < 1.4f) return true;                                     // paso del animal
            foreach (var s in new[] { SpotTocon, SpotRocas, SpotCentro, SpotSol, BedHumid, BedDry, LogHome, RocksHome })
                if ((p - s).magnitude < 2f) return true;
            if ((p - Den).magnitude < 3.2f) return true;
            if (Mathf.Abs(p.x) < 1.6f && p.z < 3f) return true;                             // llegada
            return false;
        }

        // ------------------------------------------------------------------ escena
        private static void BuildScene(Texture2D[] icons, GameObject foxPrefab)
        {
            rng = new System.Random(20261122);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("RefugioPerdido").transform;
            var geo = Group(level, "Geometry");
            var degraded = new GameObject("DegradedVisuals").transform; degraded.SetParent(level, false);
            var restored = new GameObject("RestoredVisuals").transform; restored.SetParent(level, false);
            var markers = Group(level, "Markers");
            var stat = Group(level, "V6_Estatico");

            // Suelo y muros (collider de juego) + pieles visuales por estado. El muro sur deja ver el puente de llegada.
            var groundD = B.M("M_Ground_Bank_Degraded"); var groundR = B.M("M_Ground_Bank_Restored");
            var cliffD = B.M("M_Ground_Cliff_Degraded"); var cliffR = B.M("M_Ground_Cliff_Restored");
            Box("Suelo", geo, new Vector3(0, -.5f, 18f), new Vector3(28f, 1f, 44f), null, true);
            Box("MuroOeste", geo, new Vector3(-14.5f, 1.5f, 18f), new Vector3(1f, 5f, 46f), null, true);
            Box("MuroEste", geo, new Vector3(14.5f, 1.5f, 18f), new Vector3(1f, 5f, 46f), null, true);
            Box("MuroSur", geo, new Vector3(0, 1.5f, -4.5f), new Vector3(30f, 5f, 1f), null, true);
            Box("MuroNorte", geo, new Vector3(0, 1.5f, 40.5f), new Vector3(30f, 5f, 1f), null, true);
            var skins = new (string n, Vector3 c, Vector3 s, bool cliff)[]
            {
                ("Suelo", new Vector3(0, -.5f, 18f), new Vector3(28f, 1f, 44f), false),
                ("MuroOeste", new Vector3(-14.5f, 1.5f, 18f), new Vector3(1f, 5f, 46f), true),
                ("MuroEste", new Vector3(14.5f, 1.5f, 18f), new Vector3(1f, 5f, 46f), true),
                ("MuroSurO", new Vector3(-8.5f, 1.5f, -4.5f), new Vector3(13f, 5f, 1f), true),
                ("MuroSurE", new Vector3(8.5f, 1.5f, -4.5f), new Vector3(13f, 5f, 1f), true),
                ("MuroNorte", new Vector3(0, 1.5f, 40.5f), new Vector3(30f, 5f, 1f), true),
            };
            var gD = Group(degraded, "V6_Suelo"); var gR = Group(restored, "V6_Suelo");
            foreach (var (n, c, s, cliff) in skins)
            {
                Box("V6_" + n + "_Degradado", gD, c, s, cliff ? cliffD : groundD, false);
                Box("V6_" + n + "_Restaurado", gR, c, s, cliff ? cliffR : groundR, false);
            }

            // Llegada: el puente de raíces del claro termina aquí ("usar el paso" del GDD), sobre la barranca sur.
            var arrival = Group(stat, "PuenteLlegada");
            Box("V6_Barranca_Fondo", arrival, new Vector3(0, -6.6f, -9.5f), new Vector3(28f, .4f, 10f), B.M("M_Ground_Bed_Degraded"), false);
            Box("V6_Barranca_Pared", arrival, new Vector3(0, -3.5f, -14.6f), new Vector3(28f, 7f, .4f), cliffD, false);
            var living = AssetDatabase.LoadAssetAtPath<Material>($"{ClaroBuilder.MaterialsDir}/M_Claro_TroncoVivo.mat") ?? B.M("M_DeadLog");
            var bridgeLog = Put("VIS_V6_DeadLog_A", arrival, new Vector3(0, -.5f, -9.5f), 90f, 1f);
            bridgeLog.transform.localScale = new Vector3(3.3f, 1.3f, 1.3f);
            foreach (var r in bridgeLog.GetComponentsInChildren<Renderer>()) r.sharedMaterial = living;
            Put("VIS_V6_RootCluster_A", arrival, new Vector3(.2f, -.2f, -3.6f), 180, .75f);
            // Bordes del hueco del muro sur: rocas grandes que suavizan el corte.
            foreach (var x in new[] { -2.3f, 2.3f })
                Put("VIS_V6_RockMossLarge_" + "BD"[x < 0 ? 0 : 1], arrival, new Vector3(x, 0, -4.3f), R(0, 360), .9f);
            Put("VIS_V6_PineRoots_A", arrival, new Vector3(-1.1f, -.4f, -6.2f), 90, 1.1f);
            Put("VIS_V6_PineRoots_B", arrival, new Vector3(1.1f, -.4f, -11.8f), 270, 1.1f);
            for (float z = -14f; z <= -4.6f; z += 1.2f)
                foreach (var side in new[] { -1f, 1f })
                    Put("VIS_V6_RockMoss_" + "ABC"[rng.Next(3)], arrival, new Vector3(side * R(.65f, .85f), -.18f, z + R(-.3f, .3f)), R(0, 360), R(.14f, .2f));
            for (float x = -12.5f; x <= 12.5f; x += R(2.2f, 3.2f))
                if (Mathf.Abs(x) > 2f) Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", arrival, new Vector3(x, -5.4f, -9.5f + R(-2f, 2f)), R(0, 360), R(.6f, .9f));

            // Huellas del animal a lo largo del paso (la pista principal): pares de marcas pequeñas y oscuras.
            var printMat = Variant("M_Ground_Bank_Degraded", "M_Refugio_Huella", m =>
            {
                m.SetColor("_TopColor", new Color(.42f, .33f, .26f)); m.SetColor("_SideColor", new Color(.42f, .33f, .26f));
                m.SetFloat("_MacroStrength", .1f); m.SetFloat("_SmoothnessScale", .4f);
            });
            // Solo en el estado degradado: son la pista; al recuperarse, el pasto las cubre.
            var prints = Group(degraded, "V6_Huellas");
            int printCount = 0;
            for (int i = 1; i < Route.Length; i++)
            {
                var a = Route[i - 1]; var b = Route[i]; var dir = (b - a).normalized; var side = Vector3.Cross(Vector3.up, dir);
                float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
                for (float d = .3f; d < Vector3.Distance(a, b) - .2f; d += .42f)
                {
                    bool left = (printCount++ & 1) == 0;
                    var p = a + dir * d + side * (left ? .07f : -.07f) + side * Mathf.Sin(d * 1.3f) * .05f;
                    Disc("Huella", prints, p, .07f, .095f, printMat, .0035f, yaw + R(-12f, 12f));
                }
            }

            // Madriguera entre rocas al noreste (destino del zorro) y bosque al oeste (de donde sale).
            var den = Group(stat, "V6_Madriguera");
            Put("VIS_V6_RockMossLarge_A", den, Den + new Vector3(1.2f, 0, 1.4f), 200, .9f);
            Put("VIS_V6_RockMossLarge_C", den, Den + new Vector3(-1.4f, 0, 1.5f), 40, .75f);
            Put("VIS_V6_RockMossLarge_E", den, Den + new Vector3(1.6f, 0, -1.2f), 120, .6f);
            var hole = Variant("M_Ground_Bank_Degraded", "M_Refugio_Boca", m =>
            {
                m.SetColor("_TopColor", new Color(.07f, .06f, .05f)); m.SetColor("_SideColor", new Color(.07f, .06f, .05f));
                m.SetFloat("_SmoothnessScale", 0f);
            });
            Disc("V6_BocaMadriguera", den, Den + new Vector3(0, 0, .6f), .9f, .6f, hole, .004f, 0);
            Put("VIS_V6_RootCluster_A", den, Den + new Vector3(-.2f, 0, 1.1f), 180, .55f);
            var westD = Group(degraded, "V6_BosqueOeste"); var westR = Group(restored, "V6_BosqueOeste");
            for (int k = 0; k < 11; k++)
            {
                var p = new Vector3(R(-13.4f, -10.6f), 0, R(23f, 39f));
                if ((p - Route[0]).magnitude < 1.6f) continue;
                string kind = rng.NextDouble() < .5 ? "A" : "B"; float s = R(1.5f, 2f), yaw = R(0, 360);
                Put($"VIS_V6_Tree{kind}_Green", westR, p, yaw, s);
                Put(rng.NextDouble() < .7 ? $"VIS_V6_Tree{kind}_Dead" : "VIS_V6_Stump_A", westD, p, yaw, rng.NextDouble() < .7 ? s : .7f);
            }
            foreach (var off in new[] { new Vector3(.6f, 0, 1.6f), new Vector3(-.4f, 0, -1.7f), new Vector3(1.6f, 0, -1f) })
                Put("VIS_V6_Shrub_" + "ABCD"[rng.Next(4)], westR, Route[0] + off, R(0, 360), R(3.4f, 4.2f));

            // Lugares indicados: anillo de piedras sobre tierra removida. Los válidos tienen sombra/cobertura y huellas al lado.
            var spotsRoot = Group(level, "LugaresRefugio");
            var spots = new List<RefugeSpot>();
            RefugeSpot Spot(string id, Vector3 pos, bool ok, string hint, float placeYaw)
            {
                var go = new GameObject("Lugar_" + id); go.transform.SetParent(spotsRoot, false); go.transform.position = pos;
                var col = go.AddComponent<BoxCollider>(); col.center = new Vector3(0, .03f, 0); col.size = new Vector3(1.7f, .06f, 1.7f);
                var soil = Disc("V6_Tierra", go.transform, pos, 1.6f, 1.6f, B.M("M_Ground_Bed_Degraded"), .006f, R(0, 360));
                for (int k = 0; k < 9; k++)
                {
                    float a = k / 9f * Mathf.PI * 2f;
                    Put(k % 2 == 0 ? "VIS_V6_BoulderDry_A" : "VIS_V6_BoulderDry_B", go.transform, pos + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .92f, R(0, 360), R(.1f, .14f)).name = "V6_Piedra";
                }
                var pose = new GameObject("Colocacion").transform; pose.SetParent(go.transform, false);
                pose.SetPositionAndRotation(pos, Quaternion.Euler(0, placeYaw, 0));
                var spot = go.AddComponent<RefugeSpot>();
                spot.Configure("refugio.lugar." + id, ok, hint, col, pose);
                spots.Add(spot);
                return spot;
            }
            float RouteYaw(int i) => Quaternion.LookRotation(Route[i] - Route[i - 1]).eulerAngles.y + 90f;
            Spot("tocon", SpotTocon, true, "", RouteYaw(2));
            Spot("rocas", SpotRocas, true, "", RouteYaw(5));
            Spot("centro", SpotCentro, false, "Demasiado expuesto: en medio del claro no hay sombra ni huellas. Sigue el rastro del animal hacia el norte.", 0);
            Spot("sol", SpotSol, false, "Aquí cualquier depredador lo vería. Las huellas pasan junto al tocón y las rocas del norte: el refugio va donde el animal ya camina.", 0);
            // Cobertura junto a los válidos (en ambos estados): tocón y helechos, y las rocas de la madriguera.
            Put("VIS_V6_Stump_A", stat, SpotTocon + new Vector3(-1.4f, 0, -.9f), R(0, 360), .8f);
            Put("VIS_V6_DryBranch_A", stat, SpotTocon + new Vector3(1.2f, .02f, -1.1f), R(0, 360), 1f);
            Put("VIS_V6_RockMoss_D", stat, SpotRocas + new Vector3(1.3f, 0, .2f), R(0, 360), .55f);
            Put("VIS_V6_RockMoss_F", stat, SpotRocas + new Vector3(.4f, 0, -1.4f), R(0, 360), .45f);

            // Elementos de refugio: tronco hueco y montón de piedras, en la mitad sur.
            var itemsRoot = Group(level, "ElementosRefugio");
            RefugeItem Item(string id, string label, string cue, Vector3 pos, float yaw, Action<Transform> visuals)
            {
                var go = new GameObject("Elemento_" + id); go.transform.SetParent(itemsRoot, false);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                var vis = Group(go.transform, "Visual"); visuals(vis);
                foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
                var b = RendererBounds(vis.gameObject);
                var col = go.AddComponent<BoxCollider>();
                col.center = go.transform.InverseTransformPoint(b.center);
                var local = Quaternion.Inverse(go.transform.rotation) * b.size;
                col.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)) * .9f;
                var item = go.AddComponent<RefugeItem>();
                item.Configure("refugio." + id, label, cue, col);
                return item;
            }
            var hollow = Variant("M_DeadLog", "M_Refugio_Hueco", m => m.SetColor("_BaseColor", new Color(.16f, .11f, .08f)));
            var log = Item("tronco", "el tronco hueco", "remove.log", LogHome, 70f, v =>
            {
                // El tronco del kit es largo en su eje X: se mide sin girar, se escala a 1,6 m y luego se orienta.
                var l = Put("VIS_V6_DeadLog_A", v, LogHome, 0f, 1f);
                var lb = RendererBounds(l);
                l.transform.localScale = new Vector3(1.6f / lb.size.x, .55f / Mathf.Max(.01f, lb.size.y), .55f / Mathf.Max(.01f, lb.size.z));
                l.transform.rotation = Quaternion.Euler(0, 70f, 0);
                lb = RendererBounds(l); l.transform.position += new Vector3(LogHome.x - lb.center.x, -lb.min.y, LogHome.z - lb.center.z);
                // Boca oscura en un extremo, algo menor que el grosor del tronco: se lee como "hueco".
                lb = RendererBounds(l); float thick = lb.size.y * .62f;
                var mouth = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(mouth.GetComponent<Collider>());
                mouth.name = "V6_Hueco"; mouth.transform.SetParent(v, false);
                var axis = Quaternion.Euler(0, 70f, 0) * Vector3.right;
                mouth.transform.SetPositionAndRotation(new Vector3(LogHome.x, lb.center.y, LogHome.z) + axis * .79f, Quaternion.LookRotation(axis) * Quaternion.Euler(90, 0, 0));
                mouth.transform.localScale = new Vector3(thick, .008f, thick);
                mouth.GetComponent<MeshRenderer>().sharedMaterial = hollow;
            });
            var rocks = Item("piedras", "las piedras", "remove.rocks", RocksHome, 0f, v =>
            {
                var offs = new[] { new Vector3(-.25f, 0, -.1f), new Vector3(.22f, 0, -.15f), new Vector3(0, 0, .25f), new Vector3(.02f, .18f, 0) };
                for (int k = 0; k < offs.Length; k++)
                    Put(k == 3 ? "VIS_V6_RockMoss_" + "AB"[rng.Next(2)] : "VIS_V6_BoulderDry_" + "AB"[k % 2], v, RocksHome + offs[k], R(0, 360), k == 3 ? .22f : R(.24f, .3f));
            });

            // Lechos para el matorral (Q): húmedo junto a la madriguera (válido) y seco en el centro (pista).
            var wet = AssetDatabase.LoadAssetAtPath<Material>($"{ClaroBuilder.MaterialsDir}/M_Claro_TierraHumeda.mat") ?? B.M("M_Ground_Bank_Restored");
            var bedsRoot = Group(level, "PuntosCrecimiento");
            var beds = new List<GrowthPoint>();
            foreach (var (pos, humid) in new[] { (BedHumid, true), (BedDry, false) })
            {
                var go = new GameObject(humid ? "Lecho_Humedo" : "Lecho_Seco"); go.transform.SetParent(bedsRoot, false); go.transform.position = pos;
                var trigger = go.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .9f; trigger.center = Vector3.up * .3f;
                Disc("V6_Tierra", go.transform, pos, 1.7f, 1.7f, humid ? wet : B.M("M_Ground_Bed_Degraded"), .008f, R(0, 360));
                if (humid)
                {
                    for (int k = 0; k < 5; k++) Disc("V6_Humedad", go.transform, pos + new Vector3(R(-1.4f, 1.4f), 0, R(-1.2f, 1.2f)), R(.8f, 1.4f), R(.8f, 1.3f), wet, .005f + k * .0004f, R(0, 360));
                    Put("VIS_V6_RockMoss_B", go.transform, pos + new Vector3(.7f, 0, .6f), R(0, 360), .3f).name = "V6_Musgo";
                    Put("VIS_V6_GrassA_TallA", go.transform, pos + new Vector3(-.9f, 0, .5f), R(0, 360), 2f);
                }
                else Put("VIS_V6_DryBranch_C", go.transform, pos + new Vector3(-.3f, .02f, .2f), R(0, 360), .7f).name = "V6_RamaSeca";
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f;
                    Put("VIS_V6_RockMoss_" + "ABC"[k % 3], go.transform, pos + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .95f, R(0, 360), R(.1f, .14f)).name = "V6_Piedra";
                }
                var plant = new GameObject("V6_Matorral"); plant.transform.SetParent(go.transform, false);
                Put("VIS_V6_Shrub_B", plant.transform, pos, R(0, 360), 4.6f);
                Put("VIS_V6_Shrub_D", plant.transform, pos + new Vector3(.5f, 0, -.4f), R(0, 360), 3.4f);
                Put("VIS_V6_Fern_A", plant.transform, pos + new Vector3(-.6f, 0, -.3f), R(0, 360), 1.8f);
                var gp = go.AddComponent<GrowthPoint>();
                gp.Configure(humid ? "refugio.matorral" : "refugio.seco", humid,
                    humid ? "" : "Suelo seco y expuesto: el matorral no prospera. Junto a la madriguera la tierra está húmeda.", plant);
                beds.Add(gp);
            }

            // Vegetación por estado. En el restaurado el paso queda bordeado de matorrales: un corredor conectado.
            var vD = Group(degraded, "V6_Vegetacion"); var vR = Group(restored, "V6_Vegetacion");
            string[] grass = { "VIS_V6_GrassA_LargeA", "VIS_V6_GrassA_LargeB", "VIS_V6_GrassA_MidA", "VIS_V6_GrassA_MidB", "VIS_V6_GrassA_TallA", "VIS_V6_GrassB_C", "VIS_V6_GrassB_D" };
            Vector3 Field() { for (int k = 0; k < 30; k++) { var p = new Vector3(R(-13f, 13f), 0, R(-3.5f, 39.5f)); if (!Reserved(p)) return p; } return new Vector3(0, float.NaN, 0); }
            void Scatter(Transform parent, string[] set, int n, float a, float b, bool shadow)
            {
                for (int k = 0; k < n; k++)
                {
                    var p = Field(); if (float.IsNaN(p.y)) continue;
                    var g = Put(set[rng.Next(set.Length)], parent, p, R(0, 360), R(a, b));
                    if (!shadow) foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            Scatter(vR, grass, 230, 2.2f, 3.6f, false);
            Scatter(vR, new[] { "VIS_V6_Fern_A", "VIS_V6_Fern_B", "VIS_V6_Fern_C", "VIS_V6_Fern_D" }, 36, 1.3f, 2f, false);
            Scatter(vR, new[] { "VIS_V6_FlowerYellow_A", "VIS_V6_FlowerYellow_C", "VIS_V6_Dandelion_A", "VIS_V6_Dandelion_C" }, 60, 1.5f, 2.3f, false);
            Scatter(vD, grass.Select(g => g + "_Dry").ToArray(), 120, 1.4f, 2.4f, false);
            Scatter(vD, new[] { "VIS_V6_DryBranch_A", "VIS_V6_DryBranch_B" }, 14, .8f, 1.3f, true);
            for (int i = 1; i < Route.Length - 1; i++)
            {
                var a = Route[i - 1]; var b = Route[i]; var side = Vector3.Cross(Vector3.up, (b - a).normalized);
                for (float t = .2f; t < 1f; t += .45f)
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var p = Vector3.Lerp(a, b, t) + side * s * R(1.6f, 2.2f);
                        if (CorridorReserved(p)) continue;
                        Put("VIS_V6_Shrub_" + "ABCD"[rng.Next(4)], vR, p, R(0, 360), R(3f, 3.8f));
                    }
            }
            var tD = Group(degraded, "V6_Arboles"); var tR = Group(restored, "V6_Arboles");
            for (float z = -2f; z <= 38f; z += R(4.5f, 6.5f))
                foreach (var x in new[] { R(12f, 13f), z < 22f ? R(-13f, -12f) : float.NaN })
                {
                    if (float.IsNaN(x)) continue;
                    var p = new Vector3(x, 0, z); if ((p - Den).magnitude < 3.5f) continue;
                    string kind = rng.NextDouble() < .5 ? "A" : "B"; float s = R(1.5f, 1.9f), yaw = R(0, 360);
                    Put($"VIS_V6_Tree{kind}_Green", tR, p, yaw, s);
                    if (rng.NextDouble() < .65) Put($"VIS_V6_Tree{kind}_Dead", tD, p, yaw, s); else Put("VIS_V6_Stump_A", tD, p, yaw, R(.55f, .75f));
                }
            var rD = Group(degraded, "V6_Rocas"); var rR = Group(restored, "V6_Rocas");
            for (int k = 0; k < 12; k++)
            {
                var p = Field(); if (float.IsNaN(p.y)) continue;
                float s = R(.3f, .6f), yaw = R(0, 360);
                Put("VIS_V6_RockMoss_" + "ABCDEFG"[rng.Next(7)], rR, p, yaw, s);
                Put(rng.NextDouble() < .5 ? "VIS_V6_BoulderDry_A" : "VIS_V6_BoulderDry_B", rD, p, yaw, s * .6f);
            }

            // Bordes y fondo (sin colliders).
            var back = Group(level, "V6_Fondo");
            for (float z = -4f; z <= 40f; z += R(3.4f, 4.8f))
                foreach (var x in new[] { -14.6f, 14.6f })
                    Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", back, new Vector3(x + R(-.3f, .3f), 3.6f, z), R(0, 360), R(.75f, 1.05f));
            for (float x = -12f; x <= 12f; x += R(3.4f, 4.6f))
            {
                Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", back, new Vector3(x, 3.6f, 40.6f), R(0, 360), R(.75f, 1.05f));
                if (Mathf.Abs(x) > 2.5f) Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", back, new Vector3(x, 3.6f, -4.6f), R(0, 360), R(.75f, 1.05f));
            }
            var plateau = B.M("M_Ground_Cliff_Restored");
            Box("V6_MesetaOeste", back, new Vector3(-40f, -3.5f, 18f), new Vector3(50f, 15f, 140f), plateau, false);
            Box("V6_MesetaEste", back, new Vector3(40f, -3.5f, 18f), new Vector3(50f, 15f, 140f), plateau, false);
            Box("V6_MesetaSur", back, new Vector3(0, -7.5f, -40f), new Vector3(31f, 15f, 50f), groundR, false);
            Box("V6_MesetaNorte", back, new Vector3(0, -3.5f, 66f), new Vector3(31f, 15f, 50f), plateau, false);
            var bD = Group(degraded, "V6_BosqueFondo"); var bR = Group(restored, "V6_BosqueFondo");
            for (int k = 0; k < 34; k++)
            {
                float side = rng.NextDouble() < .5 ? -1 : 1;
                var p = rng.NextDouble() < .7 ? new Vector3(side * R(16.5f, 28f), 4f, R(-14f, 52f)) : new Vector3(R(-13f, 13f), 4f, side < 0 ? R(-30f, -18f) : R(42f, 52f));
                if (p.z < -14f) p.y = 0f;
                string kind = rng.NextDouble() < .5 ? "A" : "B"; float s = R(1.6f, 2.3f), yaw = R(0, 360);
                Put($"VIS_V6_Tree{kind}_Green", bR, p, yaw, s);
                if (rng.NextDouble() < .55) Put($"VIS_V6_Tree{kind}_Dead", bD, p, yaw, s);
            }
            foreach (var r in back.GetComponentsInChildren<Renderer>().Concat(bD.GetComponentsInChildren<Renderer>()).Concat(bR.GetComponentsInChildren<Renderer>()))
                r.shadowCastingMode = ShadowCastingMode.Off;

            var fxD = Group(degraded, "V6_FX"); var fxR = Group(restored, "V6_FX");
            Fx("PFX_V6_DustMotes", fxD, new Vector3(0, 1.3f, 14f)); Fx("PFX_V6_DustGusts", fxD, new Vector3(0, .15f, 20f));
            Fx("PFX_V6_FallingLeaves_Dry", fxD, new Vector3(0, 7f, 20f));
            Fx("PFX_V6_Pollen", fxR, new Vector3(0, 1.6f, 22f)); Fx("PFX_V6_Butterflies", fxR, new Vector3(2f, .9f, 27f));
            Fx("PFX_V6_FallingLeaves", fxR, new Vector3(0, 7f, 22f));
            restored.gameObject.SetActive(false);

            // Fauna: zorro de neblina oculto hasta la recuperación; ruta por el paso hasta la madriguera.
            var faunaRoot = Group(level, "Fauna");
            var wps = Route.Select((p, i) => { var t = new GameObject($"Paso_{i:00}").transform; t.SetParent(faunaRoot, false); t.position = p; return t; }).ToArray();
            var fox = (GameObject)PrefabUtility.InstantiatePrefab(foxPrefab, faunaRoot); fox.name = "ZorroNeblina";
            fox.transform.position = Route[0];
            var route = faunaRoot.gameObject.AddComponent<FaunaRoute>();
            route.Configure(fox, fox.GetComponentInChildren<Animator>(), wps, FaunaBuilder.WalkClipSpeed);
            var vantage = new GameObject("VistaFauna").transform; vantage.SetParent(markers, false); vantage.position = new Vector3(1.5f, 2.4f, 19.5f);

            // Jugador.
            var spawn = new GameObject("PlayerSpawn").transform; spawn.SetParent(markers, false); spawn.position = new Vector3(0, 0, -1.5f);
            var safe = new GameObject("SafeRespawn").transform; safe.SetParent(markers, false); safe.position = new Vector3(0, 0, -1f);
            var gameplay = new GameObject("Gameplay").transform;
            var player = new GameObject("Player"); player.transform.SetParent(gameplay, false); player.transform.position = spawn.position;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.center = Vector3.up * .9f; cc.radius = .35f; cc.stepOffset = .3f; cc.skinWidth = .03f; cc.slopeLimit = 45f;
            var motor = player.AddComponent<ThirdPersonMotor>(); motor.SetRespawn(safe);
            var interactor = player.AddComponent<PlayerInteractor>();
            var carrier = player.AddComponent<ItemCarrier>();
            var ability = player.AddComponent<GrowthAbility>();
            var guardiana = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GuardianaBuilder.PrefabPath), player.transform);
            guardiana.name = "Guardiana"; guardiana.transform.localPosition = new Vector3(0, -cc.skinWidth, 0);
            var camGo = new GameObject("GameplayCamera"); camGo.transform.SetParent(gameplay, false); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = .05f; cam.farClipPlane = 250f; cam.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
            var rig = camGo.AddComponent<ThirdPersonCamera>();
            var cso = new SerializedObject(rig); cso.FindProperty("focusHeight").floatValue = 1.45f; cso.FindProperty("distance").floatValue = 4.2f; cso.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms(); rig.Configure(player.transform);
            player.AddComponent<ThirdPersonInput>().Configure(rig);

            var sessionGo = new GameObject("RefugioSession"); sessionGo.transform.SetParent(gameplay, false);
            var cinematic = sessionGo.AddComponent<FaunaCinematic>();
            cinematic.Configure(route, rig, vantage, player.transform);
            var session = sessionGo.AddComponent<RefugioSession>();
            session.Configure(new[] { log, rocks }, spots.ToArray(), beds.ToArray(), degraded.gameObject, restored.gameObject, route, cinematic, motor, spawn);
            sessionGo.AddComponent<RefugioHud>().Configure(session, interactor, ability, carrier, icons[0], icons[1], icons[2], icons[3]);

            // Luz y ambiente V6.
            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft; sunGo.transform.rotation = Quaternion.Euler(40f, 310f, 0f);
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
            ambience.burstPosition = new Vector3(2f, 0, 26f);
            ambience.ApplyImmediate(false);

            // Audio de zona.
            var audio = new GameObject("Audio_Refugio");
            var sfx = audio.AddComponent<SfxPlayer>(); sfx.Configure(AssetDatabase.LoadAssetAtPath<SfxLibrary>(GuardianaBuilder.SfxLibraryPath));
            var ambGo = new GameObject("Ambiente"); ambGo.transform.SetParent(audio.transform, false);
            var ac = ambGo.AddComponent<AmbienceController>();
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Bosque/Audio/Ambience/{n}.ogg");
            ac.loops = new List<AmbienceController.LoopLayer>
            {
                new AmbienceController.LoopLayer { name = "Viento seco", clip = A("AMB_Viento_Seco"), volume = .5f, weightDegradedRestored = new Vector2(1f, .15f) },
                new AmbienceController.LoopLayer { name = "Brisa en hojas", clip = A("AMB_Brisa_Hojas"), volume = .55f, weightDegradedRestored = new Vector2(.1f, 1f) },
                new AmbienceController.LoopLayer { name = "Aves", clip = A("AMB_Aves"), volume = .5f, weightDegradedRestored = new Vector2(.05f, 1f) },
            };
            ac.oneShots = new List<AmbienceController.RandomOneShots>
            {
                new AmbienceController.RandomOneShots { name = "Ramas secas", clips = new[] { A("AMB_Rama_Seca_01"), A("AMB_Rama_Seca_02"), A("AMB_Rama_Seca_03") }, volume = .45f, weightDegradedRestored = new Vector2(1f, 0f), intervalSeconds = new Vector2(7f, 15f) },
                new AmbienceController.RandomOneShots { name = "Aves cercanas", clips = new[] { A("AMB_Ave_01"), A("AMB_Ave_03"), A("AMB_Ave_04"), A("AMB_Ave_05"), A("AMB_Ave_06") }, volume = .55f, weightDegradedRestored = new Vector2(0f, 1f), intervalSeconds = new Vector2(2.5f, 7f) },
            };
            var musGo = new GameObject("Musica"); musGo.transform.SetParent(audio.transform, false);
            musGo.AddComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Bosque/Audio/Music/MUS_Bosque_Hermoso.ogg");
            var music = musGo.AddComponent<MusicPlayer>();
            audio.AddComponent<RefugioAudioBridge>().Configure(session, sfx, ac, music, guardiana.GetComponent<CharacterAnimationDriver>());

            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("No se pudo guardar " + ScenePath);
            int renderersD = degraded.GetComponentsInChildren<Renderer>(true).Length, renderersR = restored.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[Refugio] Escena guardada: {spots.Count} lugares, {printCount} huellas, renderers degradado {renderersD}, restaurado {renderersR}, ruta {route.RouteLength:F1} m.");
        }

        private static bool CorridorReserved(Vector3 p)
        {
            foreach (var s in new[] { SpotTocon, SpotRocas, BedHumid }) if ((p - s).magnitude < 1.8f) return true;
            return (p - Den).magnitude < 2.8f || DistanceToRoute(p) < 1.2f;
        }

        private static void Fx(string name, Transform parent, Vector3 pos)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent); go.transform.position = pos;
        }

        // ------------------------------------------------------------------ claro → refugio
        private static void IntegrateClaro()
        {
            var scene = EditorSceneManager.OpenScene(ClaroBuilder.ScenePath, OpenSceneMode.Single);
            var session = Object.FindFirstObjectByType<ClaroSession>();
            var t = session.GetComponent<ClaroZoneTransition>() ?? session.gameObject.AddComponent<ClaroZoneTransition>();
            t.Configure(session, SceneName, 4.5f);
            var hud = Object.FindFirstObjectByType<ClaroHud>(); hud.ConfigureNextZone("El refugio perdido");
            EditorUtility.SetDirty(t); EditorUtility.SetDirty(hud); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("No se pudo guardar " + ClaroBuilder.ScenePath);
        }
    }
}
