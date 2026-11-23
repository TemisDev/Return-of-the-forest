using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.World
{
    /// <summary>
    /// Rastreo (T): durante unos segundos marca con columnas de luz los residuos y troncos pendientes cercanos.
    /// Tiene enfriamiento para que no sustituya a la exploración.
    /// </summary>
    public sealed class TrackingPulse : MonoBehaviour
    {
        [SerializeField] private GameObject beaconPrefab;
        [SerializeField] private float radius = 40f, showSeconds = 6f, cooldown = 20f;
        private readonly List<GameObject> pool = new List<GameObject>();
        private float lastUse = -100f, hideAt;
        private Func<IEnumerable<Vector3>> targets;

        public float CooldownLeft => Mathf.Max(0f, lastUse + cooldown - Time.time);
        public bool Ready => CooldownLeft <= 0f;
        public int LastMarked { get; private set; }
        public int Visible => pool.Count(b => b.activeSelf);
        public float Radius => radius;
        public event Action<int> Pulsed;

        public void Configure(GameObject beacon, Func<IEnumerable<Vector3>> pending) { beaconPrefab = beacon; targets = pending; }
        public void SetTargets(Func<IEnumerable<Vector3>> pending) => targets = pending;

        private void Update()
        {
            if (Time.time >= hideAt) foreach (var b in pool) if (b.activeSelf) b.SetActive(false);
            var kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame && Application.isFocused && Cursor.lockState == CursorLockMode.Locked) Pulse();
        }

        public bool Pulse()
        {
            if (!Ready || targets == null || !isActiveAndEnabled) return false;
            lastUse = Time.time; hideAt = Time.time + showSeconds;
            var near = targets().Where(p => (p - transform.position).sqrMagnitude < radius * radius).ToList();
            while (pool.Count < near.Count && beaconPrefab != null) { var b = Instantiate(beaconPrefab); b.SetActive(false); pool.Add(b); }
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < near.Count;
                if (on) pool[i].transform.position = near[i];
                pool[i].SetActive(on);
            }
            LastMarked = near.Count;
            Pulsed?.Invoke(near.Count);
            return true;
        }

        public void ResetPulse() { lastUse = -100f; hideAt = 0; foreach (var b in pool) b.SetActive(false); }
        private void OnDestroy() { foreach (var b in pool) if (b != null) Destroy(b); }
    }
}
