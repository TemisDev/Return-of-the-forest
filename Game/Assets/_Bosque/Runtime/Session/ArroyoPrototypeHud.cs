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
        private GUIStyle title, body, small, interaction;
        private static readonly Color Cream = new Color(.95f, .93f, .85f);
        private static readonly Color Accent = new Color(.69f, .82f, .5f);
        public bool HasArtIcons => interactionIcon != null && progressIcon != null && restorationIcon != null && exitIcon != null;
        public void Configure(ArroyoSession owner, PlayerInteractor actor) { session = owner; interactor = actor; }
        public void ConfigureArt(Texture2D interact, Texture2D progress, Texture2D restore, Texture2D exit)
        { interactionIcon = interact; progressIcon = progress; restorationIcon = restore; exitIcon = exit; }
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
            Panel(new Rect(24, 24, 460, 190));
            GUI.Label(new Rect(44, 38, 420, 33), "BOSQUE DE REGRESO", title);
            var icon = p.IsCompleted ? restorationIcon : p.IsRestored ? exitIcon : progressIcon;
            Icon(new Rect(44, 87, 42, 42), icon, Cream);
            string objective = p.IsCompleted ? "Arroyo recuperado. ¡Gracias por cuidarlo!" :
                p.IsRestored ? "El agua ha vuelto. La salida está abierta." : "Libera el cauce desde la orilla.";
            GUI.Label(new Rect(98, 83, 355, 53), objective, body);
            for (int i = 0; i < p.TotalCount; i++)
                Icon(new Rect(44 + i * 29, 146, 23, 23), progressIcon, i < p.RemovedCount ? Accent : new Color(.48f, .51f, .45f));
            GUI.Label(new Rect(150, 143, 280, 30), p.RemovedCount + " / " + p.TotalCount + " obstáculos retirados", body);
            GUI.Label(new Rect(44, 180, 410, 24), "WASD · Ratón · Espacio      R: reiniciar", small);
            if (interactor != null && interactor.Target != null)
            {
                Panel(new Rect(378, 586, 524, 76));
                Icon(new Rect(397, 601, 44, 44), interactionIcon, Cream);
                GUI.Label(new Rect(455, 608, 420, 38), "[E] " + interactor.Prompt, interaction);
            }
            Panel(new Rect(24, 670, 900, 32));
            GUI.Label(new Rect(36, 676, 880, 24),
                "Acércate por la orilla. No te subas al obstáculo.   Esc: liberar cursor · Clic: volver", small);
            if (Cursor.lockState != CursorLockMode.Locked &&
                GUI.Button(new Rect(1040, 28, 210, 44), "Reiniciar arroyo [R]")) session.Restart();
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
