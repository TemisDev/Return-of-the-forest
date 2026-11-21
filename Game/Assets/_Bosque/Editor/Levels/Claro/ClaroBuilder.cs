using System;
using System.Collections.Generic;
using System.Linq;
using Bosque.Abilities;
using Bosque.ArtV6;
using Bosque.Audio;
using Bosque.Character;
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
    /// Zona 2, "El claro vacío". Construye la escena con el kit de arte V6, la Guardiana y el audio modular,
    /// y conecta la salida del arroyo con esta zona. Idempotente: reescribe solo sus salidas.
    ///
    /// Trazado (metros, suelo en y=0): campo sur z -4..20, barranca z 20..24 (4 m, no se puede saltar),
    /// campo norte z 24..42 con el arco de salida. Cinco lechos: tres húmedos (manantial y borde de la barranca)
    /// y dos secos. Con los tres brotes nativos crece un puente de raíces sobre la barranca.
    /// </summary>
    public static class ClaroBuilder
    {
        public const string ScenePath = "Assets/_Bosque/Scenes/Zonas/ClaroVacio.unity";
        public const string ArroyoScene = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";
        public const string SceneName = "ClaroVacio";
        private static System.Random rng;
        private static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        [MenuItem("Tools/Bosque/Zonas/Construir El claro vacío e integrar la transición")]
        public static void BuildAll()
        {
            var icons = ReadArroyoIcons();
            BuildScene(icons);
            IntegrateArroyo();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ArroyoScene, true), new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Claro] Zona construida e integrada.");
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try { BuildAll(); } catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static Texture2D[] ReadArroyoIcons()
        {
            EditorSceneManager.OpenScene(ArroyoScene, OpenSceneMode.Single);
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
            if (mat != null) r.sharedMaterial = mat; else r.enabled = false; // collider invisible: solo donde hay visual encima
            return g;
        }

        private static GameObject Put(string prefab, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public const string MaterialsDir = "Assets/_Bosque/Art/Zonas/Claro/Materials";

        /// <summary>Copia un material V6 con ajustes propios de la zona (se reescribe en cada ejecución).</summary>
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

        private static GameObject Disc(string name, Transform parent, Vector3 pos, float sx, float sz, Material mat, float lift)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(d.GetComponent<Collider>());
            d.name = name; d.transform.SetParent(parent, false);
            d.transform.SetPositionAndRotation(pos + new Vector3(0, lift, 0), Quaternion.Euler(0, R(0, 360), 0));
            d.transform.localScale = new Vector3(sx, .006f, sz);
            d.GetComponent<MeshRenderer>().sharedMaterial = mat;
            d.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return d;
        }

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }

        private static readonly Vector3[] HumidPoints = { new Vector3(-6.5f, 0, 4.5f), new Vector3(-10.5f, 0, 10.5f), new Vector3(2.6f, 0, 18.2f) };
        private static readonly Vector3[] DryPoints = { new Vector3(7f, 0, 6f), new Vector3(10.5f, 0, 14f) };
        private static readonly Vector3 Pond = new Vector3(-9.5f, 0, 6.5f);

        private static bool Reserved(Vector3 p)
        {
            if (Mathf.Abs(p.x) < 1.6f && p.z > 16f && p.z < 28f) return true;          // puente y su acceso
            if (p.z > 19.4f && p.z < 24.6f) return true;                                // barranca
            if (Mathf.Abs(p.x) < 3f && p.z > 37.5f) return true;                        // arco
            if ((new Vector2(p.x, p.z) - new Vector2(Pond.x, Pond.z)).magnitude < 3.2f) return true;
            foreach (var g in HumidPoints.Concat(DryPoints)) if ((p - g).magnitude < 1.5f) return true;
            if (Mathf.Abs(p.x) < 1.2f && p.z < 2f) return true;                         // inicio
            return false;
        }

        // ------------------------------------------------------------------ escena
        private static void BuildScene(Texture2D[] icons)
        {
            rng = new System.Random(20261121);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("ClaroVacio").transform;
            var geo = Group(level, "Geometry");
            var degraded = new GameObject("DegradedVisuals").transform; degraded.SetParent(level, false);
            var restored = new GameObject("RestoredVisuals").transform; restored.SetParent(level, false);
            var markers = Group(level, "Markers");

            // Suelo y muros con collider (gameplay) + pieles visuales por estado.
            var groundD = B.M("M_Ground_Bank_Degraded"); var groundR = B.M("M_Ground_Bank_Restored");
            var cliffD = B.M("M_Ground_Cliff_Degraded"); var cliffR = B.M("M_Ground_Cliff_Restored");
            var blocks = new (string name, Vector3 c, Vector3 s, bool cliff)[]
            {
                ("SueloSur", new Vector3(0, -.5f, 8f), new Vector3(28f, 1f, 24f), false),
                ("SueloNorte", new Vector3(0, -.5f, 33f), new Vector3(28f, 1f, 18f), false),
                ("MuroOeste", new Vector3(-14.5f, 1.5f, 19f), new Vector3(1f, 5f, 48f), true),
                ("MuroEste", new Vector3(14.5f, 1.5f, 19f), new Vector3(1f, 5f, 48f), true),
                ("MuroSur", new Vector3(0, 1.5f, -4.5f), new Vector3(30f, 5f, 1f), true),
                ("MuroNorte", new Vector3(0, 1.5f, 42.5f), new Vector3(30f, 5f, 1f), true),
            };
            var gD = Group(degraded, "V6_Suelo"); var gR = Group(restored, "V6_Suelo");
            foreach (var (name, c, s, cliff) in blocks)
            {
                Box(name, geo, c, s, null, true);
                Box("V6_" + name + "_Degradado", gD, c, s, cliff ? cliffD : groundD, false);
                Box("V6_" + name + "_Restaurado", gR, c, s, cliff ? cliffR : groundR, false);
            }
            // Barranca: paredes y fondo solo visuales (caer devuelve al punto seguro).
            var stat = Group(level, "V6_Estatico");
            Box("V6_Barranca_ParedSur", stat, new Vector3(0, -3.5f, 20.2f), new Vector3(28f, 6f, .4f), cliffD, false);
            Box("V6_Barranca_ParedNorte", stat, new Vector3(0, -3.5f, 23.8f), new Vector3(28f, 6f, .4f), cliffD, false);
            Box("V6_Barranca_Fondo", stat, new Vector3(0, -6.6f, 22f), new Vector3(28f, .4f, 4f), B.M("M_Ground_Bed_Degraded"), false);
            for (float x = -12.5f; x <= 12.5f; x += R(2.2f, 3.2f))
                Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", stat, new Vector3(x, -5.4f, 22f + R(-1f, 1f)), R(0, 360), R(.6f, .9f));

            // Manantial y suelo húmedo (pista visual): agua, barro oscuro y un reguero hasta la barranca.
            var water = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(water.GetComponent<Collider>());
            water.name = "V6_Manantial"; water.transform.SetParent(stat, false);
            water.transform.SetPositionAndRotation(Pond + new Vector3(0, .03f, 0), Quaternion.Euler(90, 0, 0));
            water.transform.localScale = new Vector3(4.2f, 3.4f, 1);
            // Agua quieta sin espuma de contacto (el plano está a ras de suelo) y con borde irregular.
            water.GetComponent<MeshRenderer>().sharedMaterial = Variant("M_Water_Stream", "M_Claro_Manantial", m =>
            {
                m.SetFloat("_FoamDistance", .003f); m.SetFloat("_FlowFoam", 0f); m.SetFloat("_PuddleShape", 1f); m.SetFloat("_FlowSpeed", .12f);
                m.SetFloat("_Murkiness", .35f); m.SetColor("_ShallowColor", new Color(.3f, .46f, .42f)); m.SetColor("_DeepColor", new Color(.03f, .14f, .15f));
                m.SetColor("_FoamColor", new Color(.16f, .13f, .09f, 0f)); m.SetFloat("_EdgeFade", .25f);
            });
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // Tierra húmeda: más oscura y brillante que el suelo seco. Manchas redondeadas, no losas.
            var wet = Variant("M_Ground_Bank_Degraded", "M_Claro_TierraHumeda", m =>
            {
                m.SetColor("_TopColor", new Color(.6f, .5f, .42f)); m.SetColor("_SideColor", new Color(.6f, .5f, .42f));
                m.SetFloat("_SmoothnessScale", 1.8f); m.SetFloat("_MacroStrength", .35f);
            });
            for (int k = 0; k < 9; k++)
            {
                float a = k / 9f * Mathf.PI * 2f;
                Disc("V6_BarroManantial", stat, Pond + new Vector3(Mathf.Cos(a) * 2.2f, 0, Mathf.Sin(a) * 1.8f), R(1.6f, 2.6f), R(1.4f, 2.4f), wet, .003f + k * .0004f);
            }
            for (float t = 0; t <= 1f; t += .07f)
            {
                var p = Vector3.Lerp(Pond + new Vector3(2.2f, 0, 2.4f), HumidPoints[2] + new Vector3(-.6f, 0, -.4f), t);
                p.x += Mathf.Sin(t * 9f) * .6f;
                Disc("V6_Reguero", stat, p, R(.8f, 1.3f), R(.9f, 1.5f), wet, .002f + t * .002f);
            }
            // Juncos alrededor del agua (en ambos estados): pista de humedad.
            for (int k = 0; k < 14; k++)
            {
                float a = R(0, Mathf.PI * 2f), d = R(2.1f, 2.8f);
                var g = Put(k % 2 == 0 ? "VIS_V6_GrassA_TallA" : "VIS_V6_GrassA_TallB", stat, Pond + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d * .85f), R(0, 360), R(1.8f, 2.6f));
                foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var p in new[] { Pond + new Vector3(-2.2f, 0, 1.4f), Pond + new Vector3(1.9f, 0, -1.9f), Pond + new Vector3(-1.4f, 0, -2.1f) })
                Put("VIS_V6_RockMoss_" + "ABCDE"[rng.Next(5)], stat, p, R(0, 360), R(.35f, .5f));

            // Lechos de siembra: anillo de piedras + suelo (húmedo u oscuro con musgo / seco y agrietado) + planta oculta.
            var pointsRoot = Group(level, "PuntosCrecimiento");
            var points = new List<GrowthPoint>();
            int idx = 0;
            foreach (var (pos, humid) in HumidPoints.Select(p => (p, true)).Concat(DryPoints.Select(p => (p, false))))
            {
                idx++;
                var go = new GameObject($"Lecho_{idx:00}_{(humid ? "Humedo" : "Seco")}");
                go.transform.SetParent(pointsRoot, false); go.transform.position = pos;
                var trigger = go.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .9f; trigger.center = Vector3.up * .3f;
                var soil = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(soil.GetComponent<Collider>());
                soil.name = "V6_Tierra"; soil.transform.SetParent(go.transform, false);
                soil.transform.localPosition = new Vector3(0, .01f, 0); soil.transform.localScale = new Vector3(1.7f, .012f, 1.7f);
                soil.GetComponent<MeshRenderer>().sharedMaterial = humid ? wet : B.M("M_Ground_Bed_Degraded");
                soil.transform.localPosition = new Vector3(0, .012f, 0);
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f;
                    var s = Put(k % 2 == 0 ? "VIS_V6_BoulderDry_A" : "VIS_V6_BoulderDry_B", go.transform, pos + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .95f, R(0, 360), R(.11f, .16f));
                    s.name = "V6_Piedra";
                }
                if (humid) Put("VIS_V6_RockMoss_B", go.transform, pos + new Vector3(.55f, 0, .5f), R(0, 360), .22f).name = "V6_Musgo";
                else Put("VIS_V6_DryBranch_C", go.transform, pos + new Vector3(-.3f, .02f, .2f), R(0, 360), .7f).name = "V6_RamaSeca";
                var plant = new GameObject("V6_Planta"); plant.transform.SetParent(go.transform, false);
                Put("VIS_V6_Shrub_" + "ABCD"[idx % 4], plant.transform, pos, R(0, 360), R(3.6f, 4.2f));
                Put("VIS_V6_Fern_" + "ABCD"[(idx + 1) % 4], plant.transform, pos + new Vector3(.5f, 0, -.3f), R(0, 360), 1.7f);
                Put("VIS_V6_FlowerYellow_" + "ABCDE"[idx % 5], plant.transform, pos + new Vector3(-.4f, 0, .4f), R(0, 360), 2.0f);
                Put("VIS_V6_Dandelion_" + "ABCDE"[(idx + 2) % 5], plant.transform, pos + new Vector3(.2f, 0, .6f), R(0, 360), 1.9f);
                var gp = go.AddComponent<GrowthPoint>();
                string hint = humid ? "" : (idx == 4 ? "Suelo seco y agrietado: aquí la planta no echa raíces. Busca tierra oscura cerca del agua."
                                                     : "Demasiado seco y expuesto. El manantial deja un rastro húmedo: síguelo.");
                gp.Configure(humid ? $"claro.brote.{idx}" : $"claro.seco.{idx}", humid, hint, plant);
                points.Add(gp);
            }

            // Puente de raíces (aparece al recuperar el claro). Collider de paso + tronco cubierto de musgo.
            var bridge = new GameObject("PuenteRaices"); bridge.transform.SetParent(level, false); bridge.transform.position = new Vector3(0, 0, 22f);
            var bc = bridge.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -.25f, 0); bc.size = new Vector3(1.8f, .5f, 8.4f);
            var log = Put("VIS_V6_DeadLog_A", bridge.transform, new Vector3(0, -.5f, 22f), 90f, 1f);
            log.transform.localScale = new Vector3(2.6f, 1.3f, 1.3f);
            var living = Variant("M_DeadLog", "M_Claro_TroncoVivo", m => m.SetColor("_BaseColor", new Color(.66f, .74f, .5f)));
            foreach (var r in log.GetComponentsInChildren<Renderer>()) r.sharedMaterial = living;
            for (float z = 18.6f; z <= 25.4f; z += 1.1f)
                foreach (var side in new[] { -1f, 1f })
                    Put("VIS_V6_RockMoss_" + "ABC"[rng.Next(3)], bridge.transform, new Vector3(side * R(.65f, .85f), -.18f, z + R(-.3f, .3f)), R(0, 360), R(.14f, .2f));
            foreach (var z in new[] { 17.9f, 26.1f })
                Put("VIS_V6_RootCluster_A", bridge.transform, new Vector3(R(-.4f, .4f), -.2f, z), z < 20 ? 0 : 180, .75f);
            foreach (var (x, z) in new[] { (-.95f, 19.4f), (.95f, 21.2f), (-.9f, 23.1f), (1f, 24.8f) })
                Put("VIS_V6_Fern_" + "ABCD"[rng.Next(4)], bridge.transform, new Vector3(x, -.05f, z), R(0, 360), 1.3f);
            Put("VIS_V6_PineRoots_A", bridge.transform, new Vector3(-1.1f, -.4f, 20.2f), 90, 1.1f);
            Put("VIS_V6_PineRoots_B", bridge.transform, new Vector3(1.1f, -.4f, 23.8f), 270, 1.1f);
            bridge.SetActive(false);

            // Arco de salida al norte.
            var arch = Group(level, "Salida");
            foreach (var x in new[] { -2.6f, 2.6f })
            {
                Box("Pilar", arch, new Vector3(x, 1.25f, 41.2f), new Vector3(.8f, 2.5f, .8f), null, true);
                Box("V6_Pilar", arch, new Vector3(x, 1.25f, 41.2f), new Vector3(.8f, 2.5f, .8f), B.M("M_Ground_Cliff_Degraded"), false);
            }
            var lintel = Put("VIS_V6_DeadLog_A", arch, new Vector3(0, 2.4f, 41.2f), 0, 1f); lintel.transform.localScale = new Vector3(1.75f, .55f, .55f);
            var exitGo = new GameObject("ExitVolume"); exitGo.transform.SetParent(arch, false); exitGo.transform.position = new Vector3(0, 1f, 40.2f);
            var ev = exitGo.AddComponent<BoxCollider>(); ev.isTrigger = true; ev.size = new Vector3(4.4f, 2f, 1.6f);
            var exit = exitGo.AddComponent<ClaroExit>();

            // Árboles, rocas y vegetación por estado.
            var tD = Group(degraded, "V6_Arboles"); var tR = Group(restored, "V6_Arboles");
            for (float z = -2f; z <= 40f; z += R(4.5f, 6.5f))
                foreach (var x in new[] { R(-12.8f, -11.6f), R(11.6f, 12.8f) })
                {
                    if (z > 19 && z < 25) continue;
                    string kind = rng.NextDouble() < .5 ? "A" : "B"; float s = R(1.5f, 1.9f), yaw = R(0, 360);
                    var p = new Vector3(x, 0, z);
                    Put($"VIS_V6_Tree{kind}_Green", tR, p, yaw, s);
                    if (rng.NextDouble() < .65) Put($"VIS_V6_Tree{kind}_Dead", tD, p, yaw, s);
                    else Put("VIS_V6_Stump_A", tD, p, yaw, R(.55f, .75f));
                }
            var vD = Group(degraded, "V6_Vegetacion"); var vR = Group(restored, "V6_Vegetacion");
            string[] grass = { "VIS_V6_GrassA_LargeA", "VIS_V6_GrassA_LargeB", "VIS_V6_GrassA_MidA", "VIS_V6_GrassA_MidB", "VIS_V6_GrassA_TallA", "VIS_V6_GrassB_C", "VIS_V6_GrassB_D" };
            Vector3 Field() { for (int k = 0; k < 30; k++) { var p = new Vector3(R(-13f, 13f), 0, rng.NextDouble() < .55 ? R(-3.5f, 19.4f) : R(24.6f, 41.5f)); if (!Reserved(p)) return p; } return new Vector3(0, float.NaN, 0); }
            void Scatter(Transform parent, string[] set, int n, float a, float b, bool castShadow)
            {
                for (int k = 0; k < n; k++)
                {
                    var p = Field(); if (float.IsNaN(p.y)) continue;
                    var g = Put(set[rng.Next(set.Length)], parent, p, R(0, 360), R(a, b));
                    if (!castShadow) foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            Scatter(vR, grass, 240, 2.2f, 3.6f, false);
            Scatter(vR, new[] { "VIS_V6_Fern_A", "VIS_V6_Fern_B", "VIS_V6_Fern_C", "VIS_V6_Fern_D" }, 40, 1.3f, 2f, false);
            Scatter(vR, new[] { "VIS_V6_FlowerYellow_A", "VIS_V6_FlowerYellow_C", "VIS_V6_Dandelion_A", "VIS_V6_Dandelion_C" }, 70, 1.5f, 2.3f, false);
            Scatter(vR, new[] { "VIS_V6_Shrub_A", "VIS_V6_Shrub_B", "VIS_V6_Shrub_C" }, 24, 3.4f, 5f, true);
            Scatter(vD, grass.Select(g => g + "_Dry").ToArray(), 120, 1.4f, 2.4f, false);
            Scatter(vD, new[] { "VIS_V6_DryBranch_A", "VIS_V6_DryBranch_B" }, 14, .8f, 1.3f, true);
            var rD = Group(degraded, "V6_Rocas"); var rR = Group(restored, "V6_Rocas");
            for (int k = 0; k < 14; k++)
            {
                var p = Field(); if (float.IsNaN(p.y)) continue;
                float s = R(.3f, .6f), yaw = R(0, 360);
                Put("VIS_V6_RockMoss_" + "ABCDEFG"[rng.Next(7)], rR, p, yaw, s);
                Put(rng.NextDouble() < .5 ? "VIS_V6_BoulderDry_A" : "VIS_V6_BoulderDry_B", rD, p, yaw, s * .6f);
            }

            // Bordes: rocas grandes sobre los muros y bosque/mesetas de fondo (sin colliders).
            var back = Group(level, "V6_Fondo");
            for (float z = -4f; z <= 42f; z += R(3.4f, 4.8f))
                foreach (var x in new[] { -14.6f, 14.6f })
                    Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", back, new Vector3(x + R(-.3f, .3f), 3.6f, z), R(0, 360), R(.75f, 1.05f));
            for (float x = -12f; x <= 12f; x += R(3.4f, 4.6f))
                foreach (var z in new[] { -4.6f, 42.6f })
                    Put("VIS_V6_Cliff_" + "ABCDEF"[rng.Next(6)] + "_Degraded", back, new Vector3(x, 3.6f, z), R(0, 360), R(.75f, 1.05f));
            var plateau = B.M("M_Ground_Cliff_Restored");
            Box("V6_MesetaOeste", back, new Vector3(-40f, -3.5f, 19f), new Vector3(50f, 15f, 140f), plateau, false);
            Box("V6_MesetaEste", back, new Vector3(40f, -3.5f, 19f), new Vector3(50f, 15f, 140f), plateau, false);
            Box("V6_MesetaSur", back, new Vector3(0, -3.5f, -30f), new Vector3(31f, 15f, 50f), plateau, false);
            Box("V6_MesetaNorte", back, new Vector3(0, -3.5f, 68f), new Vector3(31f, 15f, 50f), plateau, false);
            var bD = Group(degraded, "V6_BosqueFondo"); var bR = Group(restored, "V6_BosqueFondo");
            for (int k = 0; k < 34; k++)
            {
                float side = rng.NextDouble() < .5 ? -1 : 1;
                var p = rng.NextDouble() < .7 ? new Vector3(side * R(16.5f, 28f), 4f, R(-14f, 52f)) : new Vector3(R(-13f, 13f), 4f, side < 0 ? R(-14f, -6f) : R(44f, 54f));
                string kind = rng.NextDouble() < .5 ? "A" : "B"; float s = R(1.6f, 2.3f), yaw = R(0, 360);
                Put($"VIS_V6_Tree{kind}_Green", bR, p, yaw, s);
                if (rng.NextDouble() < .55) Put($"VIS_V6_Tree{kind}_Dead", bD, p, yaw, s);
            }
            foreach (var r in back.GetComponentsInChildren<Renderer>().Concat(bD.GetComponentsInChildren<Renderer>()).Concat(bR.GetComponentsInChildren<Renderer>()))
                r.shadowCastingMode = ShadowCastingMode.Off;

            // Partículas por estado.
            var fxD = Group(degraded, "V6_FX"); var fxR = Group(restored, "V6_FX");
            Fx("PFX_V6_DustMotes", fxD, new Vector3(0, 1.3f, 12f)); Fx("PFX_V6_DustGusts", fxD, new Vector3(0, .15f, 18f));
            Fx("PFX_V6_FallingLeaves_Dry", fxD, new Vector3(0, 7f, 18f));
            Fx("PFX_V6_Pollen", fxR, new Vector3(0, 1.6f, 14f)); Fx("PFX_V6_Butterflies", fxR, new Vector3(-4f, .9f, 10f));
            Fx("PFX_V6_FallingLeaves", fxR, new Vector3(0, 7f, 18f)); Fx("PFX_V6_ExitGlow", fxR, new Vector3(0, 1.3f, 41f));
            Fx("PFX_V6_WaterSplash", stat, Pond + new Vector3(-1.6f, 0, 1.2f));
            restored.gameObject.SetActive(false);

            // Marcadores y jugador.
            var spawn = new GameObject("PlayerSpawn").transform; spawn.SetParent(markers, false); spawn.position = new Vector3(0, 0, -1.5f);
            var safe = new GameObject("SafeRespawn").transform; safe.SetParent(markers, false); safe.position = new Vector3(0, 0, -1f);
            var gameplay = new GameObject("Gameplay").transform;
            var player = new GameObject("Player"); player.transform.SetParent(gameplay, false); player.transform.position = spawn.position;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.center = Vector3.up * .9f; cc.radius = .35f; cc.stepOffset = .3f; cc.skinWidth = .03f; cc.slopeLimit = 45f;
            var motor = player.AddComponent<ThirdPersonMotor>(); motor.SetRespawn(safe);
            player.AddComponent<PlayerInteractor>();
            var ability = player.AddComponent<GrowthAbility>();
            var guardiana = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Bosque.CharacterEditor.GuardianaBuilder.PrefabPath), player.transform);
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

            var sessionGo = new GameObject("ClaroSession"); sessionGo.transform.SetParent(gameplay, false);
            var session = sessionGo.AddComponent<ClaroSession>();
            session.Configure(points.ToArray(), degraded.gameObject, restored.gameObject, bridge, motor, spawn);
            exit.Configure(session);
            sessionGo.AddComponent<ClaroHud>().Configure(session, ability, icons[0], icons[1], icons[2], icons[3]);

            // Luz y ambiente V6 (cielo, niebla y post-proceso ligados a RestoredVisuals).
            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft; sunGo.transform.rotation = Quaternion.Euler(42f, 300f, 0f);
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
            ambience.burstPosition = new Vector3(0, 0, 10f);
            ambience.ApplyImmediate(false);

            // Audio de zona.
            var audio = new GameObject("Audio_Claro");
            var sfx = audio.AddComponent<SfxPlayer>(); sfx.Configure(AssetDatabase.LoadAssetAtPath<SfxLibrary>(Bosque.CharacterEditor.GuardianaBuilder.SfxLibraryPath));
            var ambGo = new GameObject("Ambiente"); ambGo.transform.SetParent(audio.transform, false);
            var ac = ambGo.AddComponent<AmbienceController>();
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Bosque/Audio/Ambience/{n}.ogg");
            var spring = new GameObject("Manantial").transform; spring.SetParent(ambGo.transform, false); spring.position = Pond;
            ac.loops = new List<AmbienceController.LoopLayer>
            {
                new AmbienceController.LoopLayer { name = "Viento seco", clip = A("AMB_Viento_Seco"), volume = .5f, weightDegradedRestored = new Vector2(1f, .15f) },
                new AmbienceController.LoopLayer { name = "Brisa en hojas", clip = A("AMB_Brisa_Hojas"), volume = .55f, weightDegradedRestored = new Vector2(.1f, 1f) },
                new AmbienceController.LoopLayer { name = "Aves", clip = A("AMB_Aves"), volume = .5f, weightDegradedRestored = new Vector2(.05f, 1f) },
                new AmbienceController.LoopLayer { name = "Manantial", clip = A("AMB_Corriente_3D"), volume = .45f, weightDegradedRestored = new Vector2(.5f, 1f), anchor = spring, minDistance = 1.5f, maxDistance = 14f },
            };
            ac.oneShots = new List<AmbienceController.RandomOneShots>
            {
                new AmbienceController.RandomOneShots { name = "Ramas secas", clips = new[] { A("AMB_Rama_Seca_01"), A("AMB_Rama_Seca_02"), A("AMB_Rama_Seca_03") }, volume = .45f, weightDegradedRestored = new Vector2(1f, 0f), intervalSeconds = new Vector2(7f, 15f) },
                new AmbienceController.RandomOneShots { name = "Aves cercanas", clips = new[] { A("AMB_Ave_01"), A("AMB_Ave_03"), A("AMB_Ave_04"), A("AMB_Ave_05"), A("AMB_Ave_06") }, volume = .55f, weightDegradedRestored = new Vector2(0f, 1f), intervalSeconds = new Vector2(2.5f, 7f) },
            };
            var musGo = new GameObject("Musica"); musGo.transform.SetParent(audio.transform, false);
            musGo.AddComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Bosque/Audio/Music/MUS_Bosque_Hermoso.ogg");
            var music = musGo.AddComponent<MusicPlayer>();
            audio.AddComponent<ClaroAudioBridge>().Configure(session, sfx, ac, music, guardiana.GetComponent<CharacterAnimationDriver>());

            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("No se pudo guardar " + ScenePath);
            int renderersD = degraded.GetComponentsInChildren<Renderer>(true).Length, renderersR = restored.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[Claro] Escena guardada: {points.Count} lechos ({HumidPoints.Length} húmedos), renderers degradado {renderersD}, restaurado {renderersR}.");
        }

        private static void Fx(string name, Transform parent, Vector3 pos)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent); go.transform.position = pos;
        }

        // ------------------------------------------------------------------ arroyo → claro
        private static void IntegrateArroyo()
        {
            var scene = EditorSceneManager.OpenScene(ArroyoScene, OpenSceneMode.Single);
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var t = session.GetComponent<ArroyoZoneTransition>() ?? session.gameObject.AddComponent<ArroyoZoneTransition>();
            t.Configure(session, SceneName, 4.5f);
            Object.FindFirstObjectByType<ArroyoPrototypeHud>().ConfigureNextZone("El claro vacío");
            EditorUtility.SetDirty(t); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("No se pudo guardar " + ArroyoScene);
        }
    }
}
