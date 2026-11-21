using System;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    public sealed class ArroyoSession : MonoBehaviour
    {
        [SerializeField] private RemovableObstacle[] obstacles;
        [SerializeField] private GameObject degradedVisuals;
        [SerializeField] private GameObject restoredVisuals;
        [SerializeField] private GameObject exitBarrier;
        [SerializeField] private ThirdPersonMotor player;
        [SerializeField] private Transform playerSpawn;
        public RestorationProgress Progress { get; private set; }
        public ThirdPersonMotor Player => player;
        public event Action StateChanged;
        private PlayerInteractor interactor;
        private ThirdPersonInput input;
        private void Start() => InitializeFromScene();
        public void InitializeFromScene()
        {
            if (Progress == null) Configure(obstacles, degradedVisuals, restoredVisuals, exitBarrier, player, playerSpawn);
        }
        private void OnDestroy() { if (Progress != null) Progress.Changed -= ApplyState; }
        public void Configure(RemovableObstacle[] obstacles, GameObject degraded, GameObject restored,
            GameObject barrier, ThirdPersonMotor player, Transform spawn)
        {
            if (obstacles == null || player == null || spawn == null || degraded == null || restored == null || barrier == null)
                throw new ArgumentException("Faltan referencias de integración del arroyo.");
            var ids = new string[obstacles.Length];
            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] == null) throw new ArgumentException("Obstáculo sin referencia.");
                ids[i] = obstacles[i].ObstacleId;
            }
            var progress = new RestorationProgress(ids);
            if (Progress != null) Progress.Changed -= ApplyState;
            this.obstacles = obstacles; degradedVisuals = degraded; restoredVisuals = restored;
            exitBarrier = barrier; this.player = player; playerSpawn = spawn;
            interactor = player.GetComponent<PlayerInteractor>(); input = player.GetComponent<ThirdPersonInput>();
            Progress = progress;
            foreach (var obstacle in obstacles) obstacle.Bind(this);
            Progress.Changed += ApplyState;
            ApplyState();
        }
        private void ApplyState()
        {
            foreach (var obstacle in obstacles)
                obstacle.gameObject.SetActive(!Progress.IsRemoved(obstacle.ObstacleId));
            degradedVisuals.SetActive(!Progress.IsRestored);
            restoredVisuals.SetActive(Progress.IsRestored);
            exitBarrier.SetActive(!Progress.IsRestored);
            if (interactor != null) { interactor.ClearTarget(); interactor.enabled = !Progress.IsCompleted; }
            if (Application.isPlaying && input != null) input.enabled = !Progress.IsCompleted;
            StateChanged?.Invoke();
        }
        public void Restart()
        {
            if (Progress == null) return;
            player.Teleport(playerSpawn.position, playerSpawn.rotation);
            Progress.Reset();
            if (Application.isPlaying && input != null) input.CaptureCursor();
        }
        public bool TryExit(ThirdPersonMotor actor) =>
            actor != null && actor == player && Progress != null && Progress.TryComplete();
    }
}
