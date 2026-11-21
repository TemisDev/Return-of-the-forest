using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>
    /// Capa de presentación persistente (sobrevive a los cambios de escena): fundidos, rótulo de zona, HUD,
    /// cuaderno de la cuidadora, pausa y final. Se crea sola al arrancar si existe el tema de interfaz.
    /// Con el verificador de builds (-arroyo-verify) no muestra rótulos ni notas para no alterar sus medidas.
    /// </summary>
    public sealed class Presentation : MonoBehaviour
    {
        public static Presentation Instance { get; private set; }
        /// <summary>Con la capa activa, los HUD antiguos (IMGUI) no se dibujan.</summary>
        public static bool HudActive => Instance != null;

        private Canvas hudCanvas, overlayCanvas, fadeCanvas;
        private CanvasGroup fade;
        private Coroutine fading;
        private bool endingNoteShown;
        public ZoneHudView Hud { get; private set; }
        public NotebookView Notebook { get; private set; }
        public EndingView Ending { get; private set; }
        public PauseView Pause { get; private set; }
        public TitleCardView TitleCard { get; private set; }
        public MapView Map { get; private set; }
        public bool Fading => fading != null;
        public bool InZone => Hud != null && Hud.Model != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Instance != null || Ui.Theme == null) return;
            var go = new GameObject("Presentacion");
            DontDestroyOnLoad(go);
            go.AddComponent<Presentation>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Ui.EnsureEventSystem(transform);
            hudCanvas = Ui.Canvas("LienzoHUD", 10, transform);
            overlayCanvas = Ui.Canvas("LienzoMenus", 50, transform);
            var titleCanvas = Ui.Canvas("LienzoRotulo", 40, transform);
            fadeCanvas = Ui.Canvas("LienzoFundido", 200, transform);
            Hud = new ZoneHudView(hudCanvas.transform);
            TitleCard = new TitleCardView(titleCanvas.transform);
            Notebook = new NotebookView(overlayCanvas.transform);
            Ending = new EndingView(overlayCanvas.transform);
            Pause = new PauseView(overlayCanvas.transform, Resume, RestartFromPause);
            Map = new MapView(overlayCanvas.transform);
            var black = Ui.Image(fadeCanvas.transform, "Negro", Color.black, null, true); black.rectTransform.Stretch();
            fade = black.gameObject.AddComponent<CanvasGroup>(); fade.alpha = 0; fade.blocksRaycasts = false;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GamePause.Set(false);
            Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            GamePause.Set(false);
            Pause.Hide(); Notebook.Hide(); Ending.Hide(); TitleCard.Hide(); Map.Hide(); endingNoteShown = false;
            Hud.Bind(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IZoneHud>().FirstOrDefault());
            var zone = GameFlow.Zone(scene.name);
            if (zone != null && !GameFlow.VerifierRunning) TitleCard.Show(zone);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            bool overlay = Notebook.Visible || Ending.Visible || Pause.Visible || Map.Visible;
            Map.Tick(Hud.Model as IZoneMap);
            Hud.Tick(InZone && !overlay);
            TitleCard.Tick();

            var model = Hud.Model;
            if (model != null && (model as UnityEngine.Object) != null)
            {
                // Final: primero la última nota del cuaderno y después la pantalla de cierre.
                if (model.ShowEnding && !Ending.Visible && !Notebook.Visible)
                {
                    if (!endingNoteShown) { endingNoteShown = true; Ending.SetExtra((model as IZoneHudExtra)?.EndingExtra); ShowNote(GameFlow.NoteFor(World.GameProgress.Valle), Ending.Show, "Ver el final  [E]"); }
                    else { Pause.Hide(); GamePause.Set(false); Ending.Show(); }
                }
                else if (!model.ShowEnding && endingNoteShown)
                {
                    endingNoteShown = false; Ending.Hide();
                    if (Notebook.Visible) Notebook.Hide();
                }
            }
            if (kb == null || Fading) return;
            if (Notebook.Visible && Time.unscaledTime - Notebook.ShownAt > .8f &&
                (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                Notebook.Continue();
            if (Map.Visible && (kb.mKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) { CloseMap(); return; }
            if (kb.mKey.wasPressedThisFrame && Hud.Model is IZoneMap && !Notebook.Visible && !Ending.Visible && !Pause.Visible) { OpenMap(); return; }
            if (kb.escapeKey.wasPressedThisFrame && InZone && !Notebook.Visible && !Ending.Visible)
            {
                if (!Pause.Visible) OpenPause();
                else if (Pause.SubPanelOpen) Pause.Close();
                else Resume();
            }
            else if (kb.escapeKey.wasPressedThisFrame && Ending.CreditsOpen) Ending.CloseCredits();
        }

        public void OpenPause()
        {
            if (!InZone || Pause.Visible) return;
            GamePause.Set(true);
            Pause.Show();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        public void OpenMap()
        {
            if (!(Hud.Model is IZoneMap) || Map.Visible) return;
            GamePause.Set(true); Map.Show();
        }

        public void CloseMap()
        {
            if (!Map.Visible) return;
            Map.Hide(); GamePause.Set(false);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        public void Resume()
        {
            Pause.Hide();
            GamePause.Set(false);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        private void RestartFromPause()
        {
            var model = Hud.Model;
            Resume();
            model?.RestartZone();
        }

        /// <summary>Muestra una nota del cuaderno; <paramref name="next"/> se ejecuta al pulsar Continuar.</summary>
        public void ShowNote(GameFlow.Note note, Action next, string buttonLabel = "Continuar  [E]")
        {
            Pause.Hide(); GamePause.Set(false);
            Notebook.Show(note, next, buttonLabel);
        }

        public void HideNote() => Notebook.Hide();

        public void LoadWithFade(string scene)
        {
            if (fading != null) return;
            fading = StartCoroutine(FadeLoad(scene));
        }

        private IEnumerator FadeLoad(string scene)
        {
            fade.blocksRaycasts = true;
            for (float t = 0; t < .55f; t += Time.unscaledDeltaTime) { fade.alpha = t / .55f; yield return null; }
            fade.alpha = 1;
            GamePause.Set(false);
            Pause.Hide(); Notebook.Hide(); Ending.Hide();
            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime) { fade.alpha = 1 - t / .8f; yield return null; }
            fade.alpha = 0; fade.blocksRaycasts = false;
            fading = null;
        }
    }
}
