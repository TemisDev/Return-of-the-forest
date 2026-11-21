using System;
using Bosque.Player;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// La Guardiana en el agua del valle: vadear frena (más cuanto más hondo) y el agua demasiado profunda no deja
    /// pasar, como una pared: hay que cruzar por un puente o un vado.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonMotor))]
    public sealed class WaterWading : MonoBehaviour
    {
        [SerializeField] private ValleWater water;
        [SerializeField] private GameObject ripples;
        [SerializeField] private float wetDepth = .12f, maxDepth = .95f;
        [SerializeField, Range(.2f, 1f)] private float slowestFactor = .45f;
        private ThirdPersonMotor motor;
        private float lastWarning = -100f;

        public float Depth { get; private set; }
        public bool InWater => Depth > wetDepth;
        public float MaxDepth => maxDepth;
        public ValleWater Water => water;
        /// <summary>Se intentó entrar en agua demasiado profunda (como mucho una vez cada pocos segundos).</summary>
        public event Action DeepBlocked;

        public void Configure(ValleWater w, GameObject rippleFx) { water = w; ripples = rippleFx; }

        private void Awake()
        {
            motor = GetComponent<ThirdPersonMotor>();
            motor.MoveBlocked = Blocked;
            if (ripples != null) ripples.SetActive(false);
        }

        private bool Blocked(Vector3 from, Vector3 to)
        {
            if (water == null) return false;
            float d = water.DepthAt(to);
            // Si ya está en agua honda (p. ej., tras un salto), solo se le deja ir hacia lo menos profundo.
            if (d <= maxDepth || d <= water.DepthAt(from)) return false;
            if (Time.time - lastWarning > 4f) { lastWarning = Time.time; DeepBlocked?.Invoke(); }
            return true;
        }

        private void Update()
        {
            Depth = water != null ? water.DepthAt(transform.position) : 0f;
            motor.SpeedFactor = InWater ? Mathf.Lerp(.8f, slowestFactor, Mathf.InverseLerp(wetDepth, maxDepth, Depth)) : 1f;
            if (ripples == null) return;
            if (ripples.activeSelf != InWater) ripples.SetActive(InWater);
            if (InWater) ripples.transform.position = transform.position + Vector3.up * Depth;
        }

        private void OnDisable() { if (motor != null) motor.SpeedFactor = 1f; }
    }
}
