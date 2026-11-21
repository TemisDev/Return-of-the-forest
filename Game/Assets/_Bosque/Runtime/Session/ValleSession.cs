using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>
    /// Zona 4, "El valle olvidado": un valle grande dividido en cuatro sectores con residuos y troncos caídos.
    /// - Recoger un residuo (bolsa de 8 espacios) o apartar un tronco (mantener E) limpia esa parte del sector;
    ///   un sector limpio reverdece (suelo, pasto y árboles).
    /// - Los residuos deben llevarse a un punto de reciclaje y depositarse en el contenedor de su tipo.
    /// - El valle entero revive solo cuando todo está limpio y depositado: vuelve el agua y regresa la fauna,
    ///   cuya llegada completa la zona. Cronómetro con medallas para la rejugabilidad.
    /// </summary>
    public sealed class ValleSession : MonoBehaviour
    {
        [SerializeField] private ValleSector[] sectors;
        [SerializeField] private RecycleBin[] bins;
        [SerializeField] private TerrainRestorer terrain;
        [SerializeField] private GameObject degradedVisuals, restoredVisuals;
        [SerializeField] private FaunaRoute fauna;
        [SerializeField] private FaunaCinematic cinematic;
        [SerializeField] private ThirdPersonMotor player;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private float goldSeconds = 720f, silverSeconds = 1080f;

        private readonly HashSet<string> deposited = new HashSet<string>(StringComparer.Ordinal);
        private LitterBag bag;
        private ThirdPersonInput input;
        private TrackingPulse pulse;
        private Coroutine faunaStart;
        private bool initialized;

        public ValleSector[] Sectors => sectors;
        public RecycleBin[] Bins => bins;
        public TerrainRestorer Terrain => terrain;
        public FaunaRoute Fauna => fauna;
        public FaunaCinematic Cinematic => cinematic;
        public ThirdPersonMotor Player => player;
        public LitterBag Bag => bag;
        public TrackingPulse Pulse => pulse;
        public IEnumerable<LitterItem> AllLitter => sectors.SelectMany(s => s.Litter);
        public IEnumerable<FallenWood> AllWood => sectors.SelectMany(s => s.Wood);
        public int Total => sectors.Sum(s => s.Total);
        public int Cleaned => sectors.Sum(s => s.Cleaned);
        public int LitterTotal => AllLitter.Count();
        public int DepositedCount => deposited.Count;
        public int SectorsClean => sectors.Count(s => s.IsClean);
        public bool IsRestored { get; private set; }
        public bool IsCompleted { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int Mistakes { get; private set; }
        public string LastHint { get; private set; }
        public float LastHintTime { get; private set; } = -100f;
        public bool GameFinished => IsCompleted && GameProgress.IsZoneComplete(GameProgress.Arroyo) && GameProgress.IsZoneComplete(GameProgress.Claro) &&
                                    GameProgress.IsZoneComplete(GameProgress.Refugio) && GameProgress.IsZoneComplete(GameProgress.Valle);
        public string Medal => ElapsedSeconds <= goldSeconds ? "oro" : ElapsedSeconds <= silverSeconds ? "plata" : "bronce";
        public float GoldSeconds => goldSeconds;
        public event Action StateChanged;
        public event Action<LitterItem> Collected;
        public event Action<FallenWood> WoodCleared;
        public event Action<RecycleBin, int> DepositedItems;
        public event Action<string> AttemptRejected;
        public event Action<ValleSector> SectorRestored;
        public event Action ValleRestored;

        public void Configure(ValleSector[] valleSectors, RecycleBin[] recycleBins, TerrainRestorer restorer, GameObject degraded, GameObject restored,
            FaunaRoute route, FaunaCinematic shot, ThirdPersonMotor motor, Transform spawn)
        {
            sectors = valleSectors; bins = recycleBins; terrain = restorer; degradedVisuals = degraded; restoredVisuals = restored;
            fauna = route; cinematic = shot; player = motor; playerSpawn = spawn; initialized = false;
        }

        private void Start() => InitializeFromScene();

        public void InitializeFromScene()
        {
            if (initialized) return;
            if (sectors == null || sectors.Length != 4 || bins == null || bins.Length == 0 || terrain == null || player == null || playerSpawn == null ||
                degradedVisuals == null || restoredVisuals == null || fauna == null)
                throw new ArgumentException("Faltan referencias del valle.");
            initialized = true;
            terrain.Initialize();
            bag = player.GetComponent<LitterBag>();
            input = player.GetComponent<ThirdPersonInput>();
            pulse = player.GetComponent<TrackingPulse>();
            if (pulse != null) pulse.SetTargets(PendingPositions);
            var actor = player.GetComponent<PlayerInteractor>();
            foreach (var l in AllLitter) l.Rejected += (item, hint) => Reject(hint);
            foreach (var w in AllWood) w.ClearedEvent += OnWoodCleared;
            foreach (var b in bins) { b.Deposited += OnDeposited; b.Rejected += (bin, hint) => Reject(hint); }
            if (actor != null) actor.Interacted += OnInteracted;
            fauna.ArrivedAtRefuge += OnFaunaArrived;
            ApplyVisuals();
        }

        public IEnumerable<Vector3> PendingPositions() =>
            AllLitter.Where(l => !l.Collected).Select(l => l.transform.position)
                .Concat(AllWood.Where(w => !w.Cleared).Select(w => w.transform.position));

        private void Update()
        {
            if (initialized && !IsRestored) ElapsedSeconds += Time.deltaTime;
        }

        private void OnInteracted(InteractionTarget target)
        {
            if (target is LitterItem item && item.Collected)
            {
                terrain.RestoreSpot(item.transform.position);
                Collected?.Invoke(item);
                CheckSector(item.Sector);
                StateChanged?.Invoke();
            }
        }

        private void OnWoodCleared(FallenWood w)
        {
            terrain.RestoreSpot(w.transform.position);
            WoodCleared?.Invoke(w);
            CheckSector(w.Sector);
            CheckValle();
            StateChanged?.Invoke();
        }

        private void OnDeposited(RecycleBin bin, List<LitterItem> items)
        {
            foreach (var i in items) deposited.Add(i.ItemId);
            DepositedItems?.Invoke(bin, items.Count);
            CheckValle();
            StateChanged?.Invoke();
        }

        private void Reject(string hint)
        {
            Mistakes++; LastHint = hint; LastHintTime = Time.time;
            AttemptRejected?.Invoke(hint);
            StateChanged?.Invoke();
        }

        private void CheckSector(int index)
        {
            var s = sectors.FirstOrDefault(x => x.Index == index);
            if (s == null || !s.IsClean || terrain.SectorLevel(index) > 0f) return;
            terrain.RestoreSector(index);
            if (s.RestoredDecor != null) s.RestoredDecor.SetActive(true);
            SectorRestored?.Invoke(s);
        }

        private void CheckValle()
        {
            if (IsRestored || SectorsClean < sectors.Length || DepositedCount < LitterTotal) return;
            IsRestored = true;
            ApplyVisuals();
            ValleRestored?.Invoke();
            if (Application.isPlaying && isActiveAndEnabled) faunaStart = StartCoroutine(StartFauna());
        }

        private IEnumerator StartFauna()
        {
            yield return new WaitForSeconds(2.2f);
            faunaStart = null;
            if (IsRestored) fauna.Begin();
        }

        private void OnFaunaArrived()
        {
            if (IsCompleted || !IsRestored) return;
            IsCompleted = true;
            GameProgress.CompleteZone(GameProgress.Valle);
            StateChanged?.Invoke();
        }

        private void ApplyVisuals()
        {
            degradedVisuals.SetActive(!IsRestored);
            restoredVisuals.SetActive(IsRestored);
            if (Application.isPlaying && input != null) input.enabled = !IsRestored;
            StateChanged?.Invoke();
        }

        public void Restart()
        {
            if (!initialized) return;
            if (faunaStart != null) { StopCoroutine(faunaStart); faunaStart = null; }
            foreach (var l in AllLitter) l.SetCollected(false);
            foreach (var w in AllWood) w.ResetWood();
            foreach (var s in sectors) if (s.RestoredDecor != null) s.RestoredDecor.SetActive(false);
            bag?.Clear(); deposited.Clear();
            terrain.ResetAll();
            fauna.ResetRoute();
            if (cinematic != null) cinematic.Stop();
            pulse?.ResetPulse();
            IsRestored = false; IsCompleted = false; ElapsedSeconds = 0; Mistakes = 0; LastHint = null; LastHintTime = -100f;
            player.Teleport(playerSpawn.position, playerSpawn.rotation);
            ApplyVisuals();
            if (Application.isPlaying && input != null) input.CaptureCursor();
        }

        public static string Clock(float seconds) => $"{(int)(seconds / 60)}:{(int)(seconds % 60):00}";
    }
}
