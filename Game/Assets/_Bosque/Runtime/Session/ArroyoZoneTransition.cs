using System;
using System.Collections;
using Bosque.UI;
using Bosque.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.Session
{
    /// <summary>
    /// Al completar el arroyo: registra la zona (concede el crecimiento) y, tras una pausa para leer el mensaje,
    /// carga la siguiente zona. Reiniciar con R antes de que termine la pausa la cancela.
    /// Con -arroyo-verify (verificador de builds) no cambia de escena, para no alterar sus ciclos.
    /// </summary>
    public sealed class ArroyoZoneTransition : MonoBehaviour
    {
        [SerializeField] private ArroyoSession session;
        [SerializeField] private string nextScene = "ClaroVacio";
        [SerializeField] private float delaySeconds = 4.5f;
        private Coroutine pending;
        private bool bound, wasCompleted;

        public static bool SuppressSceneLoad { get; set; }
        public bool Pending => pending != null;
        public string NextScene => nextScene;

        public void Configure(ArroyoSession owner, string scene, float delay)
        {
            session = owner; nextScene = scene; delaySeconds = delay;
        }

        private static bool VerifierRunning => Array.IndexOf(Environment.GetCommandLineArgs(), "-arroyo-verify") >= 0;

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
                GameProgress.CompleteZone(GameProgress.Arroyo);
                if (!SuppressSceneLoad && !VerifierRunning && !string.IsNullOrEmpty(nextScene) && isActiveAndEnabled)
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
                Presentation.Instance.ShowNote(GameFlow.NoteFor(GameProgress.Arroyo), () =>
                {
                    if (session != null && session.Progress != null && session.Progress.IsCompleted) GameFlow.LoadScene(nextScene);
                });
            else SceneManager.LoadScene(nextScene);
        }
    }
}
