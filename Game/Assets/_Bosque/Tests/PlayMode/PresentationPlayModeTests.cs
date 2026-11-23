using System.Collections;
using System.IO;
using System.Linq;
using Bosque.Audio;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.UI;
using Bosque.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Bosque.Tests.PlayMode
{
    /// <summary>Bloque 05: menú principal, introducción, pausa, opciones, HUD uGUI, cuaderno y final.</summary>
    public sealed class PresentationPlayModeTests
    {
        private static Presentation P => Presentation.Instance;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll(); GamePause.Set(false); ArroyoZoneTransition.SuppressSceneLoad = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GamePause.Set(false); GameProgress.ResetAll();
            if (P != null) { P.Notebook.Hide(); P.Ending.Hide(); P.Pause.Hide(); }
            yield return null;
        }

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null; yield return null;
        }

        private static IEnumerator WaitScene(string scene, float seconds = 6f)
        {
            for (float t = 0; t < seconds && (SceneManager.GetActiveScene().name != scene || P.Fading); t += Time.unscaledDeltaTime) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
        }

        private static Button FindButton(string label) =>
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.isActiveAndEnabled && b.GetComponentInChildren<TextMeshProUGUI>()?.text == label);

        [UnityTest]
        public IEnumerator PresentationLayerExistsWithThemeAndFonts()
        {
            yield return Load(GameFlow.MenuScene);
            Assert.That(Ui.Theme, Is.Not.Null, "Tema de interfaz en Resources");
            Assert.That(Ui.Theme.body != null && Ui.Theme.title != null && Ui.Theme.handwriting != null, "Tipografías");
            foreach (var f in new[] { Ui.Theme.body, Ui.Theme.bold, Ui.Theme.title, Ui.Theme.handwriting })
                Assert.That(f.HasCharacters("áéíóúñÑ¿¡·—", out uint[] missing, false, true), f.name + " sin " + (missing == null ? 0 : missing.Length) + " glifos");
            Assert.That(Ui.Theme.credits.text, Does.Contain("tomkranis"), "Créditos con la atribución CC-BY del zorro");
            Assert.That(P, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Presentation>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MainMenuIntroStartsANewGameInTheStream()
        {
            GameProgress.CompleteZone(GameProgress.Arroyo); // una partida anterior no debe arrastrarse
            yield return Load(GameFlow.MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenu>();
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), "Cursor libre en el menú");
            foreach (var label in new[] { "Comenzar", "Opciones", "Controles", "Créditos", "Salir" })
                Assert.That(FindButton(label), Is.Not.Null, "Botón " + label);
            Assert.That(P.InZone, Is.False, "Sin HUD de zona en el menú");
            FindButton("Opciones").onClick.Invoke(); yield return null;
            Assert.That(menu.PanelOpen); Assert.That(FindButton("Comenzar"), Is.Null);
            menu.Close(); yield return null;
            FindButton("Comenzar").onClick.Invoke(); yield return null;
            Assert.That(menu.IntroVisible);
            for (int i = 0; i < MainMenu.IntroPages.Length; i++) { Assert.That(menu.IntroPage, Is.EqualTo(i)); menu.NextPage(); yield return null; }
            yield return WaitScene(GameFlow.ArroyoScene);
            Assert.That(GameProgress.CompletedCount, Is.Zero, "Partida nueva");
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(P.TitleCard.Visible, "Rótulo de la zona 1");
            Assert.That(P.InZone);
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheGameAndResumes()
        {
            yield return Load(GameFlow.ArroyoScene);
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            P.OpenPause(); yield return null;
            Assert.That(GamePause.IsPaused); Assert.That(Time.timeScale, Is.EqualTo(0f)); Assert.That(AudioListener.pause);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            foreach (var label in new[] { "Continuar", "Reiniciar zona", "Opciones", "Controles", "Menú principal", "Salir del juego" })
                Assert.That(FindButton(label), Is.Not.Null, "Botón " + label);
            var before = session.Player.transform.position;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(session.Player.transform.position, Is.EqualTo(before), "Nada se mueve en pausa");
            FindButton("Controles").onClick.Invoke(); yield return null;
            Assert.That(P.Pause.SubPanelOpen); P.Pause.Close(); yield return null;
            FindButton("Continuar").onClick.Invoke(); yield return null;
            Assert.That(GamePause.IsPaused, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1f)); Assert.That(AudioListener.pause, Is.False);
        }

        [UnityTest]
        public IEnumerator RestartFromPauseResetsTheZone()
        {
            yield return Load(GameFlow.ArroyoScene);
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var m = session.Player; m.GetComponent<ThirdPersonInput>().enabled = false;
            m.Teleport(new Vector3(-.6f, 0, 10), Quaternion.identity); yield return null;
            Assert.That(m.GetComponent<PlayerInteractor>().TryInteract(), Is.True);
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(1));
            P.OpenPause(); yield return null;
            FindButton("Reiniciar zona").onClick.Invoke(); yield return null;
            Assert.That(session.Progress.RemovedCount, Is.Zero); Assert.That(GamePause.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator OptionsChangeVolumesAndCameraSensitivity()
        {
            yield return Load(GameFlow.MenuScene);
            BosqueAudio.ResetToDefaults(); CameraSettings.ResetToDefaults();
            FindButton("Opciones").onClick.Invoke(); yield return null;
            var sliders = Object.FindObjectsByType<Slider>(FindObjectsSortMode.None);
            Slider S(string label) => sliders.First(s => s.transform.parent.name == "Fila_" + label);
            S("Música").value = .3f; S("Efectos").value = .5f; S("Sensibilidad del ratón").value = 2f;
            Assert.That(BosqueAudio.GetVolume(AudioBus.Music), Is.EqualTo(.3f).Within(.001f));
            Assert.That(BosqueAudio.GetVolume(AudioBus.Sfx), Is.EqualTo(.5f).Within(.001f));
            Assert.That(CameraSettings.Sensitivity, Is.EqualTo(2f).Within(.001f));
            var cam = new GameObject("CamPrueba").AddComponent<ThirdPersonCamera>(); var target = new GameObject("Objetivo").transform;
            cam.Configure(target); float y0 = cam.Yaw; cam.Step(new Vector2(10, 0), .016f);
            Assert.That(Mathf.DeltaAngle(y0, cam.Yaw), Is.EqualTo(10 * .12f * 2f).Within(.01f), "La sensibilidad escala el giro");
            FindButton("Restablecer valores").onClick.Invoke(); yield return null;
            Assert.That(BosqueAudio.GetVolume(AudioBus.Music), Is.EqualTo(.8f).Within(.001f));
            Assert.That(CameraSettings.Sensitivity, Is.EqualTo(1f).Within(.001f));
            Object.Destroy(cam.gameObject); Object.Destroy(target.gameObject);
        }

        [UnityTest]
        public IEnumerator HudShowsZoneObjectivePromptAndHint()
        {
            GameProgress.CompleteZone(GameProgress.Arroyo);
            yield return Load(GameFlow.ClaroScene);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(P.Hud.Model, Is.InstanceOf<ClaroHud>());
            Assert.That(P.Hud.ObjectiveText, Does.Contain("humedad"));
            var session = Object.FindFirstObjectByType<ClaroSession>();
            var m = session.Player; m.GetComponent<ThirdPersonInput>().enabled = false;
            var dry = session.Points.First(p => !p.Suitable);
            m.Teleport(dry.transform.position + new Vector3(0, 0, -1.2f), Quaternion.identity);
            yield return new WaitForSecondsRealtime(.8f);
            m.GetComponent<Bosque.Abilities.GrowthAbility>().Scan(); yield return null;
            Assert.That(P.Hud.PromptShown, Is.Not.Null, "Aviso [Q] junto al lecho");
            m.GetComponent<Bosque.Abilities.GrowthAbility>().TryGrow(); yield return null;
            Assert.That(P.Hud.HintShown, Is.Not.Null.And.Not.Empty, "Pista tras el intento fallido");
        }

        [UnityTest]
        public IEnumerator EndingOffersCreditsReplayAndMenu()
        {
            GameProgress.CompleteZone(GameProgress.Arroyo);
            yield return Load(GameFlow.ArroyoScene);
            P.Ending.SetExtra("Valle limpio en 9:30 · medalla de oro");
            P.Ending.Show(); yield return null; yield return null;
            Assert.That(P.Ending.Visible, "Pantalla final");
            Assert.That(FindButton("Jugar de nuevo"), Is.Not.Null); Assert.That(FindButton("Menú principal"), Is.Not.Null);
            FindButton("Créditos").onClick.Invoke(); yield return null;
            Assert.That(P.Ending.CreditsOpen);
            P.Ending.CloseCredits(); yield return null;
            FindButton("Menú principal").onClick.Invoke();
            yield return WaitScene(GameFlow.MenuScene);
            Assert.That(P.Ending.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator CaptureThePresentation()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Presentacion/CAPTURAS"));
            Directory.CreateDirectory(dir);
            IEnumerator Shot(string name)
            {
                yield return null; yield return null; // WaitForEndOfFrame no llega en modo batch
                var cam = Camera.main;
                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
                var rt = new RenderTexture(1920, 1080, 24); var prev = cam.targetTexture; cam.targetTexture = rt;
                foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + .05f + (100 - c.sortingOrder) * .0005f; }
                Canvas.ForceUpdateCanvases();
                cam.Render(); RenderTexture.active = rt;
                var img = new Texture2D(1920, 1080, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); img.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), img.EncodeToJPG(88));
                foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
                cam.targetTexture = prev; RenderTexture.active = null; rt.Release(); Object.Destroy(rt); Object.Destroy(img);
            }
            yield return Load(GameFlow.MenuScene);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("01_menu_principal");
            var menu = Object.FindFirstObjectByType<MainMenu>();
            FindButton("Opciones").onClick.Invoke(); yield return Shot("02_opciones");
            menu.Close(); FindButton("Créditos").onClick.Invoke(); yield return Shot("03_creditos");
            menu.Close(); menu.ShowIntro(); yield return Shot("04_introduccion");
            yield return Load(GameFlow.ArroyoScene);
            yield return new WaitForSecondsRealtime(1.3f);
            yield return Shot("05_rotulo_zona");
            yield return new WaitForSecondsRealtime(3.5f);
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var m = session.Player; m.GetComponent<ThirdPersonInput>().enabled = false;
            m.Teleport(new Vector3(-.6f, 0, 8.6f), Quaternion.identity);
            for (int i = 0; i < 20; i++) { m.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null; }
            m.GetComponent<PlayerInteractor>().Scan();
            yield return Shot("06_hud_aviso");
            P.OpenPause(); yield return Shot("07_pausa");
            P.Resume();
            P.ShowNote(GameFlow.NoteFor(GameProgress.Arroyo), null); yield return Shot("08_cuaderno");
            P.HideNote();
            P.Ending.Show(); yield return Shot("09_final");
            P.Ending.Hide();
            Assert.Pass(dir);
        }
    }
}
