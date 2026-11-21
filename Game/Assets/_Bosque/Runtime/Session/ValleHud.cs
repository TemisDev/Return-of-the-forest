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
    public sealed class ValleHud : MonoBehaviour, IZoneHud, IZoneHudExtra, IZoneMap, IZoneMapInfo
    {
        [SerializeField] private ValleSession session;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Texture2D mapTexture;
        [SerializeField] private Rect mapWorld = new Rect(-110, -110, 220, 220);
        [SerializeField] private float hintSeconds = 6f;
        [SerializeField] private ValleWater water;
        [SerializeField] private float discoverRadius = 24f;
        private bool helpVisible, hooked;
        private float nextScan;
        private readonly HashSet<Component> discovered = new HashSet<Component>();
        private static readonly string[] SectorColors = { "#8FC7E8", "#9CCB7A", "#E3C07A", "#D8A0C8" };
        /// <summary>Dónde va el nombre de cada región en el mapa (zonas despejadas de ríos y puestos).</summary>
        private static readonly Vector3[] RegionLabels = { new Vector3(84, 0, 103), new Vector3(-62, 0, 58), new Vector3(-92, 0, -50), new Vector3(72, 0, -20) };

        /// <summary>Nombre de cada laguna junto a ella, donde no pisa ríos ni puestos.</summary>
        private static Vector3 LakeLabel(WaterLake l) => l.name switch
        {
            // Las lagunas grandes llevan el nombre sobre el agua, como en los mapas impresos.
            "Laguna Alta" => l.center,
            "Laguna del Medio" => l.center,
            "Laguna Baja" => l.center + new Vector3(0, 0, 1f),
            "Bofedal" => new Vector3(-17, 0, -44),
            _ => l.center + new Vector3(0, 0, -(l.radius + 4.5f)),
        };

        private static float Upright(float a) { a = Mathf.Repeat(a + 180f, 360f) - 180f; return a > 90f ? a - 180f : a < -90f ? a + 180f : a; }

        public string MapTitle => "El valle olvidado";
        public string MapSubtitle => "Cuenca del río Mayu  ·  levantamiento de la Guardiana";
        public IEnumerable<MapSector> MapSectors
        {
            get
            {
                if (session == null) yield break;
                for (int i = 0; i < session.Sectors.Length; i++)
                {
                    var s = session.Sectors[i];
                    ColorUtility.TryParseHtmlString(SectorColors[i % SectorColors.Length], out var c);
                    yield return new MapSector { name = s.SectorName, cleaned = s.Cleaned, total = s.Total, hidden = s.IsClean ? 0 : UndiscoveredIn(s), color = c };
                }
            }
        }

        public void Configure(ValleSession owner, PlayerInteractor actor, Texture2D map, Rect world, ValleWater riverSystem = null)
        { session = owner; interactor = actor; mapTexture = map; mapWorld = world; water = riverSystem; }

        public float DiscoverRadius => discoverRadius;
        /// <summary>Residuo o tronco ya ubicado (visto de cerca o marcado con el rastreo): sale en la brújula y los mapas.</summary>
        public bool IsDiscovered(Component c) => discovered.Contains(c);
        public int DiscoveredPending => PendingThings().Count(c => discovered.Contains(c));
        public int UndiscoveredIn(ValleSector s) =>
            s.Litter.Count(l => !l.Collected && !discovered.Contains(l)) + s.Wood.Count(w => !w.Cleared && !discovered.Contains(w));

        private IEnumerable<Component> PendingThings() =>
            session.AllLitter.Where(l => !l.Collected).Cast<Component>().Concat(session.AllWood.Where(w => !w.Cleared));

        /// <summary>Marca como ubicado todo lo pendiente a menos de <paramref name="radius"/> metros.</summary>
        public void Discover(Vector3 from, float radius)
        {
            if (session == null) return;
            float r2 = radius * radius;
            foreach (var c in PendingThings())
            {
                var d = c.transform.position - from; d.y = 0;
                if (d.sqrMagnitude <= r2) discovered.Add(c);
            }
        }

        private void Update()
        {
            if (session != null && !hooked && session.Pulse != null)
            {
                hooked = true;
                session.Pulse.Pulsed += n => Discover(session.Pulse.transform.position, session.Pulse.Radius);
                session.Restarted += () => discovered.Clear();
            }
            if (session != null && session.Player != null && Time.time >= nextScan)
            {
                nextScan = Time.time + .25f;
                Discover(session.Player.transform.position, discoverRadius);
            }
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
        public string Tip => "T: rastrear   ·   M: mapa   ·   La brújula marca lo que ya ubicaste   ·   El agua honda se cruza por puentes y vados.";
        public bool HelpVisible => helpVisible;
        public string HelpTitle => "Limpia el valle";
        public string HelpText => "E: recoger un residuo (bolsa de 8 espacios; la llanta ocupa 3)\nMantén E: apartar un tronco o ramas caídas\nE en un contenedor: depositar los residuos de ese tipo\nT: rastreo (marca lo pendiente cercano) · M: mapa\nLo que ves de cerca o rastreas queda en la brújula y el mapa\nEl agua honda no se cruza: busca puentes y vados\nCada sector limpio reverdece; el valle revive con todo limpio y separado\nR: reiniciar · Esc: pausa · F1: cerrar ayuda";
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
                    yield return new MapMarker { position = c, label = g.Key, color = UiTheme.Moss, big = true, kind = MarkerKind.Station };
                }
                // Regiones: solo el nombre, como en un mapa impreso (el avance va en el panel lateral).
                for (int i = 0; i < 4 && i < session.Sectors.Length; i++)
                    yield return new MapMarker { position = RegionLabels[i], label = session.Sectors[i].SectorName, kind = MarkerKind.Region,
                        color = session.Sectors[i].IsClean ? UiTheme.MossDark : UiTheme.Ink };
                foreach (var c in PendingThings())
                    if (discovered.Contains(c))
                        yield return new MapMarker { position = c.transform.position, label = "", color = UiTheme.Amber, kind = c is FallenWood ? MarkerKind.Wood : MarkerKind.Litter };
                if (water == null) yield break;
                foreach (var st in water.Streams)
                    if (!string.IsNullOrEmpty(st.name))
                    {
                        // El nombre sigue la dirección del río (sin quedar cabeza abajo).
                        ValleWater.Nearest(st, new Vector2(st.labelPosition.x, st.labelPosition.z), out _, out _, out _, out var f, out _);
                        yield return new MapMarker { position = st.labelPosition, label = st.name, kind = MarkerKind.Label, angle = Upright(Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg) };
                    }
                foreach (var l in water.Lakes)
                    if (!string.IsNullOrEmpty(l.name))
                        yield return new MapMarker { position = LakeLabel(l), label = l.name, kind = MarkerKind.Label, big = true };
                foreach (var x in water.Crossings)
                {
                    float across = 0;
                    foreach (var st in water.Streams)
                    {
                        ValleWater.Nearest(st, new Vector2(x.position.x, x.position.z), out float d, out _, out _, out var f, out _);
                        if (d < 2f) { across = Mathf.Atan2(f.x, -f.y) * Mathf.Rad2Deg; break; }
                    }
                    yield return new MapMarker { position = x.position, label = x.bridge ? "Puente" : "Vado", kind = x.bridge ? MarkerKind.Bridge : MarkerKind.Ford, angle = across };
                }
            }
        }
    }
}
