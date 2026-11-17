using Bosque.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Session
{
    // Replaceable prototype view. All state and commands belong to ArroyoSession.
    public sealed class ArroyoPrototypeHud : MonoBehaviour
    {
        [SerializeField] private ArroyoSession session;
        [SerializeField] private PlayerInteractor interactor;
        private GUIStyle title, body;
        public void Configure(ArroyoSession owner, PlayerInteractor actor) { session = owner; interactor = actor; }
        private void Update()
        {
            if (Application.isFocused && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                session.Restart();
        }
        private void OnGUI()
        {
            if (session == null || session.Progress == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                title.normal.textColor = body.normal.textColor = new Color(.95f, .94f, .85f);
            }
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            var p = session.Progress;
            GUI.Box(new Rect(20, 20, 510, 170), GUIContent.none);
            GUI.Label(new Rect(38, 32, 480, 38), "BOSQUE DE REGRESO", title);
            string objective = p.IsCompleted ? "Arroyo recuperado · recorrido completado" :
                p.IsRestored ? "El agua ha vuelto. Cruza la salida." : "Devuelve el agua: retira los tres obstáculos.";
            GUI.Label(new Rect(38, 76, 470, 60), objective + "\nObstáculos: " + p.RemovedCount + " / " + p.TotalCount, body);
            GUI.Label(new Rect(38, 140, 470, 35), "WASD · Ratón · Espacio     R: reiniciar", body);
            if (interactor != null && interactor.Target != null)
            {
                GUI.Box(new Rect(360, 590, 560, 74), GUIContent.none);
                GUI.Label(new Rect(380, 607, 530, 45), "[E] " + interactor.Prompt, title);
            }
            GUI.Label(new Rect(24, 676, 850, 36),
                "Interactúa desde la orilla, sin subir al obstáculo. Esc: liberar cursor · Clic: volver", body);
            if (Cursor.lockState != CursorLockMode.Locked &&
                GUI.Button(new Rect(1040, 28, 210, 44), "Reiniciar arroyo [R]")) session.Restart();
            GUI.matrix = oldMatrix;
        }
    }
}
