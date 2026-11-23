using System;
using System.Collections;
using System.Linq;
using Bosque.Abilities;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>
    /// Zona 3, "El refugio perdido". Hay que devolver tres condiciones de refugio en el paso del animal:
    /// un tronco hueco y un montón de piedras colocados en los lugares indicados (E) y un matorral nativo que crece
    /// en suelo húmedo (Q). Con las tres, la zona se recupera y el zorro de neblina recorre el corredor hasta el refugio;
    /// su llegada completa la zona y, con las tres zonas, desbloquea el final.
    /// </summary>
    public sealed class RefugioSession : MonoBehaviour
    {
        [SerializeField] private RefugeItem[] items;
        [SerializeField] private RefugeSpot[] spots;
        [SerializeField] private GrowthPoint[] growthPoints;
        [SerializeField] private GameObject degradedVisuals;
        [SerializeField] private GameObject restoredVisuals;
        [SerializeField] private FaunaRoute fauna;
        [SerializeField] private FaunaCinematic cinematic;
        [SerializeField] private ThirdPersonMotor player;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private float faunaDelay = 1.8f;

        public RestorationProgress Progress { get; private set; }
        public ThirdPersonMotor Player => player;
        public RefugeItem[] Items => items;
        public RefugeSpot[] Spots => spots;
        public GrowthPoint[] GrowthPoints => growthPoints;
        public FaunaRoute Fauna => fauna;
        public FaunaCinematic Cinematic => cinematic;
        public int FailedAttempts { get; private set; }
        public string LastHint { get; private set; }
        public float LastHintTime { get; private set; } = -100f;
        /// <summary>Las tres zonas están completas: se muestra el final del juego.</summary>
        public bool GameFinished => Progress != null && Progress.IsCompleted &&
            GameProgress.IsZoneComplete(GameProgress.Arroyo) && GameProgress.IsZoneComplete(GameProgress.Claro) &&
            GameProgress.IsZoneComplete(GameProgress.Refugio);
        public event Action StateChanged;
        public event Action<RefugeSpot, RefugeItem> ItemPlaced;
        public event Action<GrowthPoint> PlantGrew;
        public event Action<string> AttemptRejected;
        public event Action FaunaStarted;

        private GrowthAbility ability;
        private ThirdPersonInput input;
        private ItemCarrier carrier;
        private Coroutine faunaStart;

        public void Configure(RefugeItem[] refugeItems, RefugeSpot[] refugeSpots, GrowthPoint[] points, GameObject degraded,
            GameObject restored, FaunaRoute route, FaunaCinematic shot, ThirdPersonMotor motor, Transform spawn)
        {
            items = refugeItems; spots = refugeSpots; growthPoints = points; degradedVisuals = degraded; restoredVisuals = restored;
            fauna = route; cinematic = shot; player = motor; playerSpawn = spawn; Progress = null;
        }

        private void Start() => InitializeFromScene();

        public void InitializeFromScene()
        {
            if (Progress != null) return;
            if (items == null || items.Length == 0 || spots == null || spots.Length == 0 || growthPoints == null ||
                player == null || playerSpawn == null || degradedVisuals == null || restoredVisuals == null || fauna == null)
                throw new ArgumentException("Faltan referencias del refugio.");
            if (spots.Count(s => s.Suitable) < items.Length) throw new ArgumentException("Faltan lugares aptos para los elementos.");
            Progress = new RestorationProgress(items.Select(i => i.ItemId).Concat(growthPoints.Where(p => p.Suitable).Select(p => p.PointId)));
            Progress.Changed += ApplyState;
            foreach (var s in spots) { s.Placed += OnPlaced; s.Rejected += OnSpotRejected; }
            foreach (var p in growthPoints) p.Grew += OnGrew;
            fauna.ArrivedAtRefuge += OnFaunaArrived;
            fauna.Started += OnFaunaStarted;
            ability = player.GetComponent<GrowthAbility>();
            input = player.GetComponent<ThirdPersonInput>();
            carrier = player.GetComponent<ItemCarrier>();
            if (ability != null) ability.Rejected += OnGrowthRejected;
            ApplyState();
        }

        private void OnDestroy()
        {
            if (Progress != null) Progress.Changed -= ApplyState;
            if (spots != null) foreach (var s in spots) if (s != null) { s.Placed -= OnPlaced; s.Rejected -= OnSpotRejected; }
            if (growthPoints != null) foreach (var p in growthPoints) if (p != null) p.Grew -= OnGrew;
            if (fauna != null) { fauna.ArrivedAtRefuge -= OnFaunaArrived; fauna.Started -= OnFaunaStarted; }
            if (ability != null) ability.Rejected -= OnGrowthRejected;
        }

        private void OnPlaced(RefugeSpot spot, RefugeItem item)
        {
            ItemPlaced?.Invoke(spot, item);
            Progress.TryRemove(item.ItemId);
        }

        private void OnGrew(GrowthPoint point)
        {
            PlantGrew?.Invoke(point);
            Progress.TryRemove(point.PointId);
        }

        private void OnSpotRejected(RefugeSpot spot, string hint) => Reject(hint);
        private void OnGrowthRejected(GrowthPoint point, string hint) => Reject(hint);

        private void Reject(string hint)
        {
            FailedAttempts++; LastHint = hint; LastHintTime = Time.time;
            AttemptRejected?.Invoke(hint);
            StateChanged?.Invoke();
        }

        private void OnFaunaStarted() => FaunaStarted?.Invoke();

        private void OnFaunaArrived()
        {
            if (Progress.TryComplete()) GameProgress.CompleteZone(GameProgress.Refugio);
        }

        private void ApplyState()
        {
            bool restored = Progress.IsRestored;
            degradedVisuals.SetActive(!restored);
            restoredVisuals.SetActive(restored);
            if (restored)
            {
                if (carrier != null && carrier.Holding != null) carrier.Drop();
                if (!fauna.Running && !fauna.Arrived && faunaStart == null && Application.isPlaying && isActiveAndEnabled)
                    faunaStart = StartCoroutine(StartFauna());
            }
            // Durante el regreso del animal y en el final la Guardiana se queda mirando.
            if (ability != null) ability.enabled = !restored;
            if (carrier != null) carrier.enabled = !restored;
            if (Application.isPlaying && input != null) input.enabled = !restored;
            StateChanged?.Invoke();
        }

        private IEnumerator StartFauna()
        {
            yield return new WaitForSeconds(faunaDelay);
            faunaStart = null;
            if (Progress.IsRestored) fauna.Begin();
        }

        public void Restart()
        {
            if (Progress == null) return;
            if (faunaStart != null) { StopCoroutine(faunaStart); faunaStart = null; }
            if (carrier != null && carrier.Holding != null) carrier.Holding.ResetItem();
            foreach (var i in items) i.ResetItem();
            foreach (var s in spots) s.ResetSpot();
            foreach (var p in growthPoints) p.ResetPoint();
            fauna.ResetRoute();
            if (cinematic != null) cinematic.Stop();
            player.Teleport(playerSpawn.position, playerSpawn.rotation);
            FailedAttempts = 0; LastHint = null; LastHintTime = -100f;
            Progress.Reset();
            if (Application.isPlaying && input != null) input.CaptureCursor();
        }
    }
}
