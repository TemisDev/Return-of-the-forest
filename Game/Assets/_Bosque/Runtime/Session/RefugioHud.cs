using Bosque.Abilities;
using Bosque.UI;
using Bosque.Interaction;
using Bosque.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Session
{
    /// <summary>Interfaz de "El refugio perdido" y mensaje final del juego (mismo estilo que el arroyo y el claro).</summary>
    public sealed class RefugioHud : MonoBehaviour, IZoneHud
    {
        [SerializeField] private RefugioSession session;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private GrowthAbility ability;
        [SerializeField] private ItemCarrier carrier;
        [SerializeField] private Texture2D interactionIcon, progressIcon, restorationIcon, exitIcon;
        [SerializeField] private float hintSeconds = 6f;
        private GUIStyle title, body, small, interaction, big;
        private bool helpVisible;

        public const string ZoneMessage = "Un hábitat también necesita refugio: troncos, piedras y matorrales donde los animales se resguardan y crían.";
        public const string FinalMessage = "Agua, plantas nativas y refugio están conectados: juntos permiten que la fauna vuelva. " +
            "Aquí la recuperación fue instantánea; en la realidad toma años, seguimiento y el conocimiento de las comunidades locales. " +
            "Cuidar y mantener el bosque importa tanto como repararlo.";
        private static readonly Color Cream = new Color(.95f, .93f, .85f);
        private static readonly Color Accent = new Color(.69f, .82f, .5f);
        private static readonly Color Warning = new Color(.96f, .78f, .45f);

        public void Configure(RefugioSession owner, PlayerInteractor actor, GrowthAbility growth, ItemCarrier carry,
            Texture2D interact, Texture2D progress, Texture2D restore, Texture2D exit)
        {
            session = owner; interactor = actor; ability = growth; carrier = carry;
            interactionIcon = interact; progressIcon = progress; restorationIcon = restore; exitIcon = exit;
        }

        private void Update()
        {
            if (!Application.isFocused || Keyboard.current == null || session == null || GamePause.IsPaused) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) helpVisible = !helpVisible;
            if (Keyboard.current.rKey.wasPressedThisFrame) { session.Restart(); helpVisible = false; }
        }

        /// <summary>Texto del objetivo actual (público para las pruebas).</summary>
        public string Objective
        {
            get
            {
                var p = session.Progress;
                if (p.IsCompleted) return "El zorro de neblina encontró refugio. El corredor está conectado.";
                if (p.IsRestored) return "El refugio está listo. Observa quién regresa…";
                if (carrier != null && carrier.Holding != null) return "Lleva " + carrier.Holding.Label + " a un lugar indicado en el paso del animal.";
                return "Devuelve el refugio: coloca el tronco y las piedras (E) y haz crecer un matorral (Q) donde pasa el animal.";
            }
        }

        // ---- IZoneHud
        public int ZoneNumber => 3;
        public string ZoneTitle => "El refugio perdido";
        public int ProgressDone => session != null && session.Progress != null ? session.Progress.RemovedCount : 0;
        public int ProgressTotal => session != null && session.Progress != null ? session.Progress.TotalCount : 0;
        public string ProgressLabel => "refugios";
        public string PromptKey
        {
            get
            {
                if (session == null || session.Progress == null || session.Progress.IsRestored) return null;
                if (interactor != null && interactor.Target != null) return "E";
                if (ability != null && ability.Target != null) return "Q";
                if (carrier != null && carrier.Holding != null) return "E";
                return null;
            }
        }
        public string PromptText
        {
            get
            {
                if (interactor != null && interactor.Target != null) return interactor.Target.Prompt;
                if (ability != null && ability.Target != null) return ability.Target.Prompt;
                if (carrier != null && carrier.Holding != null) return "Soltar " + carrier.Holding.Label;
                return null;
            }
        }
        public string HintTitle => "Aquí no sirve";
        public string HintText => session != null && session.Progress != null && !session.Progress.IsRestored &&
            !string.IsNullOrEmpty(session.LastHint) && Time.time - session.LastHintTime < hintSeconds ? session.LastHint : null;
        public string Tip => "Sigue las huellas: el animal busca sombra y cobertura.";
        public bool HelpVisible => helpVisible;
        public string HelpTitle => "Devuelve el refugio";
        public string HelpText => "E: recoger un elemento y colocarlo en un lugar indicado\nLos lugares buenos están en el paso del animal: huellas, sombra y rocas\nQ: hacer crecer un matorral en suelo húmedo\nE sin objetivo: soltar lo que llevas\nR: reiniciar · Esc: pausa · F1: cerrar ayuda";
        public bool ShowEnding => session != null && session.GameFinished;
        public void RestartZone() { if (session != null) session.Restart(); helpVisible = false; }

        private void OnGUI()
        {
            if (session == null || session.Progress == null || Presentation.HudActive) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                small = new GUIStyle(body) { fontSize = 14 };
                interaction = new GUIStyle(title) { fontSize = 21 };
                big = new GUIStyle(title) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
                title.normal.textColor = body.normal.textColor = small.normal.textColor = interaction.normal.textColor = big.normal.textColor = Cream;
            }
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            var p = session.Progress;
            if (session.GameFinished) { Final(); GUI.matrix = old; return; }

            Panel(new Rect(24, 24, 460, 216));
            GUI.Label(new Rect(44, 38, 420, 33), "BOSQUE DE REGRESO", title);
            GUI.Label(new Rect(44, 74, 420, 24), "EL REFUGIO PERDIDO  ·  ODS 15", small);
            Icon(new Rect(44, 105, 42, 42), p.IsCompleted ? restorationIcon : p.IsRestored ? exitIcon : progressIcon, Cream);
            GUI.Label(new Rect(98, 101, 355, 60), Objective, body);
            for (int i = 0; i < p.TotalCount; i++)
                Icon(new Rect(44 + i * 29, 165, 23, 23), progressIcon, i < p.RemovedCount ? Accent : new Color(.48f, .51f, .45f));
            GUI.Label(new Rect(150, 162, 280, 30), p.RemovedCount + " / " + p.TotalCount + " refugios", body);
            GUI.Label(new Rect(44, 205, 410, 24), "WASD · Ratón · E: recoger/colocar · Q: crecer · F1", small);

            if (!p.IsRestored)
            {
                string prompt = null, key = "E";
                if (interactor != null && interactor.Target != null) prompt = interactor.Target.Prompt;
                else if (ability != null && ability.Target != null) { prompt = ability.Target.Prompt; key = "Q"; }
                else if (carrier != null && carrier.Holding != null) prompt = "Soltar " + carrier.Holding.Label;
                if (prompt != null)
                {
                    Panel(new Rect(378, 586, 524, 76));
                    Icon(new Rect(397, 601, 44, 44), interactionIcon, Cream);
                    GUI.Label(new Rect(455, 608, 430, 38), "[" + key + "] " + prompt, interaction);
                }
            }
            if (!string.IsNullOrEmpty(session.LastHint) && Time.time - session.LastHintTime < hintSeconds && !p.IsRestored)
            {
                Panel(new Rect(378, 486, 524, 92));
                var prev = GUI.color; GUI.color = Warning;
                GUI.Label(new Rect(398, 496, 490, 24), "Aquí no sirve", small);
                GUI.color = prev;
                GUI.Label(new Rect(398, 518, 490, 56), session.LastHint, body);
            }
            Panel(new Rect(24, 670, 900, 32));
            GUI.Label(new Rect(36, 676, 880, 24), "Sigue las huellas: el animal busca sombra y cobertura.   R: reiniciar · F1: ayuda · Esc: cursor", small);
            if (p.IsCompleted)
            {
                Panel(new Rect(300, 470, 680, 150));
                GUI.Label(new Rect(320, 486, 640, 32), "ODS 15 · Vida de ecosistemas terrestres", interaction);
                GUI.Label(new Rect(320, 524, 636, 65), ZoneMessage, body);
                GUI.Label(new Rect(320, 593, 636, 22), "Completa el arroyo y el claro para ver el final · R: volver a jugar", small);
            }
            if (helpVisible)
            {
                Panel(new Rect(350, 230, 580, 290));
                GUI.Label(new Rect(374, 250, 530, 35), "DEVUELVE EL REFUGIO", title);
                GUI.Label(new Rect(374, 296, 530, 210),
                    "E: recoger un elemento y colocarlo en un lugar indicado\nLos lugares buenos están en el paso del animal: huellas, sombra y rocas\nQ: hacer crecer un matorral en suelo húmedo\nE sin objetivo: soltar lo que llevas\nWASD · Shift: caminar · Espacio: saltar · R: reiniciar\nF1: cerrar ayuda", body);
            }
            GUI.matrix = old;
        }

        private void Final()
        {
            var prev = GUI.color;
            GUI.color = new Color(.04f, .08f, .07f, .72f); GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
            GUI.color = prev;
            Panel(new Rect(190, 120, 900, 480));
            GUI.Label(new Rect(210, 140, 860, 50), "EL BOSQUE REGRESA", big);
            GUI.Label(new Rect(210, 190, 860, 28), "Las tres zonas están recuperadas · ODS 15 · Vida de ecosistemas terrestres", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            string[] rows =
            {
                "El arroyo dormido — el agua vuelve a correr cuando se retiran los obstáculos del cauce.",
                "El claro vacío — las plantas nativas prosperan en el suelo húmedo adecuado.",
                "El refugio perdido — troncos, piedras y matorrales dan cobijo en el paso de la fauna.",
            };
            for (int i = 0; i < rows.Length; i++)
            {
                Icon(new Rect(230, 240 + i * 52, 34, 34), restorationIcon, Accent);
                GUI.Label(new Rect(278, 240 + i * 52, 780, 48), rows[i], body);
            }
            GUI.Label(new Rect(230, 410, 830, 110), FinalMessage, body);
            GUI.Label(new Rect(230, 545, 830, 30), "R: volver a jugar el refugio · Esc: liberar el cursor", small);
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
