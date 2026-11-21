using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bosque.Abilities;
using Bosque.Audio;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bosque.Tests.PlayMode
{
    /// <summary>Zona 3, "El refugio perdido", el regreso de la fauna y el final, sobre las escenas reales.</summary>
    public sealed class RefugioPlayModeTests
    {
        private RefugioSession session;
        private ThirdPersonMotor motor;
        private ThirdPersonInput input;
        private PlayerInteractor actor;
        private ItemCarrier carrier;
        private GrowthAbility ability;
        private SfxPlayer sfx;
        private AmbienceController ambience;
        private readonly List<string> played = new List<string>();

        private IEnumerator LoadRefugio()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll();
            GameProgress.CompleteZone(GameProgress.Arroyo); GameProgress.CompleteZone(GameProgress.Claro);
            SceneManager.LoadScene("RefugioPerdido");
            yield return null; yield return null;
            session = Object.FindFirstObjectByType<RefugioSession>();
            motor = session.Player; input = motor.GetComponent<ThirdPersonInput>(); input.enabled = false;
            actor = motor.GetComponent<PlayerInteractor>(); carrier = motor.GetComponent<ItemCarrier>();
            ability = motor.GetComponent<GrowthAbility>();
            sfx = Object.FindFirstObjectByType<SfxPlayer>(); ambience = Object.FindFirstObjectByType<AmbienceController>();
            played.Clear(); sfx.Played += played.Add;
        }

        [UnityTearDown] public IEnumerator TearDown() { if (sfx != null) sfx.Played -= played.Add; GameProgress.ResetAll(); yield return null; }

        private IEnumerator Run(Vector2 dir, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            { if (input.enabled) input.enabled = false; motor.Simulate(dir, false, 0, Time.deltaTime); yield return null; }
        }

        private IEnumerator StandNear(Vector3 target, float back = 1.3f)
        {
            motor.Teleport(target + new Vector3(0, 0, -back), Quaternion.identity);
            yield return Run(Vector2.zero, .15f);
        }

        private RefugeItem Item(string id) => session.Items.First(i => i.ItemId == "refugio." + id);
        private RefugeSpot[] Good => session.Spots.Where(s => s.Suitable).ToArray();
        private RefugeSpot[] Bad => session.Spots.Where(s => !s.Suitable).ToArray();
        private GrowthPoint Humid => session.GrowthPoints.First(p => p.Suitable);
        private GrowthPoint Dry => session.GrowthPoints.First(p => !p.Suitable);

        private IEnumerator CarryTo(RefugeItem item, RefugeSpot spot)
        {
            yield return StandNear(item.transform.position);
            Assert.That(actor.TryInteract(), Is.True, "Recoge " + item.name);
            Assert.That(carrier.Holding, Is.EqualTo(item));
            yield return StandNear(spot.transform.position);
            Assert.That(actor.TryInteract(), Is.True, "Coloca " + item.name + " en " + spot.name);
        }

        private IEnumerator GrowShrub()
        {
            yield return StandNear(Humid.transform.position, 1.2f);
            yield return new WaitForSeconds(.65f);
            Assert.That(ability.TryGrow(), Is.True, "Crece el matorral");
        }

        private static float DistanceToPolyline(Vector3 p, Transform[] pts)
        {
            float best = float.MaxValue;
            for (int i = 1; i < pts.Length; i++)
            {
                var a = pts[i - 1].position; var ab = pts[i].position - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector3.Distance(p, a + ab * t));
            }
            return best;
        }

        [UnityTest]
        public IEnumerator LayoutPutsGoodSpotsOnTheAnimalPathAndDecoysInTheOpen()
        {
            yield return LoadRefugio();
            Assert.That(session.Items.Length, Is.EqualTo(2));
            Assert.That(session.Spots.Length, Is.EqualTo(4)); Assert.That(Good.Length, Is.EqualTo(2));
            Assert.That(session.Progress.TotalCount, Is.EqualTo(3), "Tronco, piedras y matorral");
            Assert.That(session.Fauna.Animal.activeSelf, Is.False, "El animal no aparece hasta recuperar el refugio");
            var path = session.Fauna.Waypoints;
            foreach (var s in Good)
            {
                float d = DistanceToPolyline(s.transform.position, path);
                Assert.That(d, Is.InRange(1.0f, 2.2f), s.name + " junto al paso, sin bloquearlo");
            }
            foreach (var s in Bad) Assert.That(DistanceToPolyline(s.transform.position, path), Is.GreaterThan(5f), s.name + " lejos del paso");
            Assert.That(ability.Unlocked, "El crecimiento sigue disponible en la zona 3");
        }

        [UnityTest]
        public IEnumerator AnExposedSpotGivesAHintAndTheGuardianKeepsTheItem()
        {
            yield return LoadRefugio();
            var log = Item("tronco");
            yield return StandNear(log.transform.position);
            Assert.That(actor.Target, Is.EqualTo(log)); Assert.That(log.Prompt, Does.Contain("tronco"));
            Assert.That(actor.TryInteract(), Is.True);
            Assert.That(carrier.Holding, Is.EqualTo(log)); Assert.That(motor.SpeedFactor, Is.LessThan(1f), "Cargar ralentiza un poco");
            Assert.That(played.Count(p => p == "interact"), Is.EqualTo(1));
            yield return StandNear(Bad[0].transform.position);
            Assert.That(actor.Target, Is.EqualTo(Bad[0]));
            Assert.That(actor.TryInteract(), Is.False);
            Assert.That(session.FailedAttempts, Is.EqualTo(1)); Assert.That(session.LastHint, Is.Not.Empty);
            Assert.That(carrier.Holding, Is.EqualTo(log), "Conserva el elemento para probar en otro sitio");
            Assert.That(session.Progress.RemovedCount, Is.Zero);
            Assert.That(played.Count(p => p == "grow.fail"), Is.EqualTo(1));
            yield return StandNear(Dry.transform.position, 1.2f); yield return new WaitForSeconds(.65f);
            Assert.That(ability.TryGrow(), Is.False, "El lecho seco no admite el matorral");
            Assert.That(session.FailedAttempts, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DroppingAndFallingKeepTheItemsRecoverable()
        {
            yield return LoadRefugio();
            var rocks = Item("piedras"); var home = rocks.transform.position;
            yield return StandNear(home);
            Assert.That(actor.TryInteract(), Is.True);
            yield return Run(Vector2.up, .6f);
            Assert.That(carrier.Drop(), Is.True);
            Assert.That(carrier.Holding, Is.Null); Assert.That(motor.SpeedFactor, Is.EqualTo(1f));
            Assert.That(rocks.transform.position.y, Is.EqualTo(0f).Within(.05f), "Queda apoyado en el suelo");
            Assert.That(rocks.AtHome, Is.False);
            yield return StandNear(rocks.transform.position);
            Assert.That(actor.TryInteract(), Is.True, "Se puede volver a recoger");
            motor.Teleport(new Vector3(0, -6f, 5f), Quaternion.identity);
            yield return Run(Vector2.zero, .2f);
            Assert.That(carrier.Holding, Is.Null, "Al caer se suelta");
            Assert.That(rocks.AtHome, "Y vuelve a su sitio");
        }

        [UnityTest]
        public IEnumerator RefugeRestoresTheZoneAndTheFoxWalksToTheDenThenTheGameEnds()
        {
            yield return LoadRefugio();
            yield return CarryTo(Item("tronco"), Good[0]);
            Assert.That(Good[0].Occupant, Is.Not.Null);
            yield return StandNear(Item("piedras").transform.position);
            Assert.That(actor.TryInteract(), Is.True);
            yield return StandNear(Good[0].transform.position);
            Assert.That(Good[0].CanInteract(actor), Is.False, "Un lugar ocupado no admite otro elemento");
            yield return StandNear(Good[1].transform.position);
            Assert.That(actor.TryInteract(), Is.True);
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(2)); Assert.That(session.Progress.IsRestored, Is.False);
            Assert.That(played.Count(p => p == "remove.log"), Is.EqualTo(1)); Assert.That(played.Count(p => p == "remove.rocks"), Is.EqualTo(1));
            yield return GrowShrub();
            Assert.That(session.Progress.IsRestored);
            Assert.That(played.Count(p => p == "restore"), Is.EqualTo(1));
            Assert.That(input.enabled, Is.False, "Durante el regreso del animal no se controla a la Guardiana");

            var fauna = session.Fauna; fauna.TimeScale = 4f;
            for (float t = 0; t < 4f && !fauna.Running; t += Time.deltaTime) yield return null;
            Assert.That(fauna.Running && fauna.Animal.activeSelf, "El zorro aparece y empieza a caminar");
            Assert.That(session.Cinematic.Active, "La cámara acompaña al animal");
            var anim = fauna.Animal.GetComponentInChildren<Animator>();
            float maxSpeed = 0, maxOffPath = 0, minY = 9, maxY = -9;
            for (float t = 0; t < 20f && !fauna.Arrived; t += Time.deltaTime)
            {
                var p = fauna.Animal.transform.position;
                maxSpeed = Mathf.Max(maxSpeed, anim.GetFloat("Speed"));
                maxOffPath = Mathf.Max(maxOffPath, DistanceToPolyline(p, fauna.Waypoints));
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                yield return null;
            }
            Assert.That(fauna.Arrived, "Llega a la madriguera");
            Assert.That(maxSpeed, Is.GreaterThan(1f), "La animación de paso se activa");
            Assert.That(anim.GetFloat("Speed"), Is.EqualTo(0f), "Al llegar queda en reposo");
            Assert.That(maxOffPath, Is.LessThan(.8f), "Sigue el recorrido predefinido");
            Assert.That(minY, Is.GreaterThan(-.05f)); Assert.That(maxY, Is.LessThan(.15f), "Camina sobre el suelo");
            Assert.That(Vector3.Distance(fauna.Animal.transform.position, fauna.Waypoints.Last().position), Is.LessThan(.15f));
            yield return null;
            Assert.That(session.Progress.IsCompleted); Assert.That(GameProgress.IsZoneComplete(GameProgress.Refugio));
            Assert.That(session.GameFinished, "Con las tres zonas se muestra el final");
            Assert.That(played.Count(p => p == "complete"), Is.EqualTo(1));
            yield return Run(Vector2.zero, 2f);
            Assert.That(ambience.Blend, Is.GreaterThan(.2f));
        }

        [UnityTest]
        public IEnumerator TheEndingNeedsAllThreeZones()
        {
            yield return LoadRefugio();
            GameProgress.ResetAll(); GameProgress.UnlockGrowth(); // como si se entrara sin haber completado el claro
            yield return CarryTo(Item("tronco"), Good[0]);
            yield return CarryTo(Item("piedras"), Good[1]);
            yield return GrowShrub();
            session.Fauna.TimeScale = 8f;
            for (float t = 0; t < 15f && !session.Progress.IsCompleted; t += Time.deltaTime) yield return null;
            Assert.That(session.Progress.IsCompleted);
            Assert.That(session.GameFinished, Is.False, "Sin el arroyo y el claro no hay final");
        }

        [UnityTest]
        public IEnumerator RestartResetsItemsSpotsShrubFoxAndCamera()
        {
            yield return LoadRefugio();
            yield return CarryTo(Item("tronco"), Good[0]);
            yield return CarryTo(Item("piedras"), Good[1]);
            yield return GrowShrub();
            session.Fauna.TimeScale = 6f;
            for (float t = 0; t < 4f && !session.Fauna.Running; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(1f);
            session.Restart(); yield return null;
            Assert.That(session.Progress.RemovedCount, Is.Zero);
            Assert.That(session.Items.All(i => i.AtHome), "Elementos en su sitio");
            Assert.That(session.Spots.All(s => s.Occupant == null));
            Assert.That(Humid.Grown, Is.False); Assert.That(Humid.Plant.activeSelf, Is.False);
            Assert.That(session.Fauna.Animal.activeSelf, Is.False); Assert.That(session.Fauna.Running, Is.False);
            Assert.That(session.Cinematic.Active, Is.False);
            Assert.That(Object.FindFirstObjectByType<ThirdPersonCamera>().enabled, "La cámara de juego vuelve");
            Assert.That(input.enabled, "Se recupera el control");
            yield return CarryTo(Item("tronco"), Good[1]);
        }

        [UnityTest]
        public IEnumerator CompletingTheClearingLoadsTheRefuge()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll(); GameProgress.UnlockGrowth(); ArroyoZoneTransition.SuppressSceneLoad = false;
            SceneManager.LoadScene("ClaroVacio");
            yield return null; yield return null;
            var claro = Object.FindFirstObjectByType<ClaroSession>();
            var m = claro.Player; var inp = m.GetComponent<ThirdPersonInput>(); var grow = m.GetComponent<GrowthAbility>();
            foreach (var p in claro.Points.Where(p => p.Suitable))
            {
                m.Teleport(p.transform.position + new Vector3(0, 0, -1.2f), Quaternion.identity); inp.enabled = false;
                yield return new WaitForSeconds(.7f);
                Assert.That(grow.TryGrow(), Is.True);
            }
            for (float t = 0; t < 2.8f; t += Time.deltaTime) { inp.enabled = false; m.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null; }
            m.Teleport(new Vector3(0, 0, 15f), Quaternion.identity);
            for (float t = 0; t < 8f && !claro.Progress.IsCompleted; t += Time.deltaTime) { inp.enabled = false; m.Simulate(Vector2.up, false, 0, Time.deltaTime); yield return null; }
            Assert.That(claro.Progress.IsCompleted);
            Assert.That(GameProgress.IsZoneComplete(GameProgress.Claro));
            for (float t = 0; t < 7f && SceneManager.GetActiveScene().name != "RefugioPerdido"; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("RefugioPerdido"), "Transición a la zona 3");
        }

        [UnityTest]
        public IEnumerator CaptureTheRefuge()
        {
            yield return LoadRefugio();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Zonas/Refugio/CAPTURAS"));
            Directory.CreateDirectory(dir);
            var cam = Object.FindFirstObjectByType<Camera>(); var follow = cam.GetComponent<ThirdPersonCamera>(); follow.enabled = false;
            IEnumerator Shot(string name, Vector3 pos, Vector3 look)
            {
                yield return null;
                if (pos != Vector3.zero) { cam.transform.position = pos; cam.transform.LookAt(look); }
                var rt = new RenderTexture(1600, 900, 24); var prev = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var img = new Texture2D(1600, 900, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); img.Apply();
                File.WriteAllBytes(Path.Combine(dir, "refugio_" + name + ".jpg"), img.EncodeToJPG(90));
                cam.targetTexture = prev; RenderTexture.active = null; rt.Release(); Object.Destroy(rt); Object.Destroy(img);
            }
            yield return Run(Vector2.zero, .5f);
            yield return Shot("01_llegada_por_el_puente", new Vector3(1.5f, 2.4f, -9f), new Vector3(0, .5f, 8f));
            yield return Shot("02_degradado_vista", new Vector3(0, 7f, 4f), new Vector3(0, 0, 27f));
            yield return Shot("03_huellas_y_lugar", new Vector3(-6f, 2f, 22f), Good[0].transform.position);
            yield return Shot("04_tronco_hueco", Item("tronco").transform.position + new Vector3(2f, 1.4f, -2.2f), Item("tronco").transform.position);
            yield return StandNear(Item("tronco").transform.position); actor.TryInteract();
            follow.enabled = true; follow.Configure(motor.transform); yield return Run(new Vector2(0, 1), .8f); follow.enabled = false;
            yield return Shot("05_guardiana_lleva_tronco", cam.transform.position, motor.transform.position + Vector3.up * 1.2f);
            yield return StandNear(Bad[0].transform.position); actor.TryInteract();
            yield return Shot("06_lugar_expuesto_pista", motor.transform.position + new Vector3(2.4f, 2f, -2.4f), Bad[0].transform.position);
            yield return StandNear(Good[0].transform.position); actor.TryInteract();
            yield return CarryTo(Item("piedras"), Good[1]);
            yield return Shot("07_refugio_colocado", Good[1].transform.position + new Vector3(-2.6f, 1.6f, -2.2f), Good[1].transform.position);
            yield return GrowShrub();
            session.Fauna.TimeScale = 1f;
            for (float t = 0; t < 4f && !session.Fauna.Running; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(5f);
            yield return Shot("08_zorro_cruza_corredor", Vector3.zero, Vector3.zero);
            yield return Shot("09_zorro_de_cerca", session.Fauna.Animal.transform.position + session.Fauna.Animal.transform.right * 1.6f + new Vector3(0, .6f, .4f), session.Fauna.Animal.transform.position + Vector3.up * .3f);
            session.Fauna.TimeScale = 8f;
            for (float t = 0; t < 10f && !session.Fauna.Arrived; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(1f);
            yield return Shot("10_restaurado_madriguera", new Vector3(6f, 2.2f, 27f), session.Fauna.Animal.transform.position + Vector3.up * .3f);
            Assert.Pass(dir);
        }
    }
}
