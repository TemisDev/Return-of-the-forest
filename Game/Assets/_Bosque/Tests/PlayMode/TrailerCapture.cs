using System;
using System.Collections;
using System.IO;
using System.Linq;
using Bosque.Abilities;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.UI;
using Bosque.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Bosque.Tests.PlayMode
{
    /// <summary>
    /// Tomas del tráiler (manual: no corre en las baterías normales). Cada toma se graba fotograma a fotograma a
    /// 3840×2160 con el tiempo de juego fijo (30 fps exactos, sin tirones ni fotogramas perdidos), sin HUD, con una
    /// cámara propia que copia la de juego (posproceso V6 y SMAA). Salida: output/trailer/raw/&lt;toma&gt;/f0000.jpg.
    /// Lanzar: -runTests -testPlatform PlayMode -testFilter TrailerCapture
    /// </summary>
    [Explicit("Grabación del tráiler: se lanza a mano"), Timeout(1800000)]
    public sealed class TrailerCapture
    {
        private const int W = 3840, H = 2160, Fps = 30;
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../output/trailer/raw"));
        private Camera cam;
        private RenderTexture rt;
        private Texture2D img;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            QualitySettings.lodBias = 2.5f; QualitySettings.shadowDistance = 140f;
            GameProgress.ResetAll(); GamePause.Set(false); FallenWood.AutoTick = false;
            Directory.CreateDirectory(Root);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = 0;
            if (rt != null) { rt.Release(); Object.Destroy(rt); }
            if (img != null) Object.Destroy(img);
            GameProgress.ResetAll();
            yield return null;
        }

        // ------------------------------------------------------------------ cámara y grabación
        private void MakeCamera()
        {
            var src = Camera.main;
            var go = new GameObject("CamaraTrailer");
            cam = go.AddComponent<Camera>();
            cam.CopyFrom(src); cam.enabled = false; cam.fieldOfView = 50f; cam.nearClipPlane = .08f; cam.farClipPlane = 900f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High; data.renderShadows = true;
            data.volumeLayerMask = src.GetUniversalAdditionalCameraData().volumeLayerMask;
            foreach (var b in src.GetComponents<MonoBehaviour>()) if (!(b is AudioListener)) b.enabled = false;   // la cámara de juego no se mueve sola
            if (rt == null) { rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; rt.Create(); }
            if (img == null) img = new Texture2D(W, H, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
        }

        private static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        /// <summary>Graba una toma: <paramref name="pose"/> da posición y punto de mira para t∈[0,1]; <paramref name="each"/> anima la escena.</summary>
        private IEnumerator Shot(string name, float seconds, Func<float, (Vector3 pos, Vector3 look)> pose, Action<float, int> each = null)
        {
            var dir = Path.Combine(Root, name); Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);
            int frames = Mathf.RoundToInt(seconds * Fps);
            Time.captureFramerate = Fps;
            for (int i = 0; i < frames; i++)
            {
                float t = frames > 1 ? i / (float)(frames - 1) : 0f;
                each?.Invoke(t, i);
                yield return null;                                   // la escena avanza 1/30 s
                var (pos, look) = pose(t);
                cam.transform.position = pos; cam.transform.rotation = Quaternion.LookRotation(look - pos);
                cam.Render();
                RenderTexture.active = rt; img.ReadPixels(new Rect(0, 0, W, H), 0, 0); img.Apply(false); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"f{i:0000}.jpg"), img.EncodeToJPG(96));
            }
            Time.captureFramerate = 0;
            Debug.Log($"[Trailer] {name}: {frames} fotogramas");
        }

        private static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null; yield return null;
            if (Presentation.Instance != null) Presentation.Instance.TitleCard.Hide();
            yield return Frames(20);
        }

        private static void Walk(ThirdPersonMotor m, Vector3 toward, bool walk, float dt)
        {
            var d = toward - m.transform.position; d.y = 0;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            m.Simulate(d.magnitude > .4f ? Vector2.up : Vector2.zero, false, yaw, dt, walk);
        }

        private static Vector3 Lerp3(Vector3 a, Vector3 b, float t) => Vector3.Lerp(a, b, Ease(t));

        // ------------------------------------------------------------------ zona 1: arroyo
        [UnityTest, Order(1)]
        public IEnumerator Arroyo()
        {
            yield return Load(GameFlow.ArroyoScene);
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var m = session.Player; var input = m.GetComponent<ThirdPersonInput>(); input.enabled = false;
            var actor = m.GetComponent<PlayerInteractor>();
            MakeCamera();
            var obstacles = Object.FindObjectsByType<RemovableObstacle>(FindObjectsSortMode.None).OrderBy(o => o.transform.position.z).ToArray();
            Assert.That(obstacles.Length, Is.EqualTo(3));

            // S1: la Guardiana camina por la orilla del cauce seco; la cámara retrocede delante de ella, baja.
            var start = m.transform.position; var fwd = m.transform.forward; fwd.y = 0; fwd.Normalize();
            var target = start + fwd * 9f;
            yield return Shot("S1_arroyo_seco", 5f,
                t => { var p = m.transform.position; var side = Vector3.Cross(Vector3.up, fwd);
                       return (p - fwd * Mathf.Lerp(3.4f, 2.8f, t) + side * 1.2f + Vector3.up * Mathf.Lerp(1.7f, 1.45f, t), p + fwd * 7f + Vector3.up * .7f - side * .6f); },
                (t, i) => { input.enabled = false; Walk(m, target, true, 1f / Fps); });

            // S3: junto al primer bloqueo, lo retira (gesto real) y el agua vuelve al cauce; la cámara sube y abre el plano.
            var ob = obstacles[0]; var oc = ob.GetComponent<Collider>()?.bounds.center ?? ob.transform.position;
            var fromSide = oc + new Vector3(2.2f, 0, -1.4f); fromSide.y = start.y + .1f;
            m.Teleport(fromSide, Quaternion.LookRotation(new Vector3(oc.x - fromSide.x, 0, oc.z - fromSide.z)));
            yield return Frames(12);
            bool removed = false;
            var creekMid = obstacles[1].transform.position;
            yield return Shot("S3_arroyo_restaura", 6.5f,
                t =>
                {
                    var hero = m.transform.position + Vector3.up * 1.2f;
                    var near = (m.transform.position + m.transform.right * 1.9f + m.transform.forward * 1.6f + Vector3.up * 1.35f, hero);
                    var far = (new Vector3(creekMid.x + .6f, creekMid.y + 6.2f, creekMid.z - 11f), creekMid + new Vector3(0, -.6f, 7f));
                    float k = Ease((t - .3f) / .7f);
                    return (Vector3.Lerp(near.Item1, far.Item1, k), Vector3.Lerp(near.Item2, far.Item2, k));
                },
                (t, i) =>
                {
                    input.enabled = false; m.Simulate(Vector2.zero, false, m.transform.eulerAngles.y, 1f / Fps);
                    if (!removed && t > .18f)
                    {
                        removed = true; actor.Scan(); actor.TryInteract();
                        foreach (var id in new[] { "arroyo.log", "arroyo.rocks", "arroyo.branches" }) session.Progress.TryRemove(id);
                    }
                });
            Assert.That(session.Progress.IsRestored, "El arroyo revive en la toma");
        }

        // ------------------------------------------------------------------ zona 2: claro
        [UnityTest, Order(2)]
        public IEnumerator Claro()
        {
            GameProgress.UnlockGrowth();
            yield return Load(GameFlow.ClaroScene);
            var session = Object.FindFirstObjectByType<ClaroSession>();
            var m = session.Player; var input = m.GetComponent<ThirdPersonInput>(); input.enabled = false;
            var ability = m.GetComponent<GrowthAbility>();
            MakeCamera();
            var humid = session.Points.Where(p => p.Suitable).ToArray();
            // Los dos primeros lechos, fuera de cámara.
            for (int k = 0; k < humid.Length - 1; k++)
            {
                m.Teleport(humid[k].transform.position + new Vector3(0, 0, -1.2f), Quaternion.identity);
                yield return Frames(10); yield return new WaitForSeconds(.7f); ability.TryGrow();
            }
            yield return new WaitForSeconds(2.5f);
            // S4: el último brote crece ante la cámara y el claro revive.
            var bed = humid.Last().transform.position;
            m.Teleport(bed + new Vector3(-.4f, 0, -1.25f), Quaternion.LookRotation(new Vector3(.3f, 0, 1f)));
            yield return Frames(10); yield return new WaitForSeconds(.7f);
            bool grown = false;
            yield return Shot("S4_claro_brota", 4.5f,
                t => (Lerp3(bed + new Vector3(3.0f, 1.0f, -1.5f), bed + new Vector3(2.3f, 1.8f, -3.2f), t), bed + new Vector3(-.3f, Mathf.Lerp(.6f, .85f, t), .1f)),
                (t, i) => { input.enabled = false; m.Simulate(Vector2.zero, false, m.transform.eulerAngles.y, 1f / Fps); if (!grown && t > .08f) { grown = true; ability.TryGrow(); } });
        }

        // ------------------------------------------------------------------ zona 3: refugio
        [UnityTest, Order(3)]
        public IEnumerator Refugio()
        {
            GameProgress.CompleteZone(GameProgress.Arroyo); GameProgress.CompleteZone(GameProgress.Claro);
            yield return Load(GameFlow.RefugioScene);
            var session = Object.FindFirstObjectByType<RefugioSession>();
            var m = session.Player; var input = m.GetComponent<ThirdPersonInput>(); input.enabled = false;
            var actor = m.GetComponent<PlayerInteractor>(); var ability = m.GetComponent<GrowthAbility>();
            MakeCamera();
            IEnumerator Near(Vector3 p, float back = 1.3f) { m.Teleport(p + new Vector3(0, 0, -back), Quaternion.identity); for (int i = 0; i < 8; i++) { input.enabled = false; m.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null; } }
            var good = session.Spots.Where(s => s.Suitable).ToArray();
            var tronco = session.Items.First(i => i.ItemId == "refugio.tronco"); var piedras = session.Items.First(i => i.ItemId == "refugio.piedras");
            yield return Near(tronco.transform.position); actor.TryInteract(); yield return Near(good[0].transform.position); actor.TryInteract();
            yield return Near(piedras.transform.position); actor.TryInteract(); yield return Near(good[1].transform.position); actor.TryInteract();
            yield return Near(session.GrowthPoints.First(p => p.Suitable).transform.position, 1.2f);
            yield return new WaitForSeconds(.7f); ability.TryGrow();
            Assert.That(session.Progress.IsRestored);
            var fauna = session.Fauna;
            for (float t = 0; t < 6f && !fauna.Running; t += Time.deltaTime) yield return null;
            Assert.That(fauna.Running, "El zorro sale");
            var fox = fauna.Animal.transform;
            // Plano de documental: se elige la posición de cámara con la vista más despejada del tramo que el zorro
            // recorre antes de llegar (trazados de rayos contra rocas, troncos y árboles, que ya son sólidos).
            var wp = fauna.Waypoints.Select(w => w.position).ToArray();
            var path = new System.Collections.Generic.List<Vector3>();
            for (int i = 1; i < wp.Length; i++) for (int k = 0; k < 8; k++) path.Add(Vector3.Lerp(wp[i - 1], wp[i], k / 8f));
            path.Add(wp[wp.Length - 1]);
            float len = 0; int startIdx = path.Count - 1;
            for (int i = path.Count - 1; i > 0 && len < 8.5f; i--) { len += Vector3.Distance(path[i], path[i - 1]); startIdx = i - 1; }
            var seg = path.Skip(startIdx).Take(Mathf.Max(2, path.Count - startIdx - 3)).ToList();
            var mid = seg[seg.Count / 2];
            Vector3 best = mid + Vector3.up * 3f; int bestScore = -1;
            for (int a = 0; a < 36; a++)
                foreach (var r in new[] { 3.2f, 4.2f, 5.2f })
                    foreach (var h in new[] { .7f, 1.3f })
                    {
                        var c = mid + new Vector3(Mathf.Cos(a * 10 * Mathf.Deg2Rad), 0, Mathf.Sin(a * 10 * Mathf.Deg2Rad)) * r; c.y = mid.y + h;
                        if (Physics.CheckSphere(c, .35f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        int score = seg.Count(q => !Physics.Linecast(c, q + Vector3.up * .3f, ~0, QueryTriggerInteraction.Ignore));
                        if (score > bestScore) { bestScore = score; best = c; }
                    }
            Debug.Log($"[Trailer] zorro: cámara con {bestScore}/{seg.Count} puntos visibles");
            for (float t = 0; t < 40f && Vector3.Distance(fox.position, seg[0]) > 1.2f && Vector3.Distance(fox.position, wp[wp.Length - 1]) > len; t += Time.deltaTime) yield return null;
            var camAt = best;
            Vector3 look = fox.position + Vector3.up * .3f;
            yield return Shot("S5_refugio_zorro", 5f,
                t =>
                {
                    look = Vector3.Lerp(look, fox.position + Vector3.up * .3f, .12f);
                    return (Vector3.Lerp(camAt, camAt + (look - camAt).normalized * .6f, Ease(t)), look);
                });
        }

        // ------------------------------------------------------------------ zona 4: valle (degradado, limpieza, restaurado)
        [UnityTest, Order(4)]
        public IEnumerator Valle()
        {
            GameProgress.CompleteZone(GameProgress.Arroyo); GameProgress.CompleteZone(GameProgress.Claro); GameProgress.CompleteZone(GameProgress.Refugio);
            yield return Load(GameFlow.ValleScene);
            var s = Object.FindFirstObjectByType<ValleSession>();
            var m = s.Player; var input = m.GetComponent<ThirdPersonInput>(); input.enabled = false;
            var actor = m.GetComponent<PlayerInteractor>(); var bag = m.GetComponent<LitterBag>();
            var terrain = s.Terrain.GetComponent<Terrain>();
            Vector3 G(float x, float z, float up) => new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y + up, z);
            MakeCamera();

            // S2: vuelo sobre las lagunas y el Mayu turbios, el valle seco.
            yield return Shot("S2_valle_degradado", 5f,
                t => (Lerp3(G(12, 30, 30), G(30, 56, 21), t), Lerp3(G(72, 74, 0), G(80, 60, 0), t)));

            // Limpieza completa fuera de cámara (como en las pruebas).
            IEnumerator Use(Component target)
            {
                var col = target.GetComponent<Collider>(); var c = col.bounds.center;
                foreach (var dir in new[] { Vector3.back, Vector3.forward, Vector3.left, Vector3.right })
                {
                    var p = c + dir * (Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) + .6f);
                    p.y = Mathf.Max(col.bounds.min.y, terrain.SampleHeight(p)) + .05f;
                    m.Teleport(p, Quaternion.LookRotation(-dir));
                    for (float k = 0; k < .12f; k += Time.deltaTime) { input.enabled = false; m.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null; }
                    actor.Scan();
                    if ((Object)actor.Target == target) break;
                }
                actor.TryInteract();
            }
            RecycleBin Bin(WasteKind k) => s.Bins.First(b => b.Kind == k && b.Station == "Campamento");
            IEnumerator EmptyBag() { foreach (var k in WasteKinds.All) if (bag.CountOf(k) > 0) yield return Use(Bin(k)); }
            foreach (var sector in s.Sectors)
            {
                foreach (var l in sector.Litter.OrderBy(x => x.transform.position.y)) { if (!bag.CanFit(l)) yield return EmptyBag(); if (!l.Collected) yield return Use(l); }
                foreach (var w in sector.Wood) { yield return Use(w); w.BeginWork(actor); for (int i = 0; i < 400 && !w.Cleared; i++) { w.Tick(.05f, true); yield return null; } }
            }
            yield return EmptyBag();
            Assert.That(s.IsRestored, "El valle revive");
            s.Fauna.TimeScale = 6f;
            for (float t = 0; t < 30f && !s.IsCompleted; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(6f);                       // el color y el agua terminan de cambiar
            if (Presentation.Instance != null) { Presentation.Instance.Notebook.Hide(); Presentation.Instance.Ending.Hide(); GamePause.Set(false); }

            // S6: la Guardiana cruza el puente sobre el Mayu, cristalino; cámara a ras del agua, aguas abajo.
            var water = Object.FindFirstObjectByType<ValleWater>();
            var bridge = water.Crossings.Where(c => c.bridge).OrderBy(c => Vector3.Distance(c.position, new Vector3(32, 0, 9))).First().position;
            var mayu = water.Streams.First(x => x.name == "Río Mayu");
            ValleWater.Nearest(mayu, new Vector2(bridge.x, bridge.z), out _, out _, out _, out var flow, out _);
            var across = new Vector3(-flow.y, 0, flow.x); var down = new Vector3(flow.x, 0, flow.y);
            var a = bridge - across * 9f; var b = bridge + across * 9f;
            a.y = terrain.SampleHeight(a) + .2f;
            m.Teleport(a, Quaternion.LookRotation(across)); yield return Frames(15);
            yield return Shot("S6_valle_puente", 5f,
                t => (Lerp3(bridge + down * 9f - across * 3f + Vector3.up * .9f, bridge + down * 7.5f + across * 2f + Vector3.up * 1.4f, t), m.transform.position + Vector3.up * 1.1f),
                (t, i) => { input.enabled = false; Walk(m, b, true, 1f / Fps); });

            // S7: fondo del título: vuelo lento sobre las lagunas restauradas.
            yield return Shot("S7_valle_lagunas", 7f,
                t => (Lerp3(G(40, 104, 16), G(52, 96, 11), t), Lerp3(G(84, 50, 0), G(84, 56, 0), t)));
        }
    }
}
