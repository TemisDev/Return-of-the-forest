using System;
using System.Collections;
using Bosque.UI;
using Bosque.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.Session
{
    /// <summary>
    /// Al completar el refugio (el zorro llegó a la madriguera): nota del cuaderno y paso a "El valle olvidado".
    /// Reiniciar con R antes de que termine la pausa la cancela.
    /// </summary>
    public sealed class RefugioZoneTransition : MonoBehaviour
    {
        [SerializeField] private RefugioSession session;
        [SerializeField] private string nextScene = "ValleOlvidado";
        [SerializeField] private float delaySeconds = 4.5f;
        private Coroutine pending;
        private bool bound, wasCompleted;

        public static bool SuppressSceneLoad { get => ArroyoZoneTransition.SuppressSceneLoad; set => ArroyoZoneTransition.SuppressSceneLoad = value; }
        public bool Pending => pending != null;
        public string NextScene => nextScene;

        public void Configure(RefugioSession owner, string scene, float delay)
        {
            session = owner; nextScene = scene; delaySeconds = delay;
        }

        private void Start() => Bind();
        private void OnEnable() { if (session != null && session.Progress != null) Bind(); }
        private void OnDisable()
        {
            if (bound && session != null) session.StateChanged -= OnStateChanged;
            bound = false;
        }

        private void Bind()
        {
            if (bound || session == null) return;
            session.InitializeFromScene();
            if (session.Progress == null) return;
            session.StateChanged += OnStateChanged; bound = true;
            wasCompleted = session.Progress.IsCompleted;
        }

        private void OnStateChanged()
        {
            bool done = session.Progress.IsCompleted;
            if (done && !wasCompleted)
            {
                GameProgress.CompleteZone(GameProgress.Refugio);
                if (!SuppressSceneLoad && !string.IsNullOrEmpty(nextScene) && isActiveAndEnabled)
                    pending = StartCoroutine(LoadNext());
            }
            else if (!done)
            {
                if (pending != null) { StopCoroutine(pending); pending = null; }
                if (Presentation.Instance != null && Presentation.Instance.Notebook.Visible) Presentation.Instance.HideNote();
            }
            wasCompleted = done;
        }

        /// <summary>
        /// Tras una pausa breve aparece la nota del cuaderno de la zona; "Continuar" carga la siguiente con fundido.
        /// Sin la capa de presentación (escenas de prueba antiguas) carga sola tras <c>delaySeconds</c>.
        /// </summary>
        private IEnumerator LoadNext()
        {
            yield return new WaitForSecondsRealtime(Presentation.Instance != null ? 1.2f : delaySeconds);
            pending = null;
            if (session == null || session.Progress == null || !session.Progress.IsCompleted) yield break;
            if (Presentation.Instance != null)
                Presentation.Instance.ShowNote(GameFlow.NoteFor(GameProgress.Refugio), () =>
                {
                    if (session != null && session.Progress != null && session.Progress.IsCompleted) GameFlow.LoadScene(nextScene);
                });
            else SceneManager.LoadScene(nextScene);
        }
    }
}
