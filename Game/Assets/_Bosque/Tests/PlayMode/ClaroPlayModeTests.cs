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
    /// <summary>Zona 2, "El claro vacío", y su conexión con el arroyo, sobre las escenas reales.</summary>
    public sealed class ClaroPlayModeTests
    {
        private ClaroSession session;
        private ThirdPersonMotor motor;
        private ThirdPersonInput input;
        private GrowthAbility ability;
        private SfxPlayer sfx;
        private AmbienceController ambience;
        private readonly List<string> played = new List<string>();

        private IEnumerator LoadClaro(bool unlocked)
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll();
            if (unlocked) GameProgress.UnlockGrowth();
            SceneManager.LoadScene("ClaroVacio");
            yield return null; yield return null;
            session = Object.FindFirstObjectByType<ClaroSession>();
            motor = session.Player; input = motor.GetComponent<ThirdPersonInput>(); input.enabled = false;
            ability = motor.GetComponent<GrowthAbility>();
            sfx = Object.FindFirstObjectByType<SfxPlayer>(); ambience = Object.FindFirstObjectByType<AmbienceController>();
            played.Clear(); sfx.Played += played.Add;
        }

        [UnityTearDown] public IEnumerator TearDown() { if (sfx != null) sfx.Played -= played.Add; GameProgress.ResetAll(); yield return null; }

        private IEnumerator Run(Vector2 dir, float seconds, bool jumpFirst = false)
        {
            bool first = true;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            { input.enabled = false; motor.Simulate(dir, jumpFirst && first, 0, Time.deltaTime); first = false; yield return null; }
        }

        private IEnumerator StandAt(GrowthPoint p)
        {
            motor.Teleport(p.transform.position + new Vector3(0, 0, -1.2f), Quaternion.identity);
            yield return Run(Vector2.zero, .2f);
            yield return new WaitForSeconds(.65f); // enfriamiento de la habilidad
        }

        private GrowthPoint[] Humid => session.Points.Where(p => p.Suitable).ToArray();
        private GrowthPoint[] Dry => session.Points.Where(p => !p.Suitable).ToArray();

        [UnityTest]
        public IEnumerator GrowthIsLockedUntilTheStreamIsRestored()
        {
            yield return LoadClaro(false);
            yield return StandAt(Humid[0]);
            Assert.That(ability.Unlocked, Is.False);
            Assert.That(ability.TryGrow(), Is.False, "Sin completar el arroyo no hay crecimiento");
            Assert.That(Humid[0].Grown, Is.False);
            Assert.That(session.Points.Length, Is.EqualTo(5)); Assert.That(Humid.Length, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator DrySoilGivesAHintAndNoProgress()
        {
            yield return LoadClaro(true);
            yield return StandAt(Dry[0]);
            Assert.That(ability.Target, Is.EqualTo(Dry[0]));
            Assert.That(ability.TryGrow(), Is.False);
            Assert.That(session.FailedAttempts, Is.EqualTo(1));
            Assert.That(session.LastHint, Is.Not.Empty, "Pista tras un intento incorrecto");
            Assert.That(session.Progress.RemovedCount, Is.Zero);
            Assert.That(Dry[0].Plant.activeSelf, Is.False);
            Assert.That(played.Count(p => p == "grow.fail"), Is.EqualTo(1));
            Assert.That(ability.TryGrow(), Is.False); // enfriamiento: no repite pista ni sonido
            Assert.That(played.Count(p => p == "grow.fail"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HumidBedsGrowOnceRestoreTheClearingAndGrowTheBridge()
        {
            yield return LoadClaro(true);
            Assert.That(session.RootBridge.activeSelf, Is.False, "Sin puente antes de restaurar");
            foreach (var p in Humid)
            {
                yield return StandAt(p);
                Assert.That(ability.TryGrow(), Is.True, "Brota en " + p.name);
                yield return new WaitForSeconds(.7f);
                Assert.That(ability.TryGrow(), Is.False, "No crece dos veces en el mismo lecho");
                Assert.That(p.Plant.activeSelf, Is.True);
            }
            Assert.That(session.Progress.IsRestored);
            Assert.That(session.RootBridge.activeSelf && session.RootBridge.GetComponent<Collider>().enabled, "Puente de raíces con colisión");
            Assert.That(played.Count(p => p == "grow"), Is.EqualTo(3));
            Assert.That(played.Count(p => p == "bridge"), Is.EqualTo(1));
            Assert.That(played.Count(p => p == "restore"), Is.EqualTo(1));
            yield return Run(Vector2.zero, 2f);
            Assert.That(ambience.Blend, Is.GreaterThan(.2f), "El ambiente cambia de forma progresiva");
        }

        [UnityTest]
        public IEnumerator TheRavineCannotBeJumpedAndFallingReturnsToTheSafePoint()
        {
            yield return LoadClaro(true);
            motor.Teleport(new Vector3(0, 0, 17.5f), Quaternion.identity); yield return Run(Vector2.zero, .2f);
            yield return Run(Vector2.up, .55f);
            yield return Run(Vector2.up, 3f, true);
            Assert.That(motor.transform.position.z, Is.LessThan(19.5f), "No cruza la barranca saltando: vuelve al punto seguro");
            Assert.That(motor.transform.position.y, Is.GreaterThan(-1f));
        }

        [UnityTest]
        public IEnumerator TheBridgeLeadsToTheExitAndCompletesTheZone()
        {
            yield return LoadClaro(true);
            foreach (var p in Humid) { yield return StandAt(p); ability.TryGrow(); }
            yield return Run(Vector2.zero, 2.8f); // crecimiento del puente
            motor.Teleport(new Vector3(0, 0, 15f), Quaternion.identity); yield return Run(Vector2.zero, .2f);
            for (float t = 0; t < 8f && !session.Progress.IsCompleted; t += Time.deltaTime)
            { input.enabled = false; motor.Simulate(Vector2.up, false, 0, Time.deltaTime); yield return null; }
            Assert.That(session.Progress.IsCompleted, "Cruza el puente y llega al arco");
            Assert.That(GameProgress.IsZoneComplete(GameProgress.Claro));
            Assert.That(played.Count(p => p == "complete"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RestartResetsBedsBridgeAndAmbience()
        {
            yield return LoadClaro(true);
            foreach (var p in Humid) { yield return StandAt(p); ability.TryGrow(); }
            yield return Run(Vector2.zero, 1f);
            session.Restart(); yield return Run(Vector2.zero, 1.6f);
            Assert.That(session.Progress.RemovedCount, Is.Zero);
            Assert.That(session.Points.All(p => !p.Grown && !p.Plant.activeSelf));
            Assert.That(session.RootBridge.activeSelf, Is.False);
            Assert.That(ambience.Blend, Is.EqualTo(0f).Within(.01f));
            yield return StandAt(Humid[0]);
            Assert.That(ability.TryGrow(), Is.True, "Se puede volver a jugar");
        }

        [UnityTest]
        public IEnumerator CompletingTheStreamUnlocksGrowthAndLoadsTheClearing()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll(); ArroyoZoneTransition.SuppressSceneLoad = false;
            SceneManager.LoadScene("ArroyoPlayable");
            yield return null; yield return null;
            var arroyo = Object.FindFirstObjectByType<ArroyoSession>();
            var m = arroyo.Player; var inp = m.GetComponent<ThirdPersonInput>(); var actor = m.GetComponent<PlayerInteractor>();
            foreach (int z in new[] { 10, 18, 26 })
            {
                m.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); inp.enabled = false; yield return null;
                Assert.That(actor.TryInteract(), Is.True);
            }
            m.Teleport(new Vector3(3, 0, 33), Quaternion.identity);
            for (int i = 0; i < 120 && !arroyo.Progress.IsCompleted; i++) { inp.enabled = false; m.Simulate(Vector2.up, false, 0, Time.deltaTime); yield return null; }
            Assert.That(arroyo.Progress.IsCompleted);
            Assert.That(GameProgress.GrowthUnlocked, "Completar el arroyo concede el crecimiento");
            for (float t = 0; t < 7f && SceneManager.GetActiveScene().name != "ClaroVacio"; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ClaroVacio"), "Transición a la zona 2");
            yield return null;
            Assert.That(Object.FindFirstObjectByType<GrowthAbility>().Unlocked, "La habilidad sigue disponible en el claro");
        }

        [UnityTest]
        public IEnumerator CaptureTheClearing()
        {
            yield return LoadClaro(true);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Zonas/Claro/CAPTURAS"));
            Directory.CreateDirectory(dir);
            var cam = Object.FindFirstObjectByType<Camera>(); var follow = cam.GetComponent<ThirdPersonCamera>(); follow.enabled = false;
            IEnumerator Shot(string name, Vector3 pos, Vector3 look)
            {
                yield return null;
                cam.transform.position = pos; cam.transform.LookAt(look);
                var rt = new RenderTexture(1600, 900, 24); var prev = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var img = new Texture2D(1600, 900, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); img.Apply();
                File.WriteAllBytes(Path.Combine(dir, "claro_" + name + ".jpg"), img.EncodeToJPG(90));
                cam.targetTexture = prev; RenderTexture.active = null; rt.Release(); Object.Destroy(rt); Object.Destroy(img);
            }
            motor.Teleport(new Vector3(0, 0, -1.5f), Quaternion.identity); yield return Run(Vector2.zero, .5f);
            yield return Shot("01_degradado_vista", new Vector3(0, 6f, -6f), new Vector3(0, 0, 16f));
            yield return Shot("02_manantial_y_lechos", new Vector3(-3f, 2.6f, 0f), new Vector3(-8f, 0, 8f));
            yield return Shot("03_barranca", new Vector3(3f, 2.4f, 14f), new Vector3(0, -1f, 22f));
            yield return StandAt(Dry[0]); ability.TryGrow();
            yield return Shot("04_suelo_seco_pista", motor.transform.position + new Vector3(2.2f, 2f, -2.4f), Dry[0].transform.position);
            foreach (var p in Humid) { yield return StandAt(p); ability.TryGrow(); }
            yield return Run(Vector2.zero, .7f);
            yield return Shot("05_brote_creciendo", Humid[2].transform.position + new Vector3(2.4f, 1.8f, -2.6f), Humid[2].transform.position + Vector3.up * .6f);
            yield return Run(Vector2.zero, 6f);
            yield return Shot("06_restaurado_vista", new Vector3(0, 6f, -6f), new Vector3(0, 0, 16f));
            yield return Shot("07_puente_raices", new Vector3(4.5f, 2.2f, 17f), new Vector3(0, 0, 22.5f));
            motor.Teleport(new Vector3(0, 0, 26f), Quaternion.identity); yield return Run(Vector2.zero, .3f);
            follow.enabled = true; follow.Configure(motor.transform); yield return Run(Vector2.up, .6f);
            follow.enabled = false;
            yield return Shot("08_juego_hacia_salida", cam.transform.position, motor.transform.position + Vector3.up * 1.4f);
            Assert.Pass(dir);
        }
    }
}
