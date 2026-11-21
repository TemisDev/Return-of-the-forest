using Bosque.Abilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Session
{
    /// <summary>Interfaz de la zona "El claro vacío" (mismo estilo que la del arroyo). Solo muestra estado.</summary>
    public sealed class ClaroHud : MonoBehaviour
    {
        [SerializeField] private ClaroSession session;
        [SerializeField] private GrowthAbility ability;
        [SerializeField] private Texture2D interactionIcon, progressIcon, restorationIcon, exitIcon;
        [SerializeField] private float hintSeconds = 6f;
        [SerializeField] private string nextZoneTitle;
        private GUIStyle title, body, small, interaction;
        private bool helpVisible;
        public const string EducationalMessage = "Las plantas nativas necesitan el suelo y la humedad adecuados. Recuperar un hábitat es devolverle sus condiciones, no solo plantar.";
        private static readonly Color Cream = new Color(.95f, .93f, .85f);
        private static readonly Color Accent = new Color(.69f, .82f, .5f);
        private static readonly Color Warning = new Color(.96f, .78f, .45f);

        public void Configure(ClaroSession owner, GrowthAbility growth, Texture2D interact, Texture2D progress, Texture2D restore, Texture2D exit)
        {
            session = owner; ability = growth;
            interactionIcon = interact; progressIcon = progress; restorationIcon = restore; exitIcon = exit;
        }

        /// <summary>Con una siguiente zona, el mensaje final anuncia el paso a ella en lugar de "próximamente".</summary>
        public void ConfigureNextZone(string title) => nextZoneTitle = title;

        private void Update()
        {
            if (!Application.isFocused || Keyboard.current == null || session == null) return;
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
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            var p = session.Progress;
            Panel(new Rect(24, 24, 460, 216));
            GUI.Label(new Rect(44, 38, 420, 33), "BOSQUE DE REGRESO", title);
            GUI.Label(new Rect(44, 74, 420, 24), "EL CLARO VACÍO  ·  ODS 15", small);
            Icon(new Rect(44, 105, 42, 42), p.IsCompleted ? restorationIcon : p.IsRestored ? exitIcon : progressIcon, Cream);
            string objective = p.IsCompleted ? "Claro recuperado. ¡Las semillas encontraron su lugar!" :
                p.IsRestored ? "El claro revive. Cruza el puente de raíces hasta el arco." :
                "Haz brotar plantas nativas donde el suelo tenga humedad (Q).";
            GUI.Label(new Rect(98, 101, 355, 53), objective, body);
            for (int i = 0; i < p.TotalCount; i++)
                Icon(new Rect(44 + i * 29, 165, 23, 23), progressIcon, i < p.RemovedCount ? Accent : new Color(.48f, .51f, .45f));
            GUI.Label(new Rect(150, 162, 280, 30), p.RemovedCount + " / " + p.TotalCount + " brotes nativos", body);
            GUI.Label(new Rect(44, 205, 410, 24), "WASD · Ratón · Espacio · Q: crecer · F1: ayuda", small);

            if (ability != null && ability.Target != null && !p.IsCompleted)
            {
                Panel(new Rect(378, 586, 524, 76));
                Icon(new Rect(397, 601, 44, 44), interactionIcon, Cream);
                GUI.Label(new Rect(455, 608, 420, 38), "[Q] " + ability.Target.Prompt, interaction);
            }
            if (!string.IsNullOrEmpty(session.LastHint) && Time.time - session.LastHintTime < hintSeconds && !p.IsRestored)
            {
                Panel(new Rect(378, 486, 524, 86));
                var prev = GUI.color; GUI.color = Warning;
                GUI.Label(new Rect(398, 496, 490, 24), "Aquí no prospera", small);
                GUI.color = prev;
                GUI.Label(new Rect(398, 518, 490, 50), session.LastHint, body);
            }
            Panel(new Rect(24, 670, 900, 32));
            GUI.Label(new Rect(36, 676, 880, 24), "Observa el suelo: oscuro y con musgo es húmedo.   R: reiniciar · F1: ayuda · Esc: cursor", small);
            if (p.IsCompleted)
            {
                Panel(new Rect(300, 470, 680, 150));
                GUI.Label(new Rect(320, 486, 640, 32), "ODS 15 · Vida de ecosistemas terrestres", interaction);
                GUI.Label(new Rect(320, 524, 636, 65), EducationalMessage, body);
                GUI.Label(new Rect(320, 593, 636, 22), string.IsNullOrEmpty(nextZoneTitle) ? "Siguiente zona: El refugio perdido (próximamente) · R: volver a jugar el claro"
                    : "Siguiente zona: " + nextZoneTitle + "… (R antes cancela y reinicia el claro)", small);
            }
            if (helpVisible)
            {
                Panel(new Rect(350, 250, 580, 260));
                GUI.Label(new Rect(374, 270, 530, 35), "DEVUELVE LA VIDA AL CLARO", title);
                GUI.Label(new Rect(374, 316, 530, 170),
                    "Q: hacer crecer una planta en el lecho cercano\nSolo prospera en suelo húmedo (oscuro, con musgo)\nTres brotes nativos devuelven el claro y crean un paso\nWASD · Shift: caminar · Espacio: saltar · R: reiniciar\nF1: cerrar ayuda", body);
            }
            GUI.matrix = old;
        }

        private static void Panel(Rect rect)
        {
            var previous = GUI.color;
            GUI.color = new Color(.08f, .14f, .12f, .96f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Accent; GUI.DrawTexture(new Rect(rect.x, rect.y, 3, rect.height), Texture2D.whiteTexture);
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
