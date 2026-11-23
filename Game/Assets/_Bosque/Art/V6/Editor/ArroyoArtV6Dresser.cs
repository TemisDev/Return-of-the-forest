using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using B = Bosque.ArtV6.Editor.ArroyoArtV6Builder;

namespace Bosque.ArtV6.Editor
{
    /// <summary>
    /// Viste una escena del arroyo con el arte V6 sin tocar gameplay:
    /// - No modifica, mueve ni borra colliders, scripts, marcadores ni IDs.
    /// - Solo desactiva MeshRenderers antiguos y añade visuales sin collider bajo
    ///   DegradedVisuals / RestoredVisuals / Obstacle_0X / ExitBlocker/VisualMount / ArtV6.
    /// La escena de muestra es una copia: Assets/_Bosque/Art/V6/Showcase/ArroyoV6_Showcase.unity.
    /// </summary>
    public static class ArroyoArtV6Dresser
    {
        public const string SourceScene = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";
        public const string ShowcaseScene = B.Root + "/Showcase/ArroyoV6_Showcase.unity";
        private const string Tag = "V6";

        private static System.Random _rng;
        private static float Rand(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
        private static T Pick<T>(IList<T> list) => list[_rng.Next(list.Count)];

        [MenuItem("Tools/Bosque/Arte/V6/2 - Crear escena de muestra (copia jugable)")]
        public static void BuildShowcase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ShowcaseScene));
            if (File.Exists(ShowcaseScene))
            {
                // Reemplaza el contenido conservando el .meta: el GUID de la escena de muestra no cambia entre ejecuciones.
                File.Copy(SourceScene, ShowcaseScene, true);
                AssetDatabase.ImportAsset(ShowcaseScene, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset(SourceScene, ShowcaseScene)) throw new Exception("No se pudo copiar la escena base");
            var scene = EditorSceneManager.OpenScene(ShowcaseScene, OpenSceneMode.Single);
            Dress();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[V6] Escena de muestra lista: " + ShowcaseScene);
        }

        /// <summary>Integración: viste y guarda la escena principal (ArroyoPlayable). Uso en batch.</summary>
        public static void DressPlayableBatch()
        {
            int code = 0;
            try
            {
                var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
                Dress();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("No se pudo guardar " + SourceScene);
                Debug.Log("[V6] Escena principal vestida y guardada: " + SourceScene);
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------------ vestido
        [MenuItem("Tools/Bosque/Arte/V6/3 - Vestir la escena abierta con arte V6")]
        public static void Dress()
        {
            _rng = new System.Random(20261120);
            var degraded = Find("DegradedVisuals");
            var restored = Find("RestoredVisuals");
            if (degraded == null || restored == null) throw new Exception("La escena no tiene DegradedVisuals/RestoredVisuals");

            // Limpieza idempotente de una ejecución anterior
            foreach (var t in AllTransforms().Where(t => t != null && t.name.StartsWith(Tag + "_")).ToList())
                if (t != null) Object.DestroyImmediate(t.gameObject);
            var oldRoot = GameObject.Find("ArtV6"); if (oldRoot) Object.DestroyImmediate(oldRoot);

            HideLegacyRenderers();

            var root = new GameObject("ArtV6").transform;
            var deg = Group(degraded, "V6_Degraded");
            var res = Group(restored, "V6_Restored");

            GroundSkins(deg, res, root);
            Water(deg, res);
            Obstacles();
            ExitBarrier();
            Gate(root, deg, res);
            OptionalLog(root);
            Trees(deg, res);
            Rocks(deg, res);
            Vegetation(deg, res);
            Backdrop(deg, res);
            Particles(deg, res);
            var ambience = Ambience(root, restored.gameObject);
            SingleRestorationEffect(root, ambience);
            NoFarShadows(deg); NoFarShadows(res);
            Debug.Log($"[V6] Escena vestida. Degradado: {Count(deg)} renderers, restaurado: {Count(res)} renderers.");
        }

        /// <summary>Lo que está fuera del recorrido (meseta, valle, fondo) no proyecta sombras: ahorra pases de sombra.</summary>
        private static void NoFarShadows(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var p = r.bounds.center;
                bool far = p.x < -7f || p.x > 8f || p.y < -3f || p.z < -7f || p.z > 43f;
                if (far) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static int Count(Transform t) => t.GetComponentsInChildren<Renderer>(true).Length;

        private static IEnumerable<Transform> AllTransforms() =>
            EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));

        private static Transform Find(string name) => AllTransforms().FirstOrDefault(t => t.name == name);

        private static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        private static void HideLegacyRenderers()
        {
            int n = 0;
            foreach (var r in AllTransforms().SelectMany(t => t.GetComponents<MeshRenderer>()))
            {
                if (r.name == "ProvisionalBody") continue; // personaje provisional
                if (r.enabled) { r.enabled = false; n++; }
            }
            Debug.Log($"[V6] MeshRenderers antiguos desactivados: {n} (colliders intactos)");
        }

        private static GameObject Put(string prefab, Transform parent, Vector3 pos, Vector3 euler, float scale, Vector3? scale3 = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(prefab), parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = scale3 ?? Vector3.one * scale;
            return go;
        }

        private static GameObject PutQ(string prefab, Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(prefab), parent);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static GameObject Skin(Transform src, Transform parent, Material m, string suffix)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = $"{Tag}_{src.name}_{suffix}";
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(src.position, src.rotation);
            go.transform.localScale = src.lossyScale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        // ------------------------------------------------------------------ suelos
        private static void GroundSkins(Transform deg, Transform res, Transform root)
        {
            var gd = Group(deg, "V6_Ground"); var gr = Group(res, "V6_Ground");
            var map = new (string block, string d, string r)[]
            {
                ("Bank", "M_Ground_Bank_Degraded", "M_Ground_Bank_Restored"),
                ("Ramp_South", "M_Ground_Bank_Degraded", "M_Ground_Path_Restored"),
                ("Ramp_North", "M_Ground_Bank_Degraded", "M_Ground_Path_Restored"),
                ("ChannelFloor", "M_Ground_Bed_Degraded", "M_Ground_Bed_Restored"),
                ("WestWall", "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("SouthWall", "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("NorthWall", "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("EastRail_South", "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("EastRail_North", "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
            };
            foreach (var (block, d, r) in map)
            {
                var src = Find(block);
                if (src == null) { Debug.LogWarning("[V6] Bloque no encontrado: " + block); continue; }
                Skin(src, gd, B.M(d), "Degraded");
                Skin(src, gr, B.M(r), "Restored");
            }
            // Arco de salida: igual en ambos estados (piedra), con musgo solo restaurado
            var gs = Group(root, "V6_Static");
            foreach (var p in new[] { "GatePillar_W", "GatePillar_E" })
            {
                var src = Find(p);
                if (src) Skin(src, gs, B.M("M_Ground_Cliff_Degraded"), "Stone");
            }
        }

        // ------------------------------------------------------------------ agua
        private static void Water(Transform deg, Transform res)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(w.GetComponent<Collider>());
            w.name = "V6_Stream";
            w.transform.SetParent(res, false);
            w.transform.SetPositionAndRotation(new Vector3(-2.5f, -0.21f, 18f), Quaternion.Euler(90, 0, 0));
            w.transform.localScale = new Vector3(3.02f, 44.4f, 1f);
            var wr = w.GetComponent<MeshRenderer>(); wr.sharedMaterial = B.M("M_Water_Stream"); wr.shadowCastingMode = ShadowCastingMode.Off;

            // Charcos estancados aguas arriba de cada bloqueo (el agua no puede pasar)
            var pd = Group(deg, "V6_Puddles");
            foreach (float z in new[] { 8.4f, 16.2f, 24.3f, 2f, 33f })
            {
                int pieces = _rng.Next(2, 4);
                for (int i = 0; i < pieces; i++)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Quad);   // V16: plano (UV radial correcta), no un disco con canto
                    Object.DestroyImmediate(p.GetComponent<Collider>());
                    p.name = "V6_Puddle";
                    p.transform.SetParent(pd, false);
                    p.transform.position = new Vector3(Rand(-3.2f, -1.8f), -0.585f, z + Rand(-0.8f, 0.6f));
                    p.transform.rotation = Quaternion.Euler(90, Rand(0, 180), 0);
                    p.transform.localScale = new Vector3(Rand(1.2f, 2.2f), Rand(0.9f, 1.5f), 1f);
                    var r = p.GetComponent<MeshRenderer>(); r.sharedMaterial = B.M("M_Water_Stagnant"); r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        // ------------------------------------------------------------------ obstáculos
        private static void Obstacles()
        {
            var burst = AssetDatabase.LoadAssetAtPath<GameObject>(B.Root + "/Particles/PFX_V6_ObstacleRemoved.prefab");
            var hint = AssetDatabase.LoadAssetAtPath<GameObject>(B.Root + "/Particles/PFX_V6_ObstacleHint.prefab");

            Transform Visual(string obstacle)
            {
                var o = Find(obstacle) ?? throw new Exception("Falta " + obstacle);
                // Los montajes visuales anteriores se apagan; el GameObject y su collider siguen intactos.
                foreach (Transform c in o) if (c.name.Contains("VisualMount") || c.name.StartsWith("VIS_")) c.gameObject.SetActive(false);
                var v = new GameObject(Tag + "_Visual").transform;
                v.SetParent(o, false);
                var rb = v.gameObject.AddComponent<BosqueV6RemovalBurst>();
                rb.burstPrefab = burst;
                if (hint) ((GameObject)PrefabUtility.InstantiatePrefab(hint, v)).transform.localPosition = new Vector3(0, 0.1f, 0);
                return v;
            }

            // Tronco caído (arroyo.log). Collider 2.9 x 0.8 x 0.8, centro y=-0.2.
            var v1 = Visual("Obstacle_01");
            var log1 = Put("VIS_V6_DeadLog_A", v1, Vector3.zero, Vector3.zero, 1);
            Local(log1, new Vector3(0.05f, -0.42f, 0f), new Vector3(0, 3, 0), new Vector3(0.88f, 1.45f, 1.45f));
            // QA6-004: el tronco termina en el borde de la orilla (x≈-1), no bajo los pies de quien pulsa E,
            // y sigue cubriendo todo el ancho del collider.
            var c1 = v1.parent.GetComponent<Collider>();
            if (c1 != null) FitSpanX(log1, c1.bounds.min.x - 0.25f, Mathf.Max(c1.bounds.max.x + 0.03f, -1.02f));
            // Escombros apilados aguas arriba: leen como dique que corta el agua.
            Local(Put("VIS_V6_RootCluster_A", v1, Vector3.zero, Vector3.zero, 1), new Vector3(-0.5f, -0.42f, -0.3f), new Vector3(0, 170, 0), new Vector3(0.42f, 0.45f, 0.3f));
            Local(Put("VIS_V6_BoulderDry_A", v1, Vector3.zero, Vector3.zero, 1), new Vector3(0.9f, -0.45f, -0.35f), new Vector3(0, 40, 0), Vector3.one * 0.42f);
            Local(Put("VIS_V6_BoulderDry_B", v1, Vector3.zero, Vector3.zero, 1), new Vector3(-1.05f, -0.45f, 0.25f), new Vector3(0, 300, 0), Vector3.one * 0.3f);
            Local(Put("VIS_V6_DryBranch_B", v1, Vector3.zero, Vector3.zero, 1), new Vector3(0.2f, -0.2f, -0.45f), new Vector3(0, 95, 10), Vector3.one * 2.0f);
            Local(Put("VIS_V6_DryBranch_A", v1, Vector3.zero, Vector3.zero, 1), new Vector3(0.5f, 0.18f, 0.05f), new Vector3(8, 72, 0), Vector3.one * 1.5f);
            Local(Put("VIS_V6_DryBranch_C", v1, Vector3.zero, Vector3.zero, 1), new Vector3(-0.7f, -0.35f, 0.42f), new Vector3(0, 115, 6), Vector3.one * 1.4f);

            // Derrumbe de rocas (arroyo.rocks). Collider 2.8 x 1.0 x 1.4, centro y=-0.1.
            var v2 = Visual("Obstacle_02");
            Local(Put("VIS_V6_BoulderDry_A", v2, Vector3.zero, Vector3.zero, 1), new Vector3(-0.72f, -0.52f, 0.05f), new Vector3(0, 90, 0), Vector3.one * 1.0f);
            Local(Put("VIS_V6_BoulderDry_B", v2, Vector3.zero, Vector3.zero, 1), new Vector3(0.6f, -0.52f, -0.05f), new Vector3(0, 8, 0), Vector3.one * 0.78f);
            Local(Put("VIS_V6_BoulderDry_A", v2, Vector3.zero, Vector3.zero, 1), new Vector3(1.15f, -0.52f, 0.4f), new Vector3(0, 210, 0), Vector3.one * 0.55f);
            Local(Put("VIS_V6_DryBranch_B", v2, Vector3.zero, Vector3.zero, 1), new Vector3(-0.1f, 0.2f, -0.3f), new Vector3(15, 80, 0), Vector3.one * 1.4f);

            // Maraña de ramas y raíces (arroyo.branches). Collider 2.8 x 0.9 x 1.2, centro y=-0.15.
            var v3 = Visual("Obstacle_03");
            Local(Put("VIS_V6_RootCluster_A", v3, Vector3.zero, Vector3.zero, 1), new Vector3(0f, -0.47f, 0f), new Vector3(0, 0, 0), new Vector3(0.72f, 0.75f, 0.5f));
            Local(Put("VIS_V6_DryBranch_A", v3, Vector3.zero, Vector3.zero, 1), new Vector3(-0.3f, 0.05f, 0.2f), new Vector3(10, 78, 12), Vector3.one * 2.0f);
            Local(Put("VIS_V6_DryBranch_B", v3, Vector3.zero, Vector3.zero, 1), new Vector3(0.4f, 0.1f, -0.25f), new Vector3(-8, 104, -10), Vector3.one * 2.3f);
            Local(Put("VIS_V6_DryBranch_C", v3, Vector3.zero, Vector3.zero, 1), new Vector3(0.2f, -0.1f, 0.35f), new Vector3(0, 62, 20), Vector3.one * 2.2f);
        }

        private static void Local(GameObject go, Vector3 p, Vector3 e, Vector3 s)
        {
            go.transform.localPosition = p; go.transform.localRotation = Quaternion.Euler(e); go.transform.localScale = s;
        }

        /// <summary>Estira o encoge un objeto alargado en X (su eje largo) para que ocupe [minX, maxX] del mundo.</summary>
        private static void FitSpanX(GameObject go, float minX, float maxX)
        {
            var b = MeshBounds(go, true);
            float k = (maxX - minX) / Mathf.Max(0.01f, b.size.x);
            var s = go.transform.localScale; s.x *= k; go.transform.localScale = s;
            b = MeshBounds(go, true);
            go.transform.position += new Vector3(maxX - b.max.x, 0, 0);
        }

        // ------------------------------------------------------------------ barrera de salida
        private static void ExitBarrier()
        {
            var blocker = Find("ExitBlocker");
            if (blocker == null) { Debug.LogWarning("[V6] Sin ExitBlocker"); return; }
            var mount = blocker.Find("VisualMount") ?? blocker;
            foreach (Transform c in blocker) if (c.name.StartsWith("ArtV3")) c.gameObject.SetActive(false);
            var g = new GameObject(Tag + "_Brambles").transform;
            g.SetParent(mount, false);
            float z = blocker.position.z;
            for (float x = -4.6f; x <= 6.2f; x += 0.7f)
            {
                float y = x < -1f ? -0.6f : 0f;
                Put(Pick(new[] { "VIS_V6_DryBranch_A", "VIS_V6_DryBranch_B", "VIS_V6_DryBranch_C" }), g,
                    new Vector3(x + Rand(-0.2f, 0.2f), y, z + Rand(-0.15f, 0.15f)), new Vector3(Rand(-70, -40), Rand(-25, 25), Rand(-30, 30)), Rand(2.0f, 2.8f));
                Put(Pick(new[] { "VIS_V6_DryBranch_A", "VIS_V6_DryBranch_B" }), g,
                    new Vector3(x + Rand(-0.3f, 0.3f), y + Rand(0.4f, 1.4f), z + Rand(-0.2f, 0.2f)), new Vector3(Rand(-20, 20), Rand(70, 110), Rand(-40, 40)), Rand(1.8f, 2.6f));
            }
            Put("VIS_V6_RootCluster_A", g, new Vector3(-2.5f, -0.6f, z), new Vector3(0, 0, 0), 0.7f);
            Put("VIS_V6_RootCluster_A", g, new Vector3(1.2f, 0f, z), new Vector3(0, 180, 0), 0.75f);
            Put("VIS_V6_RootCluster_A", g, new Vector3(4.6f, 0f, z), new Vector3(0, 10, 0), 0.7f);
            Put("VIS_V6_RootCluster_A", g, new Vector3(-0.6f, 0f, z + 0.2f), new Vector3(0, 90, 0), 0.6f);
            Put("VIS_V6_RootCluster_A", g, new Vector3(3.0f, 0f, z - 0.2f), new Vector3(0, 250, 0), 0.65f);
            Put("VIS_V6_DeadLog_A", g, new Vector3(1.5f, 0.05f, z + 0.25f), new Vector3(0, 2, 0), 1, new Vector3(1.2f, 0.55f, 0.55f));

            // QA6-001: nada de la maraña sobresale por delante del collider de la barrera (lado del jugador):
            // lo que asoma se desplaza hacia la salida, que solo es accesible cuando la barrera ya no existe.
            var col = blocker.GetComponent<Collider>();
            if (col != null)
            {
                float front = col.bounds.min.z - 0.05f;
                foreach (Transform piece in g)
                {
                    var b = MeshBounds(piece.gameObject, true);
                    if (b.min.z < front) piece.position += new Vector3(0, 0, front - b.min.z);
                }
            }
        }

        // ------------------------------------------------------------------ arco de salida
        private static void Gate(Transform root, Transform deg, Transform res)
        {
            var lintel = Find("GateLintel");
            if (lintel == null) return;
            var gs = root.Find("V6_Static") ?? Group(root, "V6_Static");
            Put("VIS_V6_DeadLog_A", gs, lintel.position + new Vector3(0, -0.28f, 0), new Vector3(0, 0, 0), 1, new Vector3(1.48f, 0.5f, 0.5f));
            foreach (var px in new[] { 0.5f, 5.5f })
            {
                Put("VIS_V6_BoulderDry_A", gs, new Vector3(px, 0f, 38.5f), new Vector3(0, Rand(0, 360), 0), 0.6f);
                Put("VIS_V6_RockMoss_B", res, new Vector3(px + Rand(-0.3f, 0.3f), 2.45f, 38.4f), new Vector3(0, Rand(0, 360), 0), 0.45f);
                Put("VIS_V6_Fern_B", res, new Vector3(px, 2.9f, 38.3f), new Vector3(0, Rand(0, 360), 0), 1.4f);
            }
            for (int i = 0; i < 5; i++)
                Put("VIS_V6_Fern_" + "ABCD"[i % 4], res, new Vector3(Rand(0.8f, 5.2f), 2.5f, 38.5f + Rand(-0.2f, 0.2f)), new Vector3(0, Rand(0, 360), 180), Rand(0.9f, 1.3f));
        }

        // ------------------------------------------------------------------ árboles
        private static void Trees(Transform deg, Transform res)
        {
            var td = Group(deg, "V6_Trees"); var tr = Group(res, "V6_Trees");
            int i = 0;
            foreach (var t in AllTransforms().Where(t => t.name.StartsWith("Tree_") && t.parent && t.parent.name == "Trees").ToList())
            {
                // En la orilla jugable la copa debe quedar por encima de la cámara (≈2.7 m): árbol alto y esbelto.
                bool onBank = t.position.y < 1f;
                // Los árboles con collider usan el modelo B: su tronco es recto hasta los 3 m del collider
                // (el A se bifurca antes y no puede ocupar el collider entero, QA6-014).
                string kind = onBank ? "B_Tall" : "B";
                float s = onBank ? Rand(1.85f, 2.05f) : (i % 2 == 0 ? Rand(1.6f, 1.9f) : Rand(1.35f, 1.6f));
                float yaw = Rand(0, 360);
                var green = Put($"VIS_V6_Tree{kind}_Green", tr, t.position, new Vector3(0, yaw, 0), s);
                var dead = Put($"VIS_V6_Tree{kind}_Dead", td, t.position, new Vector3(0, yaw, 0), s);
                var trunk = t.GetComponentInChildren<CapsuleCollider>(true);
                if (trunk != null)
                {
                    if (onBank)
                    {
                        // QA6-002: de 24 orientaciones, la que deja menos corteza y ramas a la altura del cuerpo
                        // fuera del collider del tronco y dentro de la orilla transitable.
                        float best = yaw; int bestScore = int.MaxValue;
                        for (int k = 0; k < 24; k++)
                        {
                            float cand = yaw + k * 15f;
                            green.transform.SetPositionAndRotation(t.position, Quaternion.Euler(0, cand, 0));
                            AlignTrunkToCollider(green, null, trunk, false);
                            int score = BankIntrusion(green, trunk);
                            if (score < bestScore) { bestScore = score; best = cand; }
                        }
                        foreach (var go in new[] { green, dead }) go.transform.SetPositionAndRotation(t.position, Quaternion.Euler(0, best, 0));
                        Debug.Log($"[V6] {t.name}: orientación {best % 360:F0}°, vértices de madera en el paso {bestScore}");
                    }
                    AlignTrunkToCollider(green, dead, trunk); // QA6-014
                }
                i++;
            }
            // Bosque de fondo sobre los muros y más allá de la barranca este
            var extra = new List<Vector3>();
            for (float z = -3f; z <= 40f; z += Rand(4.2f, 6f)) extra.Add(new Vector3(Rand(-6.8f, -5.6f), 3f, z));
            for (float z = -6f; z <= 44f; z += Rand(3.2f, 5f)) extra.Add(new Vector3(Rand(-12f, -8f), 3f, z));
            foreach (var p in extra)
            {
                string kind = _rng.NextDouble() < 0.5 ? "A" : "B";
                float s = Rand(1.5f, 2.1f); float yaw = Rand(0, 360);
                Put($"VIS_V6_Tree{kind}_Green", tr, p, new Vector3(0, yaw, 0), s);
                if (_rng.NextDouble() < 0.6) Put($"VIS_V6_Tree{kind}_Dead", td, p, new Vector3(0, yaw, 0), s);
                else Put("VIS_V6_Stump_A", td, p, new Vector3(0, yaw, 0), Rand(0.7f, 0.9f));
            }
            // Tocones talados (relato de deforestación) solo en degradado. QA6-003: van en lo alto del muro oeste,
            // visibles desde la orilla pero fuera del paso, para que nadie atraviese un tocón sin collider.
            foreach (var p in new[] { new Vector3(-4.55f, 3f, 6.5f), new Vector3(-4.7f, 3f, 10.5f), new Vector3(-4.5f, 3f, 20.5f), new Vector3(-4.65f, 3f, 23.5f), new Vector3(-4.6f, 3f, 32f) })
                Put("VIS_V6_Stump_A", td, p, new Vector3(0, Rand(0, 360), 0), Rand(0.45f, 0.6f));
        }

        /// <summary>
        /// El pivote de los árboles V6 no está sobre el tronco. Endereza y desplaza ambos estados para que el
        /// tronco visible (submalla de corteza) ocupe el collider del tronco de juego, que no se toca.
        /// </summary>
        private static void AlignTrunkToCollider(GameObject green, GameObject dead, CapsuleCollider trunk, bool log = true)
        {
            var cb = trunk.bounds;
            float y0 = cb.min.y, y1 = cb.max.y;
            Vector3 target = new Vector3(cb.center.x, y0, cb.center.z);
            for (int pass = 0; pass < 3; pass++)
            {
                if (!BarkCentroid(green, y0, y0 + 0.5f, out var bottom, out _) || !BarkCentroid(green, y1 - 0.5f, y1, out var top, out _))
                {
                    Debug.LogWarning($"[V6] {green.name}: no se encontró corteza a la altura del collider {trunk.transform.parent?.name}");
                    return;
                }
                // Enderezar el tramo de tronco que cubre el collider y llevar su base al centro del collider.
                var straighten = Quaternion.FromToRotation(top - bottom, Vector3.up);
                foreach (var go in new[] { green, dead })
                {
                    if (go == null) continue;
                    go.transform.rotation = straighten * go.transform.rotation;
                    go.transform.position = bottom + straighten * (go.transform.position - bottom) + new Vector3(target.x - bottom.x, 0, target.z - bottom.z);
                }
            }
            if (!log) return;
            BarkCentroid(green, y0, y0 + 0.5f, out var b2, out float rb);
            BarkCentroid(green, y1 - 0.5f, y1, out var t2, out float rt);
            Debug.Log($"[V6] Tronco {trunk.transform.parent?.name}: base a {Horizontal(b2 - target):F3} m, " +
                      $"arriba a {Horizontal(t2 - target):F3} m del collider; radio corteza {rb:F2}/{rt:F2} m (collider {cb.extents.x:F2} m)");
        }

        /// <summary>Vértices de madera (corteza y ramas) entre 0,1 y 1,9 m, dentro de la orilla transitable y fuera del collider del tronco.</summary>
        private static int BankIntrusion(GameObject tree, CapsuleCollider trunk)
        {
            var c = trunk.bounds.center; float clear = trunk.bounds.extents.x + 0.3f; int n = 0;
            foreach (var mf in tree.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (r == null || mf.sharedMesh == null || mf.name.Contains("LOD1") || r.sharedMaterials.Length == 0) continue;
                if (r.sharedMaterials[0] == null || !r.sharedMaterials[0].name.Contains("Bark")) continue;
                var m = mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var w = m.MultiplyPoint3x4(v);
                    if (w.y < 0.1f || w.y > 1.9f || w.x < -0.95f || w.x > 5.95f) continue;
                    if (new Vector2(w.x - c.x, w.z - c.z).magnitude > clear) n++;
                }
            }
            return n;
        }

        private static float Horizontal(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>Centro (xz) y radio medio de la corteza del tronco entre dos alturas del mundo.</summary>
        private static bool BarkCentroid(GameObject tree, float yMin, float yMax, out Vector3 center, out float radius)
        {
            center = Vector3.zero; radius = 0;
            var pts = new List<Vector3>();
            foreach (var mf in tree.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (r == null || mf.sharedMesh == null || r.sharedMaterials.Length == 0) continue;
                var mat = r.sharedMaterials[0];
                if (mat == null || !mat.name.Contains("Bark")) continue;
                var mesh = mf.sharedMesh; var verts = mesh.vertices;
                var used = new HashSet<int>(mesh.GetTriangles(0));
                var m = mf.transform.localToWorldMatrix;
                foreach (int i in used)
                {
                    var w = m.MultiplyPoint3x4(verts[i]);
                    if (w.y >= yMin && w.y <= yMax) pts.Add(w);
                }
            }
            if (pts.Count < 6) return false;
            // Mediana en xz: ignora el arranque de ramas que comparten la submalla de corteza.
            float mx = Median(pts.Select(p => p.x)), mz = Median(pts.Select(p => p.z));
            var near = pts.Where(p => new Vector2(p.x - mx, p.z - mz).magnitude < 0.6f).ToList();
            if (near.Count < 6) near = pts;
            center = new Vector3(near.Average(p => p.x), (yMin + yMax) * 0.5f, near.Average(p => p.z));
            var c = center;
            radius = Median(near.Select(p => new Vector2(p.x - c.x, p.z - c.z).magnitude));
            return true;
        }

        private static float Median(IEnumerable<float> values)
        {
            var a = values.OrderBy(v => v).ToArray();
            return a.Length == 0 ? 0 : a[a.Length / 2];
        }

        /// <summary>QA6-013: el tronco opcional del sendero conserva su collider; necesita un visual que lo ocupe.</summary>
        private static void OptionalLog(Transform root)
        {
            var src = Find("LowLog_Optional");
            var cap = src ? src.GetComponent<CapsuleCollider>() : null;
            if (cap == null) { Debug.LogWarning("[V6] Sin LowLog_Optional"); return; }
            var gs = root.Find("V6_Static") ?? Group(root, "V6_Static");
            var log = Put("VIS_V6_DeadLog_A", gs, Vector3.zero, Vector3.zero, 1);
            log.name = Tag + "_LowLog_Optional";
            // Eje largo del collider en el mundo (dirección de la cápsula) y tamaño del modelo sin escalar.
            var axisLocal = cap.direction == 0 ? Vector3.right : cap.direction == 1 ? Vector3.up : Vector3.forward;
            var axis = src.TransformDirection(axisLocal).normalized;
            float length = cap.height * Mathf.Abs(Vector3.Dot(src.lossyScale, axisLocal));
            float radius = cap.bounds.extents.y; // tronco tumbado: el radio es la media altura de su caja
            var mb = MeshBounds(log);
            var yaw = Quaternion.FromToRotation(Vector3.right, new Vector3(axis.x, 0, axis.z).normalized);
            log.transform.rotation = yaw;
            // Algo más grueso que el collider para que el jugador choque con la madera, no con el aire.
            float thick = radius * 2f * 1.15f;
            log.transform.localScale = new Vector3(length / mb.size.x, thick / mb.size.y, thick / mb.size.z);
            var b = MeshBounds(log, true);
            log.transform.position += cap.bounds.center - b.center;
            Debug.Log($"[V6] Tronco opcional: visual {b.size.ToString("F2")} sobre collider {cap.bounds.size.ToString("F2")}");
        }

        /// <summary>Caja de los renderers; con world=false, la del modelo en rotación/escala identidad.</summary>
        private static Bounds MeshBounds(GameObject go, bool world = false)
        {
            var rot = go.transform.rotation; var sc = go.transform.localScale; var pos = go.transform.position;
            if (!world) { go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); go.transform.localScale = Vector3.one; }
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (!world) { go.transform.SetPositionAndRotation(pos, rot); go.transform.localScale = sc; }
            return b;
        }

        // ------------------------------------------------------------------ rocas
        private static readonly string[] MossRocks = { "VIS_V6_RockMoss_A", "VIS_V6_RockMoss_B", "VIS_V6_RockMoss_C", "VIS_V6_RockMoss_D", "VIS_V6_RockMoss_E", "VIS_V6_RockMoss_F", "VIS_V6_RockMoss_G" };

        private static bool NearRampOrObstacle(float z, float margin) =>
            Mathf.Abs(z - 5f) < 1.6f + margin || Mathf.Abs(z - 31.5f) < 1.6f + margin ||
            Mathf.Abs(z - 10f) < 1f + margin || Mathf.Abs(z - 18f) < 1.2f + margin || Mathf.Abs(z - 26f) < 1.1f + margin;

        private static readonly Vector3[] StreamStones =
            { new Vector3(-2.2f, -0.62f, 3f), new Vector3(-3.0f, -0.62f, 13.5f), new Vector3(-2.0f, -0.62f, 21.8f), new Vector3(-2.9f, -0.62f, 29f), new Vector3(-2.3f, -0.62f, 36f) };

        private static void Rocks(Transform deg, Transform res)
        {
            var rd = Group(deg, "V6_Rocks"); var rr = Group(res, "V6_Rocks");
            // Rocas de la orilla del cauce (lado orilla x≈-1 y lado muro x≈-4)
            for (float z = -3.4f; z <= 39.5f; z += Rand(1.4f, 2.3f))
            {
                foreach (float edge in new[] { -1.05f, -3.95f })
                {
                    if (NearRampOrObstacle(z, edge < -2f ? -0.6f : 0f) && edge > -2f) continue;
                    float s = Rand(0.35f, 0.7f); float yaw = Rand(0, 360);
                    var pos = new Vector3(edge + Rand(-0.15f, 0.15f), -0.68f, z);
                    Put(Pick(MossRocks), rr, pos, new Vector3(Rand(-8, 8), yaw, Rand(-8, 8)), s);
                    Put(_rng.NextDouble() < 0.5 ? "VIS_V6_BoulderDry_A" : "VIS_V6_BoulderDry_B", rd, pos, new Vector3(0, yaw, 0), s * 0.55f);
                }
            }
            // Piedras dentro de la corriente (con salpicaduras en restaurado)
            foreach (var p in StreamStones)
                Put(Pick(MossRocks), rr, p, new Vector3(0, Rand(0, 360), 0), Rand(0.32f, 0.42f));
            // Rocas grandes en lo alto de los muros: rompen la silueta de caja
            for (float z = -4f; z <= 40.5f; z += Rand(3.2f, 4.6f))
            {
                string id = "ABCDEF"[_rng.Next(6)].ToString();
                var pos = new Vector3(Rand(-4.9f, -4.3f), 2.6f, z);
                var e = new Vector3(Rand(-6, 6), Rand(0, 360), Rand(-6, 6));
                float s = Rand(0.75f, 1.1f);
                Put($"VIS_V6_Cliff_{id}_Restored", rr, pos, e, s);
                Put($"VIS_V6_Cliff_{id}_Degraded", rd, pos, e, s);
            }
            foreach (float x in new[] { -3.5f, -0.5f, 2.5f, 5f })
                foreach (float z in new[] { -5.2f, 41.2f })
                {
                    string id = "ABCDEF"[_rng.Next(6)].ToString();
                    var pos = new Vector3(x + Rand(-0.5f, 0.5f), 2.6f, z);
                    var e = new Vector3(0, Rand(0, 360), 0); float s = Rand(0.8f, 1.1f);
                    Put($"VIS_V6_Cliff_{id}_Restored", rr, pos, e, s);
                    Put($"VIS_V6_Cliff_{id}_Degraded", rd, pos, e, s);
                }
            // Losas de roca adosadas a los muros: convierten las caras planas de caja en acantilado.
            float[] depth = { 1.47f, 1.26f, 1.10f, 1.77f, 1.21f, 1.26f };
            for (float z = -3.2f; z <= 39.6f; z += Rand(2.6f, 3.3f))
            {
                int k = _rng.Next(6); float sc = Rand(0.95f, 1.3f);
                var q = Quaternion.AngleAxis(Rand(0, 360), Vector3.right) * Quaternion.Euler(0, 0, -90);
                var pos = new Vector3(-4.0f - depth[k] * sc + Rand(0.2f, 0.38f), Rand(0.7f, 1.6f), z);
                PutQ($"VIS_V6_Cliff_{"ABCDEF"[k]}_Restored", rr, pos, q, sc);
                PutQ($"VIS_V6_Cliff_{"ABCDEF"[k]}_Degraded", rd, pos, q, sc);
            }
            foreach (var (zFace, dir) in new[] { (-4.0f, 1f), (40.0f, -1f) })
                for (float x = -3.6f; x <= -1.4f; x += Rand(1.6f, 2.0f))
                {
                    int k = _rng.Next(6); float sc = Rand(0.95f, 1.25f);
                    var q = Quaternion.AngleAxis(Rand(0, 360), Vector3.forward) * Quaternion.Euler(dir > 0 ? 90 : -90, 0, 0);
                    var pos = new Vector3(x, Rand(0.6f, 1.5f), zFace - dir * (depth[k] * sc - Rand(0.2f, 0.35f)));
                    PutQ($"VIS_V6_Cliff_{"ABCDEF"[k]}_Restored", rr, pos, q, sc);
                    PutQ($"VIS_V6_Cliff_{"ABCDEF"[k]}_Degraded", rd, pos, q, sc);
                }
            // Raíces y troncos decorativos al pie del muro
            foreach (float z in new[] { 1f, 14f, 22f, 34f })
            {
                Put(_rng.NextDouble() < 0.5 ? "VIS_V6_PineRoots_A" : "VIS_V6_PineRoots_B", rr, new Vector3(-4.0f, -0.6f, z), new Vector3(0, 90 + Rand(-20, 20), -60), 1.1f);
            }
        }

        // ------------------------------------------------------------------ vegetación
        private static float BankHeight(float x) => x > -0.95f && x < 5.95f ? 0f : (x < -4.05f && x > -5.45f ? 3f : float.NaN);

        private static string PrefabRootName(Transform t)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
            return root != null ? root.name : t.name;
        }

        private static void Scatter(Transform parent, string[] prefabs, int count, Func<Vector3> sample, float minS, float maxS, bool alignRandom = true)
        {
            for (int i = 0; i < count; i++)
            {
                var p = sample();
                if (float.IsNaN(p.y)) continue;
                Put(Pick(prefabs), parent, p, new Vector3(0, alignRandom ? Rand(0, 360) : 0, 0), Rand(minS, maxS));
            }
        }

        private static Vector3 BankPoint(float xMin, float xMax, float zMin = -3.6f, float zMax = 39.6f, bool avoidExit = true)
        {
            for (int k = 0; k < 20; k++)
            {
                float x = Rand(xMin, xMax), z = Rand(zMin, zMax);
                if (avoidExit && x > 1f && x < 5f && z > 34.2f && z < 37.8f) continue;
                if (Mathf.Abs(z - 35f) < 0.4f) continue; // barrera
                if (x < -0.5f && (Mathf.Abs(z - 5f) < 1.4f || Mathf.Abs(z - 31.5f) < 1.4f)) continue; // rampas
                if ((new Vector2(x, z) - new Vector2(1.75f, 30f)).magnitude < 1.1f) continue; // tronco opcional
                return new Vector3(x, BankHeight(x), z);
            }
            return new Vector3(0, float.NaN, 0);
        }

        private static void Vegetation(Transform deg, Transform res)
        {
            var vd = Group(deg, "V6_Vegetation"); var vr = Group(res, "V6_Vegetation");
            string[] grass = { "VIS_V6_GrassA_LargeA", "VIS_V6_GrassA_LargeB", "VIS_V6_GrassA_LargeC", "VIS_V6_GrassA_MidA", "VIS_V6_GrassA_MidB",
                               "VIS_V6_GrassA_TallA", "VIS_V6_GrassA_TallB", "VIS_V6_GrassA_TallC", "VIS_V6_GrassB_C", "VIS_V6_GrassB_D", "VIS_V6_GrassB_E" };
            string[] dry = grass.Select(g => g + "_Dry").ToArray();
            string[] flowers = { "VIS_V6_FlowerYellow_A", "VIS_V6_FlowerYellow_B", "VIS_V6_FlowerYellow_C", "VIS_V6_FlowerYellow_D", "VIS_V6_FlowerYellow_E",
                                 "VIS_V6_Dandelion_A", "VIS_V6_Dandelion_B", "VIS_V6_Dandelion_C" };
            string[] ferns = { "VIS_V6_Fern_A", "VIS_V6_Fern_B", "VIS_V6_Fern_C", "VIS_V6_Fern_D" };
            string[] shrubs = { "VIS_V6_Shrub_A", "VIS_V6_Shrub_B", "VIS_V6_Shrub_C", "VIS_V6_Shrub_D" };

            // Restaurado: orilla del río densa, centro del sendero más abierto
            Scatter(vr, grass, 210, () => BankPoint(-0.95f, 1.0f), 2.4f, 3.8f);
            Scatter(vr, grass, 140, () => BankPoint(4.0f, 5.95f), 2.4f, 3.8f);
            Scatter(vr, grass, 100, () => BankPoint(1.0f, 4.0f), 1.8f, 2.8f);
            Scatter(vr, grass, 90, () => BankPoint(-5.45f, -4.05f, -4f, 40f, false), 2.6f, 4f);
            Scatter(vr, ferns, 34, () => BankPoint(-0.95f, -0.2f), 1.3f, 2.0f);
            Scatter(vr, ferns, 22, () => BankPoint(5.0f, 5.95f), 1.4f, 2.1f);
            Scatter(vr, ferns, 18, () => BankPoint(-5.45f, -4.05f, -4f, 40f, false), 1.6f, 2.4f);
            Scatter(vr, flowers, 70, () => BankPoint(-0.6f, 5.8f), 1.5f, 2.4f);
            Scatter(vr, shrubs, 34, () => BankPoint(4.6f, 5.95f), 3.5f, 5.5f);
            Scatter(vr, shrubs, 20, () => BankPoint(-5.45f, -4.05f, -4f, 40f, false), 4f, 6f);
            // Junquillos y helechos en el borde húmedo del cauce
            for (float z = -3.4f; z <= 39.5f; z += Rand(0.8f, 1.5f))
            {
                if (NearRampOrObstacle(z, 0f)) continue;
                Put(Pick(grass.Where(g => g.Contains("Tall")).ToArray()), vr, new Vector3(Rand(-1.3f, -1.05f), -0.6f, z), new Vector3(0, Rand(0, 360), 0), Rand(2.2f, 3.2f));
            }

            // Degradado: hierba seca rala, ramas sueltas y suelo desnudo
            Scatter(vd, dry, 120, () => BankPoint(-0.95f, 1.2f), 1.4f, 2.6f);
            Scatter(vd, dry, 80, () => BankPoint(4.2f, 5.95f), 1.4f, 2.6f);
            Scatter(vd, dry, 50, () => BankPoint(-5.45f, -4.05f, -4f, 40f, false), 1.3f, 2.2f);
            for (int i = 0; i < 16; i++)
            {
                var p = BankPoint(-0.9f, 5.9f);
                if (float.IsNaN(p.y)) continue;
                Put(Pick(new[] { "VIS_V6_DryBranch_A", "VIS_V6_DryBranch_B", "VIS_V6_DryBranch_C" }), vd, p, new Vector3(0, Rand(0, 360), 0), Rand(0.8f, 1.4f));
            }
            // Rendimiento: la vegetación baja (hierba, flores, helechos) no proyecta sombra; arbustos y árboles sí.
            foreach (var r in vd.GetComponentsInChildren<Renderer>(true).Concat(vr.GetComponentsInChildren<Renderer>(true)))
            {
                var n = r.transform.root != null ? PrefabRootName(r.transform) : r.name;
                if (n.Contains("Grass") || n.Contains("Flower") || n.Contains("Dandelion") || n.Contains("Fern"))
                    r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // ------------------------------------------------------------------ fondo de valle
        private static void Backdrop(Transform deg, Transform res)
        {
            var bd = Group(deg, "V6_Backdrop"); var br = Group(res, "V6_Backdrop");
            // Laderas lejanas al este (más allá del borde de caída) con rocas gigantes triplanares
            for (int i = 0; i < 9; i++)
            {
                string id = "ABCDEF"[_rng.Next(6)].ToString();
                var pos = new Vector3(Rand(18f, 34f), Rand(-14f, -9f), -10f + i * 7f + Rand(-2, 2));
                var e = new Vector3(Rand(-10, 10), Rand(0, 360), Rand(-10, 10));
                float s = Rand(5f, 8f);
                Put($"VIS_V6_Cliff_{id}_Restored", br, pos, e, s);
                Put($"VIS_V6_Cliff_{id}_Degraded", bd, pos, e, s);
            }
            // Fondo del valle: árboles a escala sobre las laderas
            for (int i = 0; i < 26; i++)
            {
                var p = new Vector3(Rand(14f, 36f), -11.5f, Rand(-12f, 50f));
                string kind = _rng.NextDouble() < 0.5 ? "A" : "B"; float s = Rand(2.2f, 3.2f); float yaw = Rand(0, 360);
                bool dead = _rng.NextDouble() < 0.5;
                // Rendimiento (integración V6): las copas del fondo eran ~18 % del coste; se conserva uno de cada dos.
                if (i % 2 == 1) continue;
                Put($"VIS_V6_Tree{kind}_Green", br, p, new Vector3(0, yaw, 0), s * 1.1f);
                if (dead) Put($"VIS_V6_Tree{kind}_Dead", bd, p, new Vector3(0, yaw, 0), s * 1.1f);
            }
            // Terreno alrededor del nivel (sin colliders): meseta oeste/norte/sur a la altura de los muros,
            // acantilado bajo el borde este y fondo del valle. Evita que el nivel parezca una isla flotante.
            var blocks = new (string name, Vector3 c, Vector3 size, string d, string r)[]
            {
                ("V6_PlateauWest", new Vector3(-30f, -4.5f, 18f), new Vector3(49f, 15f, 170f), "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("V6_PlateauSouth", new Vector3(0.75f, -4.5f, -45f), new Vector3(12.5f, 15f, 79f), "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("V6_PlateauNorth", new Vector3(0.75f, -4.5f, 81f), new Vector3(12.5f, 15f, 79f), "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("V6_EastCliff", new Vector3(5f, -6.5f, 18f), new Vector3(2f, 11f, 47f), "M_Ground_Cliff_Degraded", "M_Ground_Cliff_Restored"),
                ("V6_ValleyFloor", new Vector3(45f, -12.5f, 18f), new Vector3(90f, 1f, 240f), "M_Ground_Valley_Degraded", "M_Ground_Valley_Restored"),
            };
            foreach (var (name, c, size, d, r) in blocks)
                foreach (var (parent, mat) in new[] { (bd, d), (br, r) })
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(g.GetComponent<Collider>());
                    g.name = name; g.transform.SetParent(parent, false);
                    g.transform.position = c; g.transform.localScale = size;
                    g.GetComponent<MeshRenderer>().sharedMaterial = B.M(mat);
                }
            // Bosque en la meseta oeste
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector3(Rand(-26f, -7f), 3f, Rand(-14f, 52f));
                string kind = _rng.NextDouble() < 0.5 ? "A" : "B"; float s = Rand(1.6f, 2.4f); float yaw = Rand(0, 360);
                double r1 = _rng.NextDouble(), r2 = _rng.NextDouble(); float stumpScale = Rand(0.7f, 1f);
                if (i % 3 == 2) continue; // rendimiento: dos de cada tres árboles de la meseta
                Put($"VIS_V6_Tree{kind}_Green", br, p, new Vector3(0, yaw, 0), s);
                if (r1 < 0.45) Put($"VIS_V6_Tree{kind}_Dead", bd, p, new Vector3(0, yaw, 0), s);
                else if (r2 < 0.5) Put("VIS_V6_Stump_A", bd, p, new Vector3(0, yaw, 0), stumpScale);
            }
        }

        // ------------------------------------------------------------------ partículas
        private static GameObject Fx(string name, Transform parent, Vector3 pos, Vector3? euler = null)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler ?? Vector3.zero));
            return go;
        }

        private static void Particles(Transform deg, Transform res)
        {
            var fd = Group(deg, "V6_FX"); var fr = Group(res, "V6_FX");
            Fx("PFX_V6_DustMotes", fd, new Vector3(1f, 1.3f, 18f));
            Fx("PFX_V6_DustGusts", fd, new Vector3(2.5f, 0.15f, 18f));
            Fx("PFX_V6_FallingLeaves_Dry", fd, new Vector3(0f, 7f, 18f));

            Fx("PFX_V6_StreamMist", fr, new Vector3(-2.5f, -0.15f, 18f));
            Fx("PFX_V6_WaterRipples", fr, new Vector3(-2.5f, -0.2f, 18f));
            Fx("PFX_V6_Pollen", fr, new Vector3(1f, 1.6f, 18f));
            Fx("PFX_V6_Butterflies", fr, new Vector3(2f, 0.9f, 18f));
            Fx("PFX_V6_FallingLeaves", fr, new Vector3(0f, 7f, 18f));
            Fx("PFX_V6_ExitGlow", fr, new Vector3(3f, 1.3f, 38.5f));
            foreach (var st in StreamStones)
                Fx("PFX_V6_WaterSplash", fr, new Vector3(st.x, -0.2f, st.z + 0.35f));
            Fx("PFX_V6_WaterSplash", fr, new Vector3(-2.5f, -0.2f, -3.6f)); // manantial al pie del muro sur
        }

        // ------------------------------------------------------------------ ambiente
        /// <summary>
        /// QA6-009: un solo efecto de restauración. El RestorationFeedback de gameplay (que cuenta las presentaciones
        /// y se limpia al reiniciar) pasa a reproducir una versión local de la ola V6; el sistema antiguo deja de verse
        /// y el ambiente V6 ya no lanza su propia ola.
        /// </summary>
        private static void SingleRestorationEffect(Transform root, BosqueV6Ambience ambience)
        {
            var feedback = AllTransforms().SelectMany(t => t.GetComponents<MonoBehaviour>())
                .FirstOrDefault(m => m != null && m.GetType().Name == "RestorationFeedback");
            if (feedback == null) return; // escena sin gameplay: se queda la ola del ambiente
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/PFX_V6_RestorationWave.prefab");
            var fx = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            fx.name = Tag + "_RestorationFeedbackFX";
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main; main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None;
                var shape = ps.shape; var sc = shape.scale; shape.scale = new Vector3(Mathf.Min(sc.x, 7f), sc.y, Mathf.Min(sc.z, 7f));
            }
            var so = new SerializedObject(feedback);
            var old = so.FindProperty("particles").objectReferenceValue as ParticleSystem;
            if (old != null && old.gameObject != fx)
            {
                var oldMain = old.main; oldMain.playOnAwake = false;
                foreach (var r in old.GetComponentsInChildren<ParticleSystemRenderer>(true)) r.enabled = false;
            }
            so.FindProperty("particles").objectReferenceValue = fx.GetComponent<ParticleSystem>();
            so.ApplyModifiedPropertiesWithoutUndo();
            ambience.restorationBurst = null;
            Debug.Log($"[V6] Efecto de restauración único: {feedback.name} reproduce {fx.name}; sistema anterior oculto ({(old != null ? old.name : "ninguno")}).");
        }

        private static BosqueV6Ambience Ambience(Transform root, GameObject restored)
        {
            var amb = new GameObject("V6_Ambience").transform; amb.SetParent(root, false);
            Volume Vol(string name, string profile, float weight)
            {
                var go = new GameObject(name); go.transform.SetParent(amb, false);
                var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 10; v.weight = weight;
                v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{B.Root}/PostFX/{profile}.asset");
                return v;
            }
            amb.gameObject.AddComponent<BosqueV6PerfProbe>(); // solo actúa con -v6-perf
            var a = amb.gameObject.AddComponent<BosqueV6Ambience>();
            a.restoredVisuals = restored;
            a.degradedVolume = Vol("V6_PostFX_Degraded", "VP_V6_Degraded", 1);
            a.restoredVolume = Vol("V6_PostFX_Restored", "VP_V6_Restored", 0);
            a.skyBlend = B.M("M_Sky_Blend");
            a.sun = Find("Sun")?.GetComponent<Light>();
            a.restorationBurst = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/PFX_V6_RestorationWave.prefab");
            a.burstPosition = new Vector3(1f, -0.1f, 18f);
            // Reflejos: se usa el reflejo de entorno del cielo (DynamicGI.UpdateEnvironment en BosqueV6Ambience).
            a.skyProbe = null;
            if (a.sun != null)
            {
                a.sun.shadows = LightShadows.Soft;
                a.sun.transform.rotation = Quaternion.Euler(42f, 300f, 0f);
            }
            var cam = Find("GameplayCamera")?.GetComponent<Camera>();
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 250f);
            }
            a.ApplyImmediate(restored.activeInHierarchy);
            return a;
        }
    }
}
