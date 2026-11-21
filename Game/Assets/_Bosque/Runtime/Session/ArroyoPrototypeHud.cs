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
        [SerializeField] private Texture2D interactionIcon, progressIcon, restorationIcon, exitIcon;
        [SerializeField, Tooltip("Si hay una zona siguiente, al completar se anuncia la habilidad nueva en vez del final.")]
        private string nextZoneTitle;
        public void ConfigureNextZone(string title) => nextZoneTitle = title;
        private GUIStyle title, body, small, interaction;
        private bool helpVisible;
        private int lastShownRemoved = -1, lastShownTotal = -1;
        private string progressText;
        public const string EducationalMessage = "Recuperar un hábitat requiere agua, vegetación y refugio. En la naturaleza, la restauración necesita tiempo, cuidado y conocimiento local.";
        private static readonly Color Cream = new Color(.95f, .93f, .85f);
        private static readonly Color Accent = new Color(.69f, .82f, .5f);
        public bool HasArtIcons => interactionIcon != null && progressIcon != null && restorationIcon != null && exitIcon != null;
        public void Configure(ArroyoSession owner, PlayerInteractor actor) { session = owner; interactor = actor; }
        public void ConfigureArt(Texture2D interact, Texture2D progress, Texture2D restore, Texture2D exit)
        { interactionIcon = interact; progressIcon = progress; restorationIcon = restore; exitIcon = exit; }
        private void Update()
        {
            if (!Application.isFocused || Keyboard.current == null) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) helpVisible = !helpVisible;
            if (Keyboard.current.rKey.wasPressedThisFrame) { session.Restart(); helpVisible = false; }
        }
        private void OnGUI()
        {
            if (session == null || session.Progress == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                small = new GUIStyle(body) { fontSize = 14 };
                interaction = new GUIStyle(title) { fontSize = 21 };
                title.normal.textColor = body.normal.textColor = small.normal.textColor = interaction.normal.textColor = Cream;
            }
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            var p = session.Progress;
            if (p.RemovedCount != lastShownRemoved || p.TotalCount != lastShownTotal)
            {
                lastShownRemoved = p.RemovedCount; lastShownTotal = p.TotalCount;
                progressText = p.RemovedCount + " / " + p.TotalCount + " obstáculos retirados";
            }
            Panel(new Rect(24, 24, 460, 216));
            GUI.Label(new Rect(44, 38, 420, 33), "BOSQUE DE REGRESO", title);
            GUI.Label(new Rect(44, 74, 420, 24), "ARROYO DORMIDO  ·  ODS 15", small);
            var icon = p.IsCompleted ? restorationIcon : p.IsRestored ? exitIcon : progressIcon;
            Icon(new Rect(44, 105, 42, 42), icon, Cream);
            string objective = p.IsCompleted ? "Arroyo recuperado. ¡Gracias por cuidarlo!" :
                p.IsRestored ? "El agua volvió. Sigue el sendero hasta el arco." : "Retira tres bloqueos para devolver el agua.";
            GUI.Label(new Rect(98, 101, 355, 53), objective, body);
            for (int i = 0; i < p.TotalCount; i++)
                Icon(new Rect(44 + i * 29, 165, 23, 23), progressIcon, i < p.RemovedCount ? Accent : new Color(.48f, .51f, .45f));
            GUI.Label(new Rect(150, 162, 280, 30), progressText, body);
            GUI.Label(new Rect(44, 205, 410, 24), "WASD · Ratón · Espacio · E: retirar · F1: ayuda", small);
            if (interactor != null && interactor.Target != null)
            {
                Panel(new Rect(378, 586, 524, 76));
                Icon(new Rect(397, 601, 44, 44), interactionIcon, Cream);
                GUI.Label(new Rect(455, 608, 420, 38), (interactionIcon == null ? "[E] " : "") + interactor.Prompt, interaction);
            }
            Panel(new Rect(24, 670, 900, 32));
            GUI.Label(new Rect(36, 676, 880, 24),
                "Desde la orilla, sin subir al bloqueo.   R: reiniciar · F1: ayuda · Esc: cursor · Clic: volver", small);
            if (p.IsCompleted && !string.IsNullOrEmpty(nextZoneTitle))
            {
                Panel(new Rect(300, 470, 680, 150));
                GUI.Label(new Rect(320, 486, 640, 32), "Nueva habilidad: CRECIMIENTO  [Q]", interaction);
                GUI.Label(new Rect(320, 524, 636, 65),
                    "El agua volvió al arroyo. Ahora puedes hacer brotar plantas nativas donde el suelo tenga humedad.", body);
                GUI.Label(new Rect(320, 593, 636, 22), "Siguiente zona: " + nextZoneTitle + "…", small);
            }
            else if (p.IsCompleted)
            {
                Panel(new Rect(300, 470, 680, 150));
                GUI.Label(new Rect(320, 486, 640, 32), "ODS 15 · Vida de ecosistemas terrestres", interaction);
                GUI.Label(new Rect(320, 524, 636, 65), EducationalMessage, body);
                GUI.Label(new Rect(320, 593, 636, 22), "R: volver a jugar · El bosque necesita cuidado continuo.", small);
            }
            if (helpVisible)
            {
                Panel(new Rect(350, 250, 580, 260));
                GUI.Label(new Rect(374, 270, 530, 35), "RECUPERA EL ARROYO", title);
                GUI.Label(new Rect(374, 316, 530, 170),
                    "WASD: moverte · Shift: caminar · Ratón: mirar\nEspacio: saltar · E: retirar un bloqueo\nAcércate desde la orilla, sin subir al obstáculo.\nR: reiniciar · Esc: liberar el cursor\nF1: cerrar ayuda\nEl progreso no se guarda al cerrar.", body);
            }
            if (p.IsCompleted && Cursor.lockState != CursorLockMode.Locked)
            {
                if (GUI.Button(new Rect(1040, 28, 210, 44), "Reiniciar arroyo [R]")) { session.Restart(); helpVisible = false; }
                if (GUI.Button(new Rect(1040, 82, 210, 44), "Salir del juego")) Application.Quit();
            }
            GUI.matrix = oldMatrix;
        }
        private static void Panel(Rect rect)
        {
            var previous = GUI.color;
            GUI.color = new Color(.08f, .14f, .12f, .96f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Accent;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
        private static void Icon(Rect rect, Texture2D texture, Color tint)
        {
            if (texture == null) return;
            var previous = GUI.color; GUI.color = tint;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }
    }
}
