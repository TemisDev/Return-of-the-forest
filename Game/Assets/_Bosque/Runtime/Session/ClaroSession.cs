using System;
using System.Collections;
using System.Linq;
using Bosque.Abilities;
using Bosque.Player;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>
    /// Zona 2, "El claro vacío": las plantas nativas solo prosperan en suelo húmedo. Al crecer en los tres lechos
    /// aptos el claro se recupera y un puente de raíces cruza la barranca hacia la salida.
    /// Usa el mismo modelo de progreso que el arroyo (RestorationProgress) con los lechos aptos como objetivos.
    /// </summary>
    public sealed class ClaroSession : MonoBehaviour
    {
        [SerializeField] private GrowthPoint[] points;
        [SerializeField] private GameObject degradedVisuals;
        [SerializeField] private GameObject restoredVisuals;
        [SerializeField] private GameObject rootBridge;
        [SerializeField] private float bridgeGrowSeconds = 2.4f;
        [SerializeField] private ThirdPersonMotor player;
        [SerializeField] private Transform playerSpawn;

        public RestorationProgress Progress { get; private set; }
        public ThirdPersonMotor Player => player;
        public GrowthPoint[] Points => points;
        public GameObject RootBridge => rootBridge;
        public int FailedAttempts { get; private set; }
        public string LastHint { get; private set; }
        public float LastHintTime { get; private set; } = -100f;
        public event Action StateChanged;
        public event Action<GrowthPoint> PlantGrew;
        public event Action<GrowthPoint, string> AttemptRejected;

        private GrowthAbility ability;
        private ThirdPersonInput input;
        private Coroutine bridgeAnim;

        public void Configure(GrowthPoint[] growthPoints, GameObject degraded, GameObject restored, GameObject bridge,
            ThirdPersonMotor motor, Transform spawn)
        {
            points = growthPoints; degradedVisuals = degraded; restoredVisuals = restored; rootBridge = bridge;
            player = motor; playerSpawn = spawn; Progress = null;
        }

        private void Start() => InitializeFromScene();

        public void InitializeFromScene()
        {
            if (Progress != null) return;
            if (points == null || points.Length == 0 || player == null || playerSpawn == null || degradedVisuals == null ||
                restoredVisuals == null || rootBridge == null)
                throw new ArgumentException("Faltan referencias del claro.");
            Progress = new RestorationProgress(points.Where(p => p.Suitable).Select(p => p.PointId));
            Progress.Changed += ApplyState;
            foreach (var p in points) p.Grew += OnPointGrew;
            ability = player.GetComponent<GrowthAbility>();
            input = player.GetComponent<ThirdPersonInput>();
            if (ability != null) ability.Rejected += OnRejected;
            ApplyState();
        }

        private void OnDestroy()
        {
            if (Progress != null) Progress.Changed -= ApplyState;
            if (points != null) foreach (var p in points) if (p != null) p.Grew -= OnPointGrew;
            if (ability != null) ability.Rejected -= OnRejected;
        }

        private void OnPointGrew(GrowthPoint point)
        {
            Progress.TryRemove(point.PointId);
            PlantGrew?.Invoke(point);
        }

        private void OnRejected(GrowthPoint point, string hint)
        {
            FailedAttempts++; LastHint = hint; LastHintTime = Time.time;
            AttemptRejected?.Invoke(point, hint);
            StateChanged?.Invoke();
        }

        private void ApplyState()
        {
            bool restored = Progress.IsRestored;
            degradedVisuals.SetActive(!restored);
            bool wasActive = restoredVisuals.activeSelf;
            restoredVisuals.SetActive(restored);
            if (restored && !rootBridge.activeSelf)
            {
                rootBridge.SetActive(true);
                if (Application.isPlaying && isActiveAndEnabled)
                {
                    if (bridgeAnim != null) StopCoroutine(bridgeAnim);
                    bridgeAnim = StartCoroutine(GrowthReveal.Animate(rootBridge.transform, bridgeGrowSeconds));
                }
            }
            else if (!restored)
            {
                if (bridgeAnim != null) { StopCoroutine(bridgeAnim); bridgeAnim = null; }
                rootBridge.transform.localScale = Vector3.one;
                rootBridge.SetActive(false);
            }
            if (ability != null) ability.enabled = !Progress.IsCompleted;
            if (Application.isPlaying && input != null) input.enabled = !Progress.IsCompleted;
            if (Progress.IsCompleted) GameProgress.CompleteZone(GameProgress.Claro);
            StateChanged?.Invoke();
        }

        public void Restart()
        {
            if (Progress == null) return;
            player.Teleport(playerSpawn.position, playerSpawn.rotation);
            foreach (var p in points) p.ResetPoint();
            FailedAttempts = 0; LastHint = null; LastHintTime = -100f;
            Progress.Reset();
            if (Application.isPlaying && input != null) input.CaptureCursor();
        }

        public bool TryExit(ThirdPersonMotor actor) =>
            actor != null && actor == player && Progress != null && Progress.TryComplete();
    }
}
