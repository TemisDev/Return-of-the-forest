using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bosque.Interaction;
using Bosque.UI;
using Bosque.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Session
{
    /// <summary>Datos de la interfaz de "El valle olvidado" (HUD común, tarjeta lateral, mapa y final).</summary>
    public sealed class ValleHud : MonoBehaviour, IZoneHud, IZoneHudExtra, IZoneMap
    {
        [SerializeField] private ValleSession session;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Texture2D mapTexture;
        [SerializeField] private Rect mapWorld = new Rect(-110, -110, 220, 220);
        [SerializeField] private float hintSeconds = 6f;
        private bool helpVisible;
        private static readonly string[] SectorColors = { "#8FC7E8", "#9CCB7A", "#E3C07A", "#D8A0C8" };

        public void Configure(ValleSession owner, PlayerInteractor actor, Texture2D map, Rect world)
        { session = owner; interactor = actor; mapTexture = map; mapWorld = world; }

        private void Update()
        {
            if (!Application.isFocused || Keyboard.current == null || session == null || GamePause.IsPaused) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) helpVisible = !helpVisible;
            if (Keyboard.current.rKey.wasPressedThisFrame) { session.Restart(); helpVisible = false; }
        }

        // ---- IZoneHud
        public int ZoneNumber => 4;
        public string ZoneTitle => "El valle olvidado";
        public string Objective
        {
            get
            {
                if (session == null) return "";
                if (session.IsCompleted) return "El valle está limpio. ¡La vida regresa!";
                if (session.IsRestored) return "El valle revive. Mira quién vuelve…";
                var bag = session.Bag;
                if (bag != null && bag.Free == 0) return "La bolsa está llena: llévala a un punto de reciclaje (M: mapa).";
                if (session.Cleaned == session.Total) return "Todo recogido. Deposita lo que queda en la bolsa en los contenedores.";
                return "Limpia el valle: recoge los residuos (E), aparta los troncos caídos (mantén E) y separa todo en el reciclaje.";
            }
        }
        public int ProgressDone => session != null ? session.Cleaned : 0;
        public int ProgressTotal => session != null ? session.Total : 0;
        public string ProgressLabel => "limpio";
        public string PromptKey
        {
            get
            {
                if (session == null || session.IsRestored || interactor == null || interactor.Target == null) return null;
                return "E";
            }
        }
        public string PromptText => interactor != null && interactor.Target != null ? interactor.Target.Prompt : null;
        public string HintTitle => "Atención";
        public string HintText => session != null && !session.IsRestored && !string.IsNullOrEmpty(session.LastHint) &&
                                  Time.time - session.LastHintTime < hintSeconds ? session.LastHint : null;
        public string Tip => "T: rastrear residuos cercanos   ·   M: mapa   ·   Una llanta ocupa 3 espacios de la bolsa.";
        public bool HelpVisible => helpVisible;
        public string HelpTitle => "Limpia el valle";
        public string HelpText => "E: recoger un residuo (bolsa de 8 espacios; la llanta ocupa 3)\nMantén E: apartar un tronco o ramas caídas\nE en un contenedor: depositar los residuos de ese tipo\nT: rastreo (marca lo pendiente cercano) · M: mapa\nCada sector limpio reverdece; el valle revive con todo limpio y separado\nR: reiniciar · Esc: pausa · F1: cerrar ayuda";
        public bool ShowEnding => session != null && session.GameFinished;
        public void RestartZone() { if (session != null) session.Restart(); helpVisible = false; }

        // ---- IZoneHudExtra
        public string SideTitle => "VALLE  ·  " + ValleSession.Clock(session != null ? session.ElapsedSeconds : 0);
        public string SideText
        {
            get
            {
                if (session == null) return "";
                var sb = new StringBuilder();
                var bag = session.Bag;
                if (bag != null) sb.Append($"<b>Bolsa {bag.Used}/{bag.Capacity}</b>  <size=80%>{bag.Summary()}</size>\n");
                sb.Append($"Reciclado {session.DepositedCount}/{session.LitterTotal}\n");
                for (int i = 0; i < session.Sectors.Length; i++)
                {
                    var s = session.Sectors[i];
                    string state = s.IsClean ? "  <color=#B0D180><b>limpio</b></color>" : $"  {s.Cleaned}/{s.Total}";
                    sb.Append($"<color={SectorColors[i]}>{s.SectorName}</color>{state}\n");
                }
                var p = session.Pulse;
                if (p != null) sb.Append(p.Ready ? "<color=#B0D180>T: rastreo listo</color>" : $"<color=#999999>Rastreo en {Mathf.CeilToInt(p.CooldownLeft)} s</color>");
                return sb.ToString();
            }
        }
        public float WorkProgress => interactor != null && interactor.Target is FallenWood w && w.Work > 0f ? w.Progress01 : -1f;
        public string EndingExtra => session == null ? "" :
            $"Valle limpio en {ValleSession.Clock(session.ElapsedSeconds)} · medalla de {session.Medal} · {session.Mistakes} intentos fallidos";

        // ---- IZoneMap
        public Texture MapTexture => mapTexture;
        public Rect MapWorldRect => mapWorld;
        public Transform MapPlayer => session != null && session.Player != null ? session.Player.transform : null;
        public IEnumerable<MapMarker> MapMarkers
        {
            get
            {
                if (session == null) yield break;
                foreach (var g in session.Bins.GroupBy(b => b.Station))
                {
                    var c = g.Aggregate(Vector3.zero, (a, b) => a + b.transform.position) / g.Count();
                    yield return new MapMarker { position = c, label = g.Key, color = UiTheme.Moss, big = true };
                }
                string[] names = { "La quebrada", "El bosque", "La pampa", "El mirador" };
                Vector3[] centers = { new Vector3(50, 0, 50), new Vector3(-50, 0, 50), new Vector3(-50, 0, -50), new Vector3(50, 0, -50) };
                for (int i = 0; i < 4 && i < session.Sectors.Length; i++)
                {
                    var s = session.Sectors[i];
                    yield return new MapMarker { position = centers[i], label = $"{names[i]} {s.Cleaned}/{s.Total}", color = s.IsClean ? UiTheme.Moss : new Color(0, 0, 0, 0), big = false };
                }
            }
        }
    }
}
