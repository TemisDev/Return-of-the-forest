using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bosque.Audio;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.UI;
using Bosque.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bosque.Tests.PlayMode
{
    /// <summary>Zona 4, "El valle olvidado": limpieza por sectores, bolsa, reciclaje, troncos, rastreo, final.</summary>
    public sealed class VallePlayModeTests
    {
        private ValleSession s;
        private ThirdPersonMotor motor;
        private ThirdPersonInput input;
        private PlayerInteractor actor;
        private LitterBag bag;
        private readonly List<string> played = new List<string>();
        private SfxPlayer sfx;

        private IEnumerator Load(bool previousZones = true)
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll(); GamePause.Set(false); FallenWood.AutoTick = false;
            if (previousZones) { GameProgress.CompleteZone(GameProgress.Arroyo); GameProgress.CompleteZone(GameProgress.Claro); GameProgress.CompleteZone(GameProgress.Refugio); }
            SceneManager.LoadScene(GameFlow.ValleScene);
            yield return null; yield return null;
            s = Object.FindFirstObjectByType<ValleSession>();
            motor = s.Player; input = motor.GetComponent<ThirdPersonInput>(); input.enabled = false;
            actor = motor.GetComponent<PlayerInteractor>(); bag = motor.GetComponent<LitterBag>();
            sfx = Object.FindFirstObjectByType<SfxPlayer>(); played.Clear(); sfx.Played += played.Add;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (sfx != null) sfx.Played -= played.Add;
            FallenWood.AutoTick = true; GamePause.Set(false); GameProgress.ResetAll();
            if (Presentation.Instance != null) { Presentation.Instance.Notebook.Hide(); Presentation.Instance.Ending.Hide(); }
            yield return null;
        }

        private IEnumerator Settle(float t = .12f)
        {
            for (float k = 0; k < t; k += Time.deltaTime) { if (input.enabled) input.enabled = false; motor.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null; }
        }

        /// <summary>Se coloca junto al objetivo (por el lado libre) y pulsa E.</summary>
        private IEnumerator Use(Component target)
        {
            var col = target.GetComponent<Collider>();
            var c = col.bounds.center;
            foreach (var dir in new[] { Vector3.back, Vector3.forward, Vector3.left, Vector3.right })
            {
                var p = c + dir * (Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) + .6f);
                p.y = Mathf.Max(col.bounds.min.y, s.Terrain.GetComponent<Terrain>().SampleHeight(p)) + .05f;
                motor.Teleport(p, Quaternion.LookRotation(-dir)); yield return Settle();
                actor.Scan();
                if ((Object)actor.Target == target) break;
            }
            Assert.That((Object)actor.Target, Is.EqualTo(target), "Alcanza " + target.name);
            actor.TryInteract();
        }

        private RecycleBin Bin(WasteKind k, string station = "Campamento") => s.Bins.First(b => b.Kind == k && b.Station == station);

        private IEnumerator EmptyBag()
        {
            foreach (var k in WasteKinds.All)
                if (bag.CountOf(k) > 0) yield return Use(Bin(k));
            Assert.That(bag.IsEmpty);
        }

        private IEnumerator ClearWood(FallenWood w)
        {
            yield return Use(w);
            w.BeginWork(actor);
            for (int i = 0; i < 400 && !w.Cleared; i++) { w.Tick(.05f, true); yield return null; }
            Assert.That(w.Cleared, w.name);
        }

        [UnityTest]
        public IEnumerator ValleyHasFourSectorsFiftySixItemsAndThreeStations()
        {
            yield return Load();
            Assert.That(s.Sectors.Length, Is.EqualTo(4));
            Assert.That(s.LitterTotal, Is.EqualTo(40)); Assert.That(s.AllWood.Count(), Is.EqualTo(16));
            Assert.That(s.Sectors.All(x => x.Total == 14), "Sectores equilibrados");
            Assert.That(s.Bins.Length, Is.EqualTo(12));
            foreach (var st in s.Bins.GroupBy(b => b.Station))
                CollectionAssert.AreEquivalent(WasteKinds.All, st.Select(b => b.Kind), st.Key + " con los cuatro contenedores");
            foreach (var k in WasteKinds.All) Assert.That(s.AllLitter.Count(l => l.Kind == k), Is.GreaterThanOrEqualTo(4), "Hay residuos de " + k);
            Assert.That(s.AllLitter.Count(l => l.Slots == 3), Is.GreaterThanOrEqualTo(3), "Llantas que ocupan 3 espacios");
            var terrain = s.Terrain.GetComponent<Terrain>();
            foreach (var l in s.AllLitter)
            {
                var p = l.transform.position;
                Assert.That(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)), Is.LessThan(100f), l.name + " dentro del valle");
                Assert.That(p.y, Is.GreaterThan(terrain.SampleHeight(p) - .3f), l.name + " no enterrado");
            }
            Assert.That(s.AllLitter.Count(l => l.transform.position.y - terrain.SampleHeight(l.transform.position) > 2f), Is.EqualTo(2), "Dos residuos en lo alto de las salientes");
            Assert.That(s.Fauna.Animal.activeSelf, Is.False);
            Assert.That(bag.Capacity, Is.EqualTo(8));
            Assert.That(Presentation.Instance.Hud.Model, Is.InstanceOf<ValleHud>());
        }

        [UnityTest]
        public IEnumerator TheBagFillsUpAndTyresTakeThreeSlots()
        {
            yield return Load();
            var singles = s.AllLitter.Where(l => l.Slots == 1).OrderBy(l => l.transform.position.sqrMagnitude).Take(8).ToList();
            foreach (var l in singles) { yield return Use(l); Assert.That(l.Collected, l.name); }
            Assert.That(bag.Used, Is.EqualTo(8)); Assert.That(bag.Free, Is.Zero);
            var next = s.AllLitter.First(l => !l.Collected && l.Slots == 1);
            yield return Use(next);
            Assert.That(next.Collected, Is.False, "Bolsa llena: no cabe más");
            Assert.That(s.LastHint, Does.Contain("reciclaje")); Assert.That(s.Mistakes, Is.EqualTo(1));
            yield return EmptyBag();
            Assert.That(s.DepositedCount, Is.EqualTo(8));
            // Con 6 libres cabe una llanta; con 2 no.
            var tyre = s.AllLitter.First(l => l.Slots == 3);
            for (int i = 0; i < 6; i++) { var l = s.AllLitter.Where(x => !x.Collected && x.Slots == 1).OrderBy(x => (x.transform.position - motor.transform.position).sqrMagnitude).First(); yield return Use(l); }
            yield return Use(tyre);
            Assert.That(tyre.Collected, Is.False, "La llanta no cabe con 2 espacios");
            Assert.That(s.LastHint, Does.Contain("3 espacios"));
        }

        [UnityTest]
        public IEnumerator EachBinOnlyTakesItsOwnKind()
        {
            yield return Load();
            var plastic = s.AllLitter.Where(l => l.Kind == WasteKind.Plastico).OrderBy(l => l.transform.position.sqrMagnitude).First();
            var glass = s.AllLitter.Where(l => l.Kind == WasteKind.VidrioMetal).OrderBy(l => l.transform.position.sqrMagnitude).First();
            yield return Use(plastic); yield return Use(glass);
            yield return Use(Bin(WasteKind.PapelCarton));
            Assert.That(bag.Count, Is.EqualTo(2), "El contenedor de papel no acepta plástico ni vidrio");
            Assert.That(s.LastHint, Does.Contain("No llevas"));
            yield return Use(Bin(WasteKind.Plastico));
            Assert.That(bag.Count, Is.EqualTo(1)); Assert.That(bag.CountOf(WasteKind.VidrioMetal), Is.EqualTo(1));
            Assert.That(s.DepositedCount, Is.EqualTo(1));
            Assert.That(played.Contains("remove.rocks"), "Sonido al depositar");
        }

        [UnityTest]
        public IEnumerator FallenWoodNeedsHoldingAndProgressDecays()
        {
            yield return Load();
            var w = s.AllWood.Where(x => x.Label == "el tronco grueso").OrderBy(x => x.transform.position.sqrMagnitude).First();
            yield return Use(w);
            Assert.That(w.Cleared, Is.False, "Una pulsación no basta");
            w.BeginWork(actor);
            for (int i = 0; i < 20; i++) w.Tick(.05f, true);
            Assert.That(w.Progress01, Is.InRange(.2f, .4f));
            Assert.That(Presentation.Instance.Hud.Model is IZoneHudExtra x && x.WorkProgress > 0, "Barra de progreso en el HUD");
            for (int i = 0; i < 20; i++) w.Tick(.05f, false);
            Assert.That(w.Progress01, Is.LessThan(.2f), "Al soltar, el avance se pierde poco a poco");
            yield return ClearWood(w);
            Assert.That(w.GetComponent<Collider>().enabled, Is.False, "Ya no bloquea el paso");
            Assert.That(played.Contains("remove.log"));
        }

        [UnityTest]
        public IEnumerator CleaningASectorTurnsItGreen()
        {
            yield return Load();
            var sector = s.Sectors[2]; // la pampa
            var terrain = s.Terrain;
            var probe = new Vector3(-50, 0, -50);
            float before = terrain.GrassAt(probe); int greenBefore = terrain.GreenTrees;
            foreach (var l in sector.Litter)
            {
                if (!bag.CanFit(l)) yield return EmptyBag();
                yield return Use(l);
                Assert.That(l.Collected, l.name);
            }
            foreach (var w in sector.Wood) yield return ClearWood(w);
            Assert.That(sector.IsClean);
            yield return new WaitForSeconds(3.6f);
            Assert.That(terrain.SectorLevel(2), Is.EqualTo(1f));
            Assert.That(terrain.GrassAt(probe), Is.GreaterThan(before + .4f), "El suelo del sector reverdece");
            Assert.That(terrain.GreenTrees, Is.GreaterThan(greenBefore), "Los árboles del sector reverdecen");
            Assert.That(terrain.SectorLevel(0) + terrain.SectorLevel(1) + terrain.SectorLevel(3), Is.Zero, "Los demás siguen secos");
            Assert.That(sector.RestoredDecor.activeSelf);
            Assert.That(s.IsRestored, Is.False, "El valle entero aún no");
        }

        [UnityTest]
        public IEnumerator LedgeStepsCanBeClimbedByJumping()
        {
            yield return Load();
            var steps = GameObject.Find("Salientes").GetComponentsInChildren<BoxCollider>().Select(b => b.transform).ToList();
            Assert.That(steps.Count, Is.EqualTo(8));
            for (int run = 0; run < 2; run++)
                for (int i = 0; i < 3; i++)
                {
                    var a = steps[run * 4 + i]; var b = steps[run * 4 + i + 1];
                    var topA = a.GetComponent<Collider>().bounds.max.y; var topB = b.GetComponent<Collider>().bounds.max.y;
                    var dir = (b.position - a.position); dir.y = 0; dir.Normalize();
                    motor.Teleport(new Vector3(a.position.x, topA + .05f, a.position.z) - dir * .4f, Quaternion.LookRotation(dir));
                    yield return Settle(.2f);
                    float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
                    bool first = true;
                    for (float t = 0; t < .9f; t += Time.deltaTime) { motor.Simulate(t < .45f ? Vector2.up : Vector2.zero, first, yaw, Time.deltaTime); first = false; yield return null; }
                    yield return Settle(.3f);
                    Assert.That(motor.transform.position.y, Is.GreaterThan(topB - .15f), $"Salta al escalón {i + 1} de la saliente {run + 1}");
                }
        }

        [UnityTest]
        public IEnumerator TrackingPulseMarksNearbyPendingItemsWithCooldown()
        {
            yield return Load();
            var pulse = motor.GetComponent<TrackingPulse>();
            var l = s.AllLitter.OrderBy(x => x.transform.position.sqrMagnitude).First();
            motor.Teleport(l.transform.position + Vector3.back * 6f, Quaternion.identity); yield return Settle();
            Assert.That(pulse.Pulse(), Is.True);
            Assert.That(pulse.LastMarked, Is.GreaterThan(0)); Assert.That(pulse.Visible, Is.EqualTo(pulse.LastMarked));
            Assert.That(pulse.Pulse(), Is.False, "Enfriamiento");
            Assert.That(pulse.CooldownLeft, Is.GreaterThan(15f));
        }

        [UnityTest]
        public IEnumerator CleaningEverythingRestoresTheValleyBringsTheFoxAndEndsTheGame()
        {
            yield return Load();
            var terrain = s.Terrain.GetComponent<Terrain>();
            foreach (var sector in s.Sectors)
            {
                foreach (var l in sector.Litter.OrderBy(x => x.transform.position.y))
                {
                    if (!bag.CanFit(l)) yield return EmptyBag();
                    yield return Use(l);
                    Assert.That(l.Collected, l.name);
                }
                foreach (var w in sector.Wood) yield return ClearWood(w);
            }
            Assert.That(s.Cleaned, Is.EqualTo(s.Total));
            Assert.That(s.IsRestored, Is.False, "Falta depositar lo de la bolsa");
            yield return EmptyBag();
            Assert.That(s.DepositedCount, Is.EqualTo(40));
            Assert.That(s.IsRestored, "Todo limpio y separado: el valle revive");
            var look = Object.FindFirstObjectByType<ValleWaterLook>();
            Assert.That(look, Is.Not.Null); Assert.That(look.Count, Is.GreaterThan(20), "Malla de agua en trozos");
            Assert.That(played.Contains("restore"));
            s.Fauna.TimeScale = 8f;
            for (float t = 0; t < 20f && !s.IsCompleted; t += Time.deltaTime) yield return null;
            Assert.That(s.IsCompleted); Assert.That(GameProgress.IsZoneComplete(GameProgress.Valle));
            Assert.That(look.Clarity, Is.GreaterThan(.5f), "El agua se aclara al revivir el valle");
            Assert.That(s.GameFinished);
            Assert.That(s.Medal, Is.EqualTo("oro").Or.EqualTo("plata").Or.EqualTo("bronce"));
            var pres = Presentation.Instance;
            for (float t = 0; t < 3f && !pres.Notebook.Visible; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(pres.Notebook.Visible, "Nota final del cuaderno");
            pres.Notebook.Continue(); yield return null; yield return null;
            Assert.That(pres.Ending.Visible, "Pantalla final");
        }

        [UnityTest]
        public IEnumerator RestartResetsTheWholeValley()
        {
            yield return Load();
            var sector = s.Sectors[3];
            foreach (var l in sector.Litter.Where(x => x.transform.position.y - s.Terrain.GetComponent<Terrain>().SampleHeight(x.transform.position) < 1f).Take(4)) yield return Use(l);
            yield return ClearWood(sector.Wood[0]);
            s.Pulse.Pulse();
            s.Restart(); yield return Settle();
            Assert.That(s.Cleaned, Is.Zero); Assert.That(bag.IsEmpty); Assert.That(s.DepositedCount, Is.Zero);
            Assert.That(s.AllLitter.All(l => !l.Collected && l.GetComponent<Collider>().enabled));
            Assert.That(s.AllWood.All(w => !w.Cleared && w.GetComponent<Collider>().enabled));
            Assert.That(s.ElapsedSeconds, Is.LessThan(.5f));
            Assert.That(s.Pulse.Ready);
            Assert.That(s.Terrain.SectorLevel(3), Is.Zero);
        }

        [UnityTest]
        public IEnumerator CompletingTheRefugeLoadsTheValley()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll(); GameProgress.CompleteZone(GameProgress.Arroyo); GameProgress.CompleteZone(GameProgress.Claro);
            SceneManager.LoadScene(GameFlow.RefugioScene);
            yield return null; yield return null;
            var r = Object.FindFirstObjectByType<RefugioSession>();
            var m = r.Player; m.GetComponent<ThirdPersonInput>().enabled = false;
            var a = m.GetComponent<PlayerInteractor>();
            var good = r.Spots.Where(x => x.Suitable).ToArray();
            for (int i = 0; i < r.Items.Length; i++)
            {
                m.Teleport(r.Items[i].transform.position + new Vector3(0, 0, -1.3f), Quaternion.identity); yield return null;
                Assert.That(a.TryInteract());
                m.Teleport(good[i].transform.position + new Vector3(0, 0, -1.3f), Quaternion.identity); yield return null;
                Assert.That(a.TryInteract());
            }
            var bed = r.GrowthPoints.First(p => p.Suitable);
            m.Teleport(bed.transform.position + new Vector3(0, 0, -1.2f), Quaternion.identity);
            yield return new WaitForSeconds(.7f);
            Assert.That(m.GetComponent<Bosque.Abilities.GrowthAbility>().TryGrow());
            r.Fauna.TimeScale = 8f;
            var pres = Presentation.Instance;
            for (float t = 0; t < 12f && !pres.Notebook.Visible; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(pres.Notebook.Visible, "Nota del refugio");
            Assert.That(pres.Ending.Visible, Is.False, "El final ya no es en el refugio");
            pres.Notebook.Continue();
            for (float t = 0; t < 8f && SceneManager.GetActiveScene().name != GameFlow.ValleScene; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameFlow.ValleScene), "Transición a la zona 4");
        }

        private ValleWater Water => Object.FindFirstObjectByType<ValleWater>();

        private float Ground(Vector3 p) => s.Terrain.GetComponent<Terrain>().SampleHeight(p);

        /// <summary>Camina con el motor real hacia un punto; devuelve el resultado al llegar o al agotar el tiempo.</summary>
        private IEnumerator Walk(Vector3 to, float seconds, System.Action<float> eachFrame = null)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                var d = to - motor.transform.position; d.y = 0;
                if (d.magnitude < 1f) yield break;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                motor.Simulate(Vector2.up, false, yaw, Time.deltaTime);
                eachFrame?.Invoke(t);
                yield return null;
            }
        }

        private static Vector2 Across(WaterStream st, Vector3 at)
        {
            ValleWater.Nearest(st, new Vector2(at.x, at.z), out _, out _, out _, out var flow, out _);
            return new Vector2(-flow.y, flow.x);
        }

        [UnityTest]
        public IEnumerator RiversLakesBridgesAndFordsFollowTheDesign()
        {
            yield return Load();
            var w = Water;
            Assert.That(w, Is.Not.Null);
            var names = w.Streams.Select(x => x.name).Where(n => !string.IsNullOrEmpty(n)).ToList();
            CollectionAssert.IsSupersetOf(names, new[] { "Río Mayu", "Río Chico", "Río de la Salida", "Río del Sur" });
            Assert.That(w.Lakes.Length, Is.EqualTo(4), "Tres lagunas encadenadas y un bofedal");
            Assert.That(w.Crossings.Count(c => c.bridge), Is.EqualTo(3)); Assert.That(w.Crossings.Count(c => !c.bridge), Is.EqualTo(4));
            foreach (var st in w.Streams)
                for (int i = 1; i < st.points.Length; i++)
                    Assert.That(st.points[i].y, Is.LessThanOrEqualTo(st.points[i - 1].y + .001f), $"{st.name}: el agua baja río abajo");
            var lakes = w.Lakes.Where(l => l.name.StartsWith("Laguna")).Select(l => l.center.y).ToArray();
            Assert.That(lakes[0] > lakes[1] && lakes[1] > lakes[2], "La cadena de lagunas desagua escalón a escalón");
            foreach (var c in w.Crossings.Where(c => c.bridge))
            {
                Assert.That(Physics.Raycast(c.position + Vector3.up * 6f, Vector3.down, out var hit, 10f), "Tablero del puente");
                Assert.That(hit.point.y, Is.GreaterThan(c.position.y + .3f), "El puente queda sobre el agua");
            }
            var look = Object.FindFirstObjectByType<ValleWaterLook>();
            Assert.That(look.Count, Is.GreaterThan(20));
            foreach (var b in s.Bins) Assert.That(w.DepthAt(b.transform.position), Is.Zero, b.Station + " en seco");
            Assert.That(w.DepthAt(motor.transform.position), Is.Zero, "Inicio en seco");
            int floating = 0;
            foreach (var l in s.AllLitter)
            {
                var p = l.transform.position; p.y = Ground(p);
                float d = w.DepthAt(p);
                Assert.That(d, Is.LessThan(.6f), l.name + " se alcanza vadeando");
                if (d > .15f) floating++;
            }
            Assert.That(floating, Is.GreaterThanOrEqualTo(6), "Residuos flotando en lagunas, ríos y bofedal");
            foreach (var wd in s.AllWood) { var p = wd.transform.position; p.y = Ground(p); Assert.That(w.DepthAt(p), Is.LessThan(.7f), wd.name); }
            Assert.That(s.AllWood.Count(wd => { var p = wd.transform.position; p.y = Ground(p); return w.DepthAt(p) > .2f; }), Is.GreaterThanOrEqualTo(4), "Troncos que tapan canales y ríos");
        }

        [UnityTest]
        public IEnumerator WadingSlowsDeepWaterBlocksAndFordsAndBridgesCross()
        {
            yield return Load();
            var w = Water; var mayu = w.Streams.First(x => x.name == "Río Mayu");
            var wading = motor.GetComponent<WaterWading>();
            // Agua honda: no se cruza.
            int idx = Enumerable.Range(0, mayu.points.Length).First(i => mayu.depth[i] > 1.45f && Mathf.Abs(mayu.points[i].z) < 80f && mayu.points[i].z > 20f &&
                                                                             w.Crossings.All(c => Vector3.Distance(c.position, mayu.points[i]) > 16f));
            var center = mayu.points[idx]; var side = Across(mayu, center);
            var start = center + new Vector3(side.x, 0, side.y) * (mayu.bank + 2f); start.y = Ground(start) + .05f;
            motor.Teleport(start, Quaternion.identity); yield return Settle();
            Assert.That(wading.Depth, Is.Zero);
            float maxDepth = 0, minFactor = 1;
            var far = center - new Vector3(side.x, 0, side.y) * (mayu.bank + 2f);
            yield return Walk(far, 5f, t => { maxDepth = Mathf.Max(maxDepth, wading.Depth); minFactor = Mathf.Min(minFactor, motor.SpeedFactor); });
            Assert.That(maxDepth, Is.LessThan(wading.MaxDepth + .08f), "No entra en lo hondo");
            Assert.That(minFactor, Is.LessThan(.9f), "Vadear frena");
            var flat = motor.transform.position - center; flat.y = 0;
            Assert.That(Vector2.Dot(new Vector2(flat.x, flat.z), side), Is.GreaterThan(0f), "Sigue en la orilla de partida");
            Assert.That(s.LastHint, Is.EqualTo(ValleSession.DeepWaterHint));
            Assert.That(s.Mistakes, Is.Zero, "El agua honda no cuenta como error");
            // Vado y puente: se cruzan.
            foreach (var c in w.Crossings)
            {
                var st = w.Streams.OrderBy(x => { ValleWater.Nearest(x, new Vector2(c.position.x, c.position.z), out float d, out _, out _, out _, out _); return d; }).First();
                var a = Across(st, c.position); float reach = st.bank + (c.bridge ? 4.5f : 2.5f);
                var from = c.position + new Vector3(a.x, 0, a.y) * reach; from.y = Ground(from) + .1f;
                var to = c.position - new Vector3(a.x, 0, a.y) * reach;
                motor.Teleport(from, Quaternion.identity); yield return Settle();
                yield return Walk(to, 16f);
                var rest = to - motor.transform.position; rest.y = 0;
                Assert.That(rest.magnitude, Is.LessThan(1.6f), $"{(c.bridge ? "Puente" : "Vado")} en {c.position} se cruza");
            }
        }

        [UnityTest]
        public IEnumerator CompassMinimapAndMapShowWhatWasFound()
        {
            yield return Load();
            var hud = s.GetComponent<ValleHud>(); var pres = Presentation.Instance;
            var item = s.AllLitter.OrderByDescending(x => (x.transform.position - motor.transform.position).sqrMagnitude).First(x => x.transform.position.y - Ground(x.transform.position) < 1f);
            yield return null;
            Assert.That(hud.IsDiscovered(item), Is.False, "Lo lejano no está ubicado");
            Assert.That(hud.MapMarkers.Any(m => m.kind == MarkerKind.Litter && m.position == item.transform.position), Is.False);
            var p = item.transform.position + Vector3.back * 3f; p.y = Mathf.Max(Ground(p), Water.SurfaceAt(p, out float ws) ? ws - .5f : 0f) + .1f;
            motor.Teleport(p, Quaternion.identity);
            for (float t = 0; t < .6f; t += Time.deltaTime) { motor.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null; }
            Assert.That(hud.IsDiscovered(item), "Visto de cerca: queda ubicado");
            Assert.That(hud.MapMarkers.Any(m => m.kind == MarkerKind.Litter && m.position == item.transform.position));
            var cam = Camera.main; cam.GetComponent<ThirdPersonCamera>().enabled = false;
            cam.transform.rotation = Quaternion.Euler(10, 90, 0);
            for (float t = 0; t < .6f; t += Time.unscaledDeltaTime) yield return null;
            var nav = pres.Navigation;
            Assert.That(nav.Visible, "Brújula y minimapa visibles en el valle");
            Assert.That(nav.HeadingName, Is.EqualTo("E"));
            Assert.That(nav.CompassMarkersShown, Is.GreaterThanOrEqualTo(4), "Tres puntos de reciclaje y lo ubicado");
            Assert.That(nav.MinimapMarkersShown, Is.GreaterThanOrEqualTo(1));
            Assert.That(nav.CaptionText, Does.Contain("ubicado"));
            pres.OpenMap(); yield return null; yield return null;
            Assert.That(pres.Map.MarkersShown, Is.GreaterThanOrEqualTo(3 + 4 + 8 + 7 + 1), "Mapa: reciclaje, sectores, ríos y lagunas, cruces y lo ubicado");
            pres.CloseMap();
            // El rastreo también ubica.
            var other = s.AllLitter.Where(x => !hud.IsDiscovered(x)).OrderBy(x => (x.transform.position - item.transform.position).sqrMagnitude).First();
            var q = other.transform.position + Vector3.back * 30f; q.y = Ground(q) + .1f;
            if (Water.DepthAt(q) == 0f)
            {
                motor.Teleport(q, Quaternion.identity); yield return Settle();
                Assert.That(s.Pulse.Pulse());
                Assert.That(hud.IsDiscovered(other), "El rastreo (T) ubica lo pendiente cercano");
            }
            s.Restart(); yield return null;
            Assert.That(hud.IsDiscovered(item), Is.False, "Reiniciar olvida lo ubicado");
        }

        [UnityTest]
        public IEnumerator CaptureTheValley()
        {
            yield return Load();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Valle/CAPTURAS"));
            Directory.CreateDirectory(dir);
            var cam = Camera.main; var follow = cam.GetComponent<ThirdPersonCamera>(); follow.enabled = false;
            var terrain = s.Terrain.GetComponent<Terrain>();
            Vector3 G(float x, float z, float up) => new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)) + up, z);
            IEnumerator Shot(string name, Vector3 pos, Vector3 look)
            {
                yield return null; yield return null;
                cam.transform.position = pos; cam.transform.LookAt(look);
                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
                foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + .05f + (100 - c.sortingOrder) * .0005f; }
                var rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt; Canvas.ForceUpdateCanvases(); cam.Render(); RenderTexture.active = rt;
                var img = new Texture2D(1920, 1080, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); img.Apply();
                File.WriteAllBytes(Path.Combine(dir, "valle_" + name + ".jpg"), img.EncodeToJPG(88));
                foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
                cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.Destroy(rt); Object.Destroy(img);
            }
            yield return new WaitForSecondsRealtime(4.5f);
            yield return Shot("01_vista_general_degradado", G(0, -40, 26), G(0, 0, 6));
            yield return Shot("02_campamento_reciclaje", G(-4, -2, 3), G(0, 8, 1));
            yield return Shot("03_quebrada", G(10, 50, 4), G(30, 46, 0));
            yield return Shot("04_bosque", G(-30, 30, 3), G(-50, 50, 2));
            yield return Shot("05_basural_pampa", G(-46, -56, 3.5f), G(-55, -64, 0));
            yield return Shot("06_mirador_salientes", G(74, -42, 9), G(84, -31, 2));
            var l = s.Sectors[2].Litter.OrderBy(x => (x.transform.position - new Vector3(-72, 0, -52)).sqrMagnitude).First(); // el basural, a campo abierto
            motor.Teleport(l.transform.position + new Vector3(2f, .2f, -1.5f), Quaternion.identity); yield return Settle(.3f);
            follow.enabled = true; follow.Configure(motor.transform); yield return Settle(.3f); actor.Scan(); follow.enabled = false;
            yield return Shot("07_hud_residuo", cam.transform.position, motor.transform.position + Vector3.up);
            Presentation.Instance.OpenMap(); yield return Shot("08_mapa", cam.transform.position, motor.transform.position + Vector3.up); Presentation.Instance.CloseMap();
            yield return Shot("10_puente_mayu", G(18, 2, 4), G(32, 9, 0));
            yield return Shot("11_lagunas", G(44, 92, 9), G(80, 58, 0));
            yield return Shot("12_vado_mayu", G(22, -36, 3), G(21, -45, 0));
            yield return Shot("13_afluente_chico", G(-40, -6, 4), G(-16, -23, 0));
            yield return Shot("14_rio_del_sur_bofedal", G(-20, -30, 5), G(-44, -60, 0));
            // Sector de la pampa limpio para ver el contraste.
            foreach (var x in s.Sectors[2].Litter) { if (!bag.CanFit(x)) bag.Clear(); bag.Add(x); x.SetCollected(true); }
            foreach (var w in s.Sectors[2].Wood) { w.BeginWork(actor); }
            s.Terrain.RestoreSector(2); s.Sectors[2].RestoredDecor.SetActive(true);
            yield return new WaitForSeconds(3.6f);
            yield return Shot("09_pampa_limpia_vs_resto", G(-8, -20, 14), G(-45, -55, 0));
            Assert.Pass(dir);
        }
    }
}
