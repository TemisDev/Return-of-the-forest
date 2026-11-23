using System;
using System.Collections;
using Bosque.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.World
{
    /// <summary>
    /// Tronco o ramas caídas que bloquean el paso o el cauce. Hay que mantener E pulsada cerca durante unos segundos
    /// (más para los troncos gruesos); alejarse o soltar la tecla interrumpe el trabajo, que se va perdiendo poco a poco.
    /// </summary>
    public sealed class FallenWood : InteractionTarget
    {
        [SerializeField] private string woodId;
        [SerializeField] private string label = "tronco caído";
        [SerializeField] private float workSeconds = 2.5f;
        [SerializeField] private int sector;
        [SerializeField] private Collider shape;
        [SerializeField] private Transform visual;

        private PlayerInteractor worker;
        private Coroutine sinking;
        private Vector3 visualPos; private Vector3 visualScale = Vector3.one;
        public string WoodId => woodId;
        public string Label => label;
        public int Sector => sector;
        public float WorkSeconds => workSeconds;
        public float Work { get; private set; }
        public float Progress01 => Mathf.Clamp01(Work / workSeconds);
        public bool Cleared { get; private set; }
        public bool BeingWorked => worker != null;
        public override string Prompt => Work > 0f ? $"Mantén E: apartando {label}" : $"Mantén E para apartar {label}";
        public event Action<FallenWood> ClearedEvent;
        /// <summary>Con false, el trabajo solo avanza con <see cref="Tick"/> (pruebas automáticas).</summary>
        public static bool AutoTick { get; set; } = true;

        public void Configure(string id, string displayName, float seconds, int sectorIndex, Collider col, Transform vis)
        {
            woodId = id; label = displayName; workSeconds = seconds; sector = sectorIndex; shape = col; visual = vis;
            if (visual != null) { visualPos = visual.localPosition; visualScale = visual.localScale; }
        }

        private void Awake() { if (visual != null) { visualPos = visual.localPosition; visualScale = visual.localScale; } }

        public override bool CanInteract(PlayerInteractor actor) =>
            actor != null && isActiveAndEnabled && !Cleared && shape != null && shape.enabled;

        /// <summary>La pulsación de E empieza el trabajo; se completa manteniéndola (ver <see cref="Tick"/>).</summary>
        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor) || !actor.CanReach(this, shape)) return false;
            worker = actor;
            return false; // la interacción "real" ocurre al completar el trabajo
        }

        private void Update()
        {
            if (Cleared || !AutoTick) return;
            bool holding = worker != null && Keyboard.current != null && Keyboard.current.eKey.isPressed &&
                           Cursor.lockState == CursorLockMode.Locked;
            Tick(Time.deltaTime, holding);
        }

        /// <summary>Avanza el trabajo (público para pruebas). Sin mantener o fuera de alcance, el avance decae.</summary>
        public void Tick(float dt, bool holding)
        {
            if (Cleared) return;
            if (holding && (worker == null || !worker.isActiveAndEnabled || !worker.CanReach(this, shape))) holding = false;
            if (holding) Work += dt;
            else { Work = Mathf.Max(0f, Work - dt * .6f); if (Work <= 0f) worker = null; }
            if (visual != null && !Cleared) visual.localPosition = visualPos + (holding ? UnityEngine.Random.insideUnitSphere * .012f : Vector3.zero);
            if (Work >= workSeconds) Clear();
        }

        /// <summary>Solo pruebas: trabaja como si el actor mantuviera E.</summary>
        public void BeginWork(PlayerInteractor actor) { if (CanInteract(actor) && actor.CanReach(this, shape)) worker = actor; }

        private void Clear()
        {
            Cleared = true; Work = workSeconds; worker = null;
            if (shape != null) shape.enabled = false;
            if (visual != null && isActiveAndEnabled) sinking = StartCoroutine(Sink());
            ClearedEvent?.Invoke(this);
        }

        private IEnumerator Sink()
        {
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                float k = t / 1.2f;
                visual.localScale = visualScale * (1f - k * .9f);
                visual.localPosition = visualPos + Vector3.down * k * .5f;
                yield return null;
            }
            visual.gameObject.SetActive(false);
            sinking = null;
        }

        public void ResetWood()
        {
            if (sinking != null) { StopCoroutine(sinking); sinking = null; }
            Cleared = false; Work = 0; worker = null;
            if (shape != null) shape.enabled = true;
            if (visual != null) { visual.gameObject.SetActive(true); visual.localPosition = visualPos; visual.localScale = visualScale; }
        }
    }
}
