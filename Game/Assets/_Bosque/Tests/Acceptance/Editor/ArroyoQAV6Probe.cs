using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.Acceptance
{
    /// <summary>
    /// Sonda de QA del arte V6 (Entregable 06 de QA). Solo lectura: abre escenas sin guardarlas.
    /// Compara la escena de muestra V6 con la de integración y conduce el gameplay real por API.
    /// Los colliders temporales que usa para medir intrusiones de cámara/jugador se crean en memoria y
    /// se destruyen; la escena nunca se guarda. No sustituye pruebas con teclado/ratón ni juicio humano.
    /// Ejecutar: Unity -batchmode -projectPath Game -executeMethod Bosque.Acceptance.ArroyoQAV6Probe.RunBatch
    /// </summary>
    public static class ArroyoQAV6Probe
    {
        private const string Integration = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";
        private const string Greybox = "Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity";
        private const string Showcase = "Assets/_Bosque/Art/V6/Showcase/ArroyoV6_Showcase.unity";
        private const string ArtRoot = "Assets/_Bosque/Art/V6";
        private const int ProbeLayer = 31;

        private const string BaselineFile = "../../Docs/QA/Entregable06/EVIDENCIAS/contrato-arroyoplayable-pre-v6.json";

        /// <summary>Contrato de ArroyoPlayable antes de vestirla con V6 (colliders y componentes de gameplay).</summary>
        [Serializable] private sealed class Baseline
        {
            public string escena, sha, fecha_utc;
            public List<string> colliders = new List<string>();
            public List<string> componentes = new List<string>();
        }

        private sealed class Result { public string id, area, status, detail; }
        private static bool _integrated;
        private static readonly List<Result> Results = new List<Result>();
        private static readonly Dictionary<string, object> Metrics = new Dictionary<string, object>();

        private static void Rec(string id, string area, string status, string detail)
        {
            Results.Add(new Result { id = id, area = area, status = status, detail = detail });
            Debug.Log($"[QA6] {id} {status} | {area} | {detail}");
        }
        private static void Check(string id, string area, bool ok, string detail) => Rec(id, area, ok ? "OK" : "FALLA", detail);

        /// <summary>Guarda la línea base del contrato a partir de ArroyoPlayable sin vestir. Solo antes de integrar V6.</summary>
        public static void ExportBaselineBatch()
        {
            int code = 0;
            try
            {
                var scene = EditorSceneManager.OpenScene(Integration, OpenSceneMode.Single);
                if (scene.GetRootGameObjects().Any(g => g.name == "ArtV6")) throw new Exception("ArroyoPlayable ya está vestida con V6: la línea base debe salir de la escena sin vestir.");
                var b = new Baseline { escena = Integration, sha = Sha(Integration), fecha_utc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") };
                foreach (var kv in Colliders(scene).OrderBy(k => k.Key)) b.colliders.Add(kv.Key + "=" + kv.Value);
                b.componentes.AddRange(Behaviours(scene).OrderBy(x => x));
                File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, BaselineFile)), JsonUtility.ToJson(b, true), new UTF8Encoding(false));
                Debug.Log($"[QA6] Línea base: {b.colliders.Count} colliders, {b.componentes.Count} componentes, sha {b.sha}");
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static Baseline LoadBaseline()
        {
            string f = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, BaselineFile));
            if (!File.Exists(f)) throw new FileNotFoundException("Falta la línea base del contrato: " + f);
            return JsonUtility.FromJson<Baseline>(File.ReadAllText(f));
        }

        public static void RunBatch()
        {
            int code = 0;
            try
            {
                Run(Array.IndexOf(Environment.GetCommandLineArgs(), "-qa6-integrada") >= 0);
                if (Results.Any(r => r.status == "FALLA")) code = 2;
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        [MenuItem("Tools/Bosque/QA/V6 - Ejecutar sonda de QA del arte V6")]
        public static void RunShowcase() => Run(false);

        [MenuItem("Tools/Bosque/QA/V6 - Ejecutar sonda sobre ArroyoPlayable integrada")]
        public static void RunIntegrated() => Run(true);

        /// <param name="integrated">false: escena de muestra V6; true: ArroyoPlayable ya vestida (integración).</param>
        public static void Run(bool integrated)
        {
            Results.Clear(); Metrics.Clear(); _integrated = integrated;
            string target = integrated ? Integration : Showcase;
            Metrics["escena_probada"] = target;
            ShaderUtil.allowAsyncCompilation = false;

            // ---------------------------------------------------------------- Integridad de archivos
            Metrics["sha_ArroyoPlayable"] = Sha(Integration);
            Metrics["sha_ArroyoGreybox"] = Sha(Greybox);
            var baseline = LoadBaseline();
            Metrics["sha_linea_base"] = baseline.sha;
            var baseColliders = baseline.colliders.ToDictionary(s => s.Substring(0, s.IndexOf('=')), s => s.Substring(s.IndexOf('=') + 1));
            var baseBehaviours = new HashSet<string>(baseline.componentes);
            Check("C10", "Contrato", (string)Metrics["sha_ArroyoGreybox"] == "617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2",
                  "ArroyoGreybox con hash histórico: " + Metrics["sha_ArroyoGreybox"]);
            AssetChecks();

            // ---------------------------------------------------------------- Contrato de escena
            // C09: ArroyoPlayable (vestida o no) conserva exactamente el contrato de la línea base pre-V6.
            var integrationScene = EditorSceneManager.OpenScene(Integration, OpenSceneMode.Single);
            var liveColliders = Colliders(integrationScene); var liveBehaviours = Behaviours(integrationScene);
            int liveDiff = baseColliders.Count(kv => !liveColliders.TryGetValue(kv.Key, out var v) || v != kv.Value) +
                           liveColliders.Keys.Count(k => !baseColliders.ContainsKey(k)) + baseBehaviours.Count(b => !liveBehaviours.Contains(b));
            bool dressed = integrationScene.GetRootGameObjects().Any(g => g.name == "ArtV6");
            Check("C09", "Contrato", liveDiff == 0,
                  $"ArroyoPlayable ({(dressed ? "vestida con V6" : "sin vestir")}, sha {Metrics["sha_ArroyoPlayable"]}) frente a la línea base pre-V6 (sha {baseline.sha}): {liveDiff} diferencias de colliders/componentes");
            var showcase = EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
            var v6Colliders = Colliders(showcase);
            var v6Behaviours = Behaviours(showcase);

            var missing = baseColliders.Keys.Where(k => !v6Colliders.ContainsKey(k)).ToList();
            var changed = baseColliders.Keys.Where(k => v6Colliders.ContainsKey(k) && v6Colliders[k] != baseColliders[k]).ToList();
            var added = v6Colliders.Keys.Where(k => !baseColliders.ContainsKey(k)).ToList();
            Check("C02", "Contrato", missing.Count == 0 && changed.Count == 0,
                  $"{baseColliders.Count} colliders de integración; ausentes {missing.Count}, cambiados {changed.Count} {string.Join("; ", changed.Concat(missing).Take(5))}");
            Check("C03", "Contrato", added.Count == 0, $"Colliders añadidos por V6: {added.Count} {string.Join("; ", added.Take(5))}");
            var lostBehaviours = baseBehaviours.Where(b => !v6Behaviours.Contains(b)).ToList();
            Check("C04", "Contrato", lostBehaviours.Count == 0,
                  $"{baseBehaviours.Count} componentes de gameplay/escena conservados; perdidos {lostBehaviours.Count} {string.Join("; ", lostBehaviours.Take(5))}");

            var all = showcase.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            Transform F(string n) => all.FirstOrDefault(t => t.name == n);
            foreach (var marker in new[] { "PlayerSpawn", "SafeRespawn", "ExitMarker", "Obstacle_01", "Obstacle_02", "Obstacle_03", "DegradedVisuals", "RestoredVisuals" })
                Check("C05." + marker, "Contrato", all.Count(t => t.name == marker) == 1, $"Marcador único '{marker}'");
            var ids = UnityEngine.Object.FindObjectsByType<RemovableObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(o => o.ObstacleId).OrderBy(x => x).ToArray();
            Check("C06", "Contrato", ids.SequenceEqual(new[] { "arroyo.branches", "arroyo.log", "arroyo.rocks" }), "IDs: " + string.Join(",", ids));
            Check("C07", "Contrato", F("DegradedVisuals").gameObject.activeSelf && !F("RestoredVisuals").gameObject.activeSelf,
                  "Estado inicial: DegradedVisuals activo, RestoredVisuals inactivo");
            var v6Roots = all.Where(t => t.name.StartsWith("V6_") || t.name == "ArtV6").ToList();
            int v6ColliderCount = v6Roots.Sum(t => t.GetComponentsInChildren<Collider>(true).Length);
            Check("C08", "Contrato", v6ColliderCount == 0, $"Colliders bajo raíces V6: {v6ColliderCount}");
            int missingScripts = all.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            Check("C11", "Contrato", missingScripts == 0, $"Scripts perdidos en la escena V6: {missingScripts}");

            // C12: todo collider sólido de juego debe tener geometría visible que lo represente (sin paredes invisibles).
            // Solo cuentan renderers de tamaño comparable (una copa de 8 m no "representa" un tronco de 35 cm).
            var visibleRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && !Path(r.transform).Contains("V6_Backdrop")).ToList();
            var invisible = new List<string>();
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!c.enabled || c.isTrigger || c is CharacterController || !c.gameObject.activeInHierarchy) continue;
                var cb = c.bounds; cb.Expand(-0.02f);
                float cVol = Mathf.Max(1e-4f, cb.size.x * cb.size.y * cb.size.z);
                var visibleBounds = visibleRenderers.Select(r => r.bounds)
                    .Where(vb => vb.Intersects(cb) && vb.size.x * vb.size.y * vb.size.z < Mathf.Max(cVol * 60f, 2f)).ToList();
                // Volumen del collider cubierto por renderers activos (muestreo de 5×5×5 puntos).
                int inside = 0, total = 0;
                for (int ix = 0; ix < 5; ix++) for (int iy = 0; iy < 5; iy++) for (int iz = 0; iz < 5; iz++)
                {
                    total++;
                    var pnt = cb.min + Vector3.Scale(cb.size, new Vector3(ix / 4f, iy / 4f, iz / 4f));
                    if (visibleBounds.Any(vb => vb.Contains(pnt))) inside++;
                }
                if (inside >= total * 0.5f) continue;
                // Segunda opinión con la malla real: puntos del collider dentro o a <=15 cm de una superficie visible.
                int meshCover = MeshCoverage(c, cb, visibleRenderers);
                if (meshCover < 40) invisible.Add($"{Path(c.transform)} (cajas {100 * inside / total}%, malla {meshCover}%)");
            }
            Check("C12", "Contrato", invisible.Count == 0, $"Colliders sólidos sin visual que los represente: {invisible.Count} {string.Join("; ", invisible)}");

            // ---------------------------------------------------------------- Gameplay por API
            var session = UnityEngine.Object.FindFirstObjectByType<ArroyoSession>();
            session.InitializeFromScene();
            var player = session.Player;
            var actor = player.GetComponent<PlayerInteractor>();
            var restored = F("RestoredVisuals").gameObject;
            var degraded = F("DegradedVisuals").gameObject;

            // F06: recorrido de la orilla sin bloqueos (degradado)
            float reached = Walk(player, new Vector3(3, 0, 0), Vector2.up, 600);
            Check("F06a", "Gameplay", reached >= 33f && reached < 35f, $"Degradado: caminando por X=3 alcanza Z={reached:F2} y la barrera detiene antes de Z=35");
            // F01: barrera bloquea al saltar antes de restaurar
            player.Teleport(new Vector3(3, 0, 33), Quaternion.identity); Physics.SyncTransforms();
            for (int i = 0; i < 60; i++) player.Simulate(Vector2.up, i == 1, 0, .02f);
            Check("F01", "Gameplay", player.transform.position.z < 35f && !session.Progress.IsCompleted,
                  $"Salto contra la barrera: Z final {player.transform.position.z:F2}, completado={session.Progress.IsCompleted}");
            // F10: caída por el borde este con el acantilado V6 (sin collider) sigue devolviendo al SafeRespawn
            player.Teleport(new Vector3(5.6f, 0, 20), Quaternion.identity); Physics.SyncTransforms();
            float minY = 0f;
            for (int i = 0; i < 200; i++) { player.Simulate(Vector2.right, false, 0, .02f); minY = Mathf.Min(minY, player.transform.position.y); if (player.transform.position.z < 5f) break; }
            var safe = F("SafeRespawn").position;
            Check("F10", "Gameplay", (player.transform.position - safe).magnitude < 1.5f,
                  $"Caída por el borde este: Y mínima {minY:F2}, posición final {player.transform.position.ToString("F2")}, SafeRespawn {safe.ToString("F2")}");

            Intrusions("degradado", all, player);

            // F02/F03: retirar obstáculos desde la orilla con los visuales V6 presentes
            bool allReach = true; var detail = new StringBuilder();
            foreach (int z in new[] { 10, 18, 26 })
            {
                player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); Physics.SyncTransforms(); actor.Scan();
                bool reach = actor.Target != null;
                bool first = reach && actor.TryInteract();
                bool second = actor.TryInteract();
                allReach &= reach && first && !second;
                detail.Append($"Z{z}: alcance={reach} primera={first} repetida={second}; ");
            }
            Check("F02", "Gameplay", allReach, detail.ToString());
            Check("F03", "Gameplay", session.Progress.IsRestored && restored.activeSelf && !degraded.activeSelf,
                  $"3/3: restaurado={session.Progress.IsRestored}, RestoredVisuals={restored.activeSelf}, DegradedVisuals={degraded.activeSelf}");
            var stream = restored.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "V6_Stream");
            Check("F03b", "Visual", stream != null && stream.gameObject.activeInHierarchy, "Agua V6 visible tras restaurar");
            var blocker = F("ExitBlocker");
            Check("F03c", "Visual", blocker != null && !blocker.gameObject.activeInHierarchy, "La maraña V6 de la barrera desaparece con ExitBlocker");

            reached = Walk(player, new Vector3(3, 0, 0), Vector2.up, 800);
            Check("F06b", "Gameplay", reached > 36f, $"Restaurado: el recorrido supera la barrera hasta Z={reached:F2}");
            Intrusions("restaurado", all, player);

            // F04: salida y reinicio
            player.Teleport(new Vector3(3, 0, 33), Quaternion.identity); Physics.SyncTransforms();
            for (int i = 0; i < 60 && !session.TryExit(player); i++) player.Simulate(Vector2.up, false, 0, .02f);
            bool completed = session.Progress.IsCompleted;
            session.Restart();
            var visuals = new[] { "Obstacle_01", "Obstacle_02", "Obstacle_03" }.Select(n => F(n).Find("V6_Visual")).ToArray();
            bool visualsBack = visuals.All(v => v != null && v.gameObject.activeInHierarchy && v.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "BosqueV6RemovalBurst"));
            Check("F04", "Gameplay", completed && session.Progress.RemovedCount == 0 && degraded.activeSelf && !restored.activeSelf && visualsBack,
                  $"Completa={completed}; tras R: retirados={session.Progress.RemovedCount}, degradado={degraded.activeSelf}, visuales de obstáculo V6 activos={visualsBack}");

            ObstacleFit(F);
            ImageChecks();
            Write();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        private static int MeshCoverage(Collider c, Bounds cb, List<MeshRenderer> renderers)
        {
            var temp = new List<MeshCollider>(); var layers = new Dictionary<GameObject, int>();
            foreach (var r in renderers)
            {
                if (!r.bounds.Intersects(cb)) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                layers[r.gameObject] = r.gameObject.layer;
                var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; r.gameObject.layer = ProbeLayer; temp.Add(mc);
            }
            Physics.SyncTransforms();
            bool prev = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int mask = 1 << ProbeLayer, hit = 0, total = 0;
            var dirs = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            for (int ix = 0; ix < 5; ix++) for (int iy = 0; iy < 5; iy++) for (int iz = 0; iz < 5; iz++)
            {
                total++;
                var p = cb.min + Vector3.Scale(cb.size, new Vector3(ix / 4f, iy / 4f, iz / 4f));
                if (c.ClosestPoint(p) != p) continue; // solo puntos dentro del collider real
                if (Physics.CheckSphere(p, .15f, mask)) { hit++; continue; }
                int back = dirs.Count(d => Physics.Raycast(p, d, out var h, 8f, mask) && Vector3.Dot(h.normal, d) > 0f);
                if (back >= 4) hit++;
            }
            Physics.queriesHitBackfaces = prev;
            foreach (var mc in temp) { var go = mc.gameObject; UnityEngine.Object.DestroyImmediate(mc); go.layer = layers[go]; }
            Physics.SyncTransforms();
            int inCollider = 0;
            for (int ix = 0; ix < 5; ix++) for (int iy = 0; iy < 5; iy++) for (int iz = 0; iz < 5; iz++)
            { var p = cb.min + Vector3.Scale(cb.size, new Vector3(ix / 4f, iy / 4f, iz / 4f)); if (c.ClosestPoint(p) == p) inCollider++; }
            return 100 * hit / Mathf.Max(1, inCollider);
        }

        // ---------------------------------------------------------------- utilidades de gameplay
        private static float Walk(ThirdPersonMotor p, Vector3 start, Vector2 dir, int steps)
        {
            p.Teleport(start, Quaternion.identity); Physics.SyncTransforms();
            float z = start.z;
            for (int i = 0; i < steps; i++) { p.Simulate(dir, false, 0, .02f); z = Mathf.Max(z, p.transform.position.z); }
            return z;
        }

        // ---------------------------------------------------------------- intrusiones de cámara y jugador
        private static bool IsSolid(string n) =>
            n.Contains("Rock") || n.Contains("Boulder") || n.Contains("Cliff") || n.Contains("DeadLog") || n.Contains("Stump") ||
            n.Contains("RootCluster") || n.Contains("PineRoots") || (n.Contains("TreeBroadleaf") && !n.Contains("Canopy"));
        private static bool IsFoliage(string n) =>
            n.Contains("Grass") || n.Contains("Fern") || n.Contains("Shrub") || n.Contains("Flower") || n.Contains("Dandelion") || n.Contains("Canopy");

        private static void Intrusions(string state, List<Transform> all, ThirdPersonMotor player)
        {
            // Posiciones de cámara calculadas con la misma lógica que ThirdPersonCamera (sin capa de sonda).
            var cams = new List<(Vector3 cam, Vector3 focus, Vector3 playerPos)>();
            int mask = ~(1 << ProbeLayer);
            var hits = new RaycastHit[32];
            foreach (float x in new[] { -0.6f, 1.5f, 3f, 5.4f })
                for (float z = -2f; z <= 34f; z += 2f)
                    foreach (float pitch in new[] { 0f, 20f, 45f })
                        for (float yaw = 0f; yaw < 360f; yaw += 30f)
                        {
                            var pp = new Vector3(x, 0, z);
                            var focus = pp + Vector3.up * 1.35f;
                            var desired = focus + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * 4.4f;
                            var off = desired - focus; float len = off.magnitude; var dir = off / len;
                            int c = Physics.SphereCastNonAlloc(focus, .18f, dir, hits, len, mask, QueryTriggerInteraction.Ignore);
                            float allowed = len;
                            for (int i = 0; i < c; i++)
                            {
                                if (hits[i].transform == player.transform || hits[i].transform.IsChildOf(player.transform)) continue;
                                allowed = Mathf.Min(allowed, Mathf.Max(.2f, hits[i].distance - .08f));
                            }
                            cams.Add((focus + dir * allowed, focus, pp));
                        }

            // Colliders temporales de medida sobre la geometría visual V6 activa (no se guardan).
            var temp = new List<MeshCollider>();
            var owner = new Dictionary<Collider, string>();
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (!mf.gameObject.activeInHierarchy || mf.sharedMesh == null) continue;
                string path = Path(mf.transform);
                if (!path.Contains("V6_") || path.Contains("V6_Ground") || path.Contains("V6_Backdrop") || path.Contains("V6_Puddle") || path.Contains("V6_Stream")) continue;
                string n = mf.sharedMesh.name;
                if (!IsSolid(n) && !IsFoliage(n)) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mf.gameObject.layer = ProbeLayer;
                temp.Add(mc);
                owner[mc] = (IsSolid(n) ? "S:" : "F:") + n;
            }
            Physics.SyncTransforms();
            bool prevBack = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            int probeMask = 1 << ProbeLayer;

            int inside = 0, nearSolid = 0, occSolid = 0, occFoliage = 0;
            var culprits = new Dictionary<string, int>();
            var dirs = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            foreach (var (cam, focus, _) in cams)
            {
                bool inSolid = false;
                int backHits = 0;
                foreach (var d in dirs)
                    if (Physics.Raycast(cam, d, out var h, 6f, probeMask) && owner[h.collider].StartsWith("S:") && Vector3.Dot(h.normal, d) > 0f) backHits++;
                if (backHits >= 4) { inside++; inSolid = true; }
                var near = Physics.OverlapSphere(cam, .12f, probeMask).Where(c => owner[c].StartsWith("S:")).ToArray();
                if (near.Length > 0) { nearSolid++; foreach (var c in near) Count(culprits, owner[c]); }
                var seg = focus - cam;
                var segHits = Physics.RaycastAll(cam, seg.normalized, seg.magnitude, probeMask);
                if (segHits.Any(h => owner[h.collider].StartsWith("S:"))) occSolid++;
                else if (segHits.Any(h => owner[h.collider].StartsWith("F:"))) occFoliage++;
                if (inSolid) foreach (var h in segHits) Count(culprits, owner[h.collider]);
            }
            int n0 = cams.Count;
            Metrics[$"camara_{state}"] = new Dictionary<string, object> { ["muestras"] = n0, ["dentro_de_solido"] = inside, ["a_menos_de_12cm_de_solido"] = nearSolid,
                ["linea_de_vista_cortada_por_solido"] = occSolid, ["linea_de_vista_cortada_por_vegetacion"] = occFoliage };
            float pct(int v) => 100f * v / Mathf.Max(1, n0);
            Rec($"K01-{state}", "Cámara", pct(inside) <= 1f ? "OK" : "AVISO",
                $"{n0} posiciones (4 carriles × 19 tramos × 3 inclinaciones × 12 giros): dentro de geometría sólida V6 {inside} ({pct(inside):F1}%), a <12 cm {nearSolid} ({pct(nearSolid):F1}%). Más afectados: {Top(culprits)}");
            Rec($"K02-{state}", "Cámara", pct(occSolid) <= 5f ? "OK" : "AVISO",
                $"Línea cámara→jugador cortada por sólido V6 {occSolid} ({pct(occSolid):F1}%), solo por vegetación {occFoliage} ({pct(occFoliage):F1}%)");

            // Paso del jugador a través de sólidos V6 en la orilla transitable.
            int cells = 0, blocked = 0; var walk = new Dictionary<string, int>(); var where = new Dictionary<string, Bounds>();
            for (float x = -0.6f; x <= 5.6f; x += 0.4f)
                for (float z = -3.5f; z <= 34.5f; z += 0.5f)
                {
                    var a = new Vector3(x, 0.35f + 0.05f, z); var b = new Vector3(x, 1.8f - 0.35f, z);
                    // Solo celdas transitables: si un collider de juego ya ocupa la celda, el jugador no puede estar ahí.
                    if (Physics.OverlapCapsule(a, b, .3f, ~probeMask, QueryTriggerInteraction.Ignore).Any(c => !(c is CharacterController))) continue;
                    cells++;
                    var hitsC = Physics.OverlapCapsule(a, b, .3f, probeMask).Where(c => owner[c].StartsWith("S:")).ToArray();
                    if (hitsC.Length > 0)
                    {
                        blocked++;
                        foreach (var c in hitsC)
                        {
                            Count(walk, owner[c]);
                            string key = owner[c].Substring(2) + "@" + Path(c.transform.parent != null ? c.transform.parent : c.transform).Split('/').Reverse().Skip(1).FirstOrDefault();
                            if (where.TryGetValue(key, out var bb)) { bb.Encapsulate(new Vector3(x, 0, z)); where[key] = bb; } else where[key] = new Bounds(new Vector3(x, 0, z), Vector3.zero);
                        }
                    }
                }
            Metrics[$"paso_{state}"] = new Dictionary<string, object> { ["celdas"] = cells, ["atraviesa_solido_visual"] = blocked };
            Rec($"K03-{state}", "Nivel", 100f * blocked / cells <= 2f ? "OK" : "AVISO",
                $"Orilla transitable ({cells} celdas libres de colliders de juego, rejilla 0,4×0,5 m): el cuerpo del jugador atraviesa sólidos V6 sin collider en {blocked} ({100f * blocked / cells:F1}%). Objetos: {Top(walk)}. Zonas: " +
                string.Join("; ", where.Select(kv => $"{kv.Key} X[{kv.Value.min.x:F1},{kv.Value.max.x:F1}] Z[{kv.Value.min.z:F1},{kv.Value.max.z:F1}]")));

            // Abertura del arco de salida (solo restaurado: la maraña es la barrera en degradado).
            if (state == "restaurado")
            {
                var gate = Physics.OverlapBox(new Vector3(3f, 1.2f, 38.5f), new Vector3(2.0f, 1.1f, 0.5f), Quaternion.identity, probeMask)
                    .Where(c => owner[c].StartsWith("S:")).Select(c => owner[c]).Distinct().ToArray();
                Check("K04", "Nivel", gate.Length == 0, $"Abertura del arco libre de sólidos V6: {gate.Length} {string.Join(",", gate)}");
            }

            Physics.queriesHitBackfaces = prevBack;
            foreach (var mc in temp) { mc.gameObject.layer = 0; UnityEngine.Object.DestroyImmediate(mc); }
            Physics.SyncTransforms();
        }

        private static void Count(Dictionary<string, int> d, string k) => d[k] = d.TryGetValue(k, out var v) ? v + 1 : 1;
        private static string Top(Dictionary<string, int> d) =>
            d.Count == 0 ? "ninguno" : string.Join(", ", d.OrderByDescending(kv => kv.Value).Take(5).Select(kv => $"{kv.Key.Substring(2)}×{kv.Value}"));

        // ---------------------------------------------------------------- legibilidad de obstáculos
        private static void ObstacleFit(Func<string, Transform> F)
        {
            foreach (var name in new[] { "Obstacle_01", "Obstacle_02", "Obstacle_03" })
            {
                var o = F(name);
                var box = o.GetComponent<BoxCollider>();
                var cb = box.bounds;
                var rs = o.Find("V6_Visual").GetComponentsInChildren<MeshRenderer>();
                var vb = rs[0].bounds; foreach (var r in rs) vb.Encapsulate(r.bounds);
                float topGap = cb.max.y - vb.max.y;            // >0: el collider sobresale por encima del visual (pared invisible)
                float overX = Mathf.Max(0, vb.max.x - cb.max.x) + Mathf.Max(0, cb.min.x - vb.min.x);
                float coverX = (Mathf.Min(vb.max.x, cb.max.x) - Mathf.Max(vb.min.x, cb.min.x)) / cb.size.x;
                Metrics["obstaculo_" + name] = new Dictionary<string, object> { ["collider"] = cb.size.ToString("F2"), ["visual"] = vb.size.ToString("F2"),
                    ["collider_sobre_visual_m"] = Math.Round(topGap, 2), ["visual_fuera_en_x_m"] = Math.Round(overX, 2), ["cobertura_x"] = Math.Round(coverX, 2) };
                Rec("L01." + name, "Legibilidad", topGap <= 0.25f && coverX >= 0.8f ? "OK" : "AVISO",
                    $"Collider {cb.size.ToString("F2")} vs visual {vb.size.ToString("F2")}; collider por encima del visual {topGap:F2} m; visual cubre {coverX:P0} del ancho; sobresale {overX:F2} m en X");
            }
        }

        // ---------------------------------------------------------------- recursos
        private static void AssetChecks()
        {
            int bad = 0; var shaderInfo = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Shader", new[] { ArtRoot + "/Shaders" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g));
                int errors = ShaderUtil.GetShaderMessages(s).Count(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error);
                if (errors > 0 || !s.isSupported) bad++;
                shaderInfo.Add($"{s.name}:{errors}");
            }
            Check("A01", "Recursos", bad == 0, "Shaders V6 sin errores: " + string.Join(", ", shaderInfo));

            int prefabs = 0, withCollider = 0, nullMat = 0, badShader = 0, missing = 0, noLod = 0;
            var offenders = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { ArtRoot + "/Prefabs" }))
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)); prefabs++;
                if (p.GetComponentsInChildren<Collider>(true).Length > 0) { withCollider++; offenders.Add(p.name + ":collider"); }
                foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) { nullMat++; offenders.Add(p.name + ":material nulo"); }
                        else if (!m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader") { badShader++; offenders.Add(p.name + ":" + m.shader.name); }
                    }
                missing += p.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (p.GetComponent<LODGroup>() == null) noLod++;
            }
            Check("A02", "Recursos", withCollider == 0 && nullMat == 0 && badShader == 0 && missing == 0,
                  $"{prefabs} prefabs VIS_V6: con collider {withCollider}, material nulo {nullMat}, shader no soportado {badShader}, scripts perdidos {missing} {string.Join("; ", offenders.Take(5))}");
            Rec("A03", "Rendimiento", noLod == 0 ? "OK" : "AVISO", $"Prefabs VIS_V6 sin LODGroup/corte por distancia: {noLod} de {prefabs}");

            int wrongNormal = 0, wrongMask = 0, textures = 0, big = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                textures++;
                string n = System.IO.Path.GetFileNameWithoutExtension(path);
                if ((n.EndsWith("_Normal") || n.StartsWith("T_Water_Normal")) && ti.textureType != TextureImporterType.NormalMap) wrongNormal++;
                if (n.EndsWith("_Mask") && ti.sRGBTexture) wrongMask++;
                if (ti.maxTextureSize > 2048) big++;
            }
            Check("A04", "Recursos", wrongNormal == 0 && wrongMask == 0 && big == 0,
                  $"{textures} texturas: normales mal importadas {wrongNormal}, máscaras en sRGB {wrongMask}, >2048 px {big}");

            int systems = 0, loopsUnbounded = 0, oneShotNoDestroy = 0, noMat = 0; var fx = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { ArtRoot + "/Particles" }))
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                var root = p.GetComponent<ParticleSystem>();
                foreach (var ps in p.GetComponentsInChildren<ParticleSystem>(true))
                {
                    systems++;
                    var r = ps.GetComponent<ParticleSystemRenderer>();
                    if (r.sharedMaterial == null) { noMat++; fx.Add(p.name + ":sin material"); }
                    if (ps.main.loop && ps.main.maxParticles > 500) { loopsUnbounded++; fx.Add(p.name + $":max {ps.main.maxParticles}"); }
                }
                if (!root.main.loop && root.main.stopAction != ParticleSystemStopAction.Destroy) { oneShotNoDestroy++; fx.Add(p.name + ":no se destruye"); }
            }
            Check("A05", "Partículas", noMat == 0 && loopsUnbounded == 0 && oneShotNoDestroy == 0,
                  $"{systems} sistemas en {AssetDatabase.FindAssets("t:Prefab", new[] { ArtRoot + "/Particles" }).Length} prefabs: sin material {noMat}, bucles >500 partículas {loopsUnbounded}, ráfagas que no se destruyen {oneShotNoDestroy} {string.Join("; ", fx)}");
        }

        // ---------------------------------------------------------------- capturas
        private static void ImageChecks()
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Docs/Art/Entregable06/CAPTURAS"));
            var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "v6_*.png") : new string[0];
            int magentaFiles = 0, flatFiles = 0; var lum = new List<string>();
            foreach (var f in files)
            {
                var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(f));
                var px = t.GetPixels32();
                int magenta = 0; double sum = 0, sum2 = 0; int step = 7, n = 0;
                for (int i = 0; i < px.Length; i += step)
                {
                    var c = px[i]; n++;
                    if (c.r > 230 && c.g < 40 && c.b > 230) magenta++;
                    double l = (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0; sum += l; sum2 += l * l;
                }
                double mean = sum / n, std = Math.Sqrt(Math.Max(0, sum2 / n - mean * mean));
                if (magenta > n * 0.001) magentaFiles++;
                if (std < 0.03) flatFiles++;
                lum.Add($"{System.IO.Path.GetFileNameWithoutExtension(f).Replace("v6_", "")}={mean:F2}");
                UnityEngine.Object.DestroyImmediate(t);
            }
            int deg = files.Count(f => f.Contains("_degradado_")), res = files.Count(f => f.Contains("_restaurado_"));
            Check("V01", "Visual", deg >= 7 && deg == res && magentaFiles == 0 && flatFiles == 0,
                  $"{files.Length} capturas: con magenta (shader roto) {magentaFiles}, planas/vacías {flatFiles}. Luminancia media: {string.Join(", ", lum)}");
        }

        // ---------------------------------------------------------------- escena: claves
        private static string Path(Transform t)
        {
            var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s;
        }

        private static Dictionary<string, string> Colliders(UnityEngine.SceneManagement.Scene scene)
        {
            var d = new Dictionary<string, string>();
            foreach (var c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)))
            {
                string key = Path(c.transform) + "#" + c.GetType().Name;
                int k = 0; while (d.ContainsKey(key + (k == 0 ? "" : "~" + k))) k++;
                var b = c.transform;
                string extra = c is BoxCollider bc ? $"{bc.center.ToString("F3")}{bc.size.ToString("F3")}" :
                               c is CapsuleCollider cc ? $"{cc.center.ToString("F3")}{cc.radius:F3}{cc.height:F3}{cc.direction}" :
                               c is CharacterController ch ? $"{ch.center.ToString("F3")}{ch.radius:F3}{ch.height:F3}" : "";
                d[key + (k == 0 ? "" : "~" + k)] = $"{c.enabled}{c.isTrigger}{b.position.ToString("F3")}{b.rotation.eulerAngles.ToString("F2")}{b.lossyScale.ToString("F3")}{extra}";
            }
            return d;
        }

        private static HashSet<string> Behaviours(UnityEngine.SceneManagement.Scene scene) =>
            new HashSet<string>(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(m => m != null && !(m.GetType().Namespace ?? "").StartsWith("Bosque.ArtV6"))
                .Select(m => Path(m.transform) + "#" + m.GetType().FullName));

        private static string Sha(string assetPath)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", assetPath))))).Replace("-", "");
        }

        private static void Write()
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Docs/QA/Entregable06/EVIDENCIAS"));
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            sb.AppendLine("id,area,estado,detalle");
            foreach (var r in Results) sb.AppendLine($"{r.id},{r.area},{r.status},\"{r.detail.Replace("\"", "'")}\"");
            string name = _integrated ? "sonda-qa-v6-integrada" : "sonda-qa-v6";
            File.WriteAllText(System.IO.Path.Combine(dir, name + ".csv"), sb.ToString(), new UTF8Encoding(true));
            var js = new StringBuilder("{\n");
            js.AppendLine($"  \"unity\": \"{Application.unityVersion}\",");
            js.AppendLine($"  \"fecha_utc\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");
            js.AppendLine($"  \"resumen\": {{\"OK\": {Results.Count(r => r.status == "OK")}, \"AVISO\": {Results.Count(r => r.status == "AVISO")}, \"FALLA\": {Results.Count(r => r.status == "FALLA")}}},");
            js.AppendLine("  \"metricas\": {");
            js.AppendLine(string.Join(",\n", Metrics.Select(kv => $"    \"{kv.Key}\": {Json(kv.Value)}")));
            js.AppendLine("  }\n}");
            File.WriteAllText(System.IO.Path.Combine(dir, name + ".json"), js.ToString(), new UTF8Encoding(false));
            Debug.Log($"[QA6] RESUMEN OK={Results.Count(r => r.status == "OK")} AVISO={Results.Count(r => r.status == "AVISO")} FALLA={Results.Count(r => r.status == "FALLA")}");
        }

        private static string Json(object v) =>
            v is Dictionary<string, object> d ? "{" + string.Join(", ", d.Select(kv => $"\"{kv.Key}\": {Json(kv.Value)}")) + "}" :
            v is string s ? $"\"{s}\"" : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
    }
}
