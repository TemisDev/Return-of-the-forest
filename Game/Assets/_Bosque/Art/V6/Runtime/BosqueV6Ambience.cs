using UnityEngine;
using UnityEngine.Rendering;

namespace Bosque.ArtV6
{
    /// <summary>
    /// Presentación visual V6. Solo lee si el grupo RestoredVisuals está activo (estado que ya
    /// controla ArroyoSession) y mezcla cielo, niebla, sol, post-proceso y reflejos. No guarda ni
    /// modifica progreso, no pausa, no toca Time.timeScale.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BosqueV6Ambience : MonoBehaviour
    {
        [Header("Estado observado (solo lectura)")]
        public GameObject restoredVisuals;

        [Header("Escena")]
        public Light sun;
        public Material skyBlend;
        public Volume degradedVolume;
        public Volume restoredVolume;
        public ReflectionProbe skyProbe;

        [Header("Degradado")]
        public Color degradedFog = new Color(0.70f, 0.64f, 0.54f);
        public float degradedFogDensity = 0.022f;
        public Color degradedSun = new Color(1.00f, 0.88f, 0.72f);
        public float degradedSunIntensity = 1.55f;

        [Header("Restaurado")]
        public Color restoredFog = new Color(0.60f, 0.70f, 0.74f);
        public float restoredFogDensity = 0.011f;
        public Color restoredSun = new Color(1.00f, 0.95f, 0.86f);
        public float restoredSunIntensity = 1.9f;

        [Header("Recuperación parcial (0..1) mientras no esté restaurado; la usa la zona 4 por sectores")]
        public float partial;

        [Header("Transición")]
        public float transitionSeconds = 3.5f;
        public GameObject restorationBurst;
        public Vector3 burstPosition = new Vector3(-2.5f, -0.2f, 18f);

        private float _blend = -1f;
        private bool _lastTarget;
        private float _nextEnvironmentUpdate;

        private bool TargetRestored => restoredVisuals != null && restoredVisuals.activeInHierarchy;

        private void OnEnable()
        {
            _lastTarget = TargetRestored;
            ApplyImmediate(_lastTarget);
        }

        /// <summary>Aplica un estado sin transición (inicio, reinicio o capturas de editor).</summary>
        public void ApplyImmediate(bool restored)
        {
            _blend = restored ? 1f : 0f;
            Apply(_blend);
            RefreshEnvironment();
        }

        private void Update()
        {
            bool target = TargetRestored;
            if (!Application.isPlaying)
            {
                if (_blend < 0f || target != _lastTarget) ApplyImmediate(target);
                _lastTarget = target;
                return;
            }

            if (target && !_lastTarget && restorationBurst != null)
                Instantiate(restorationBurst, burstPosition, Quaternion.identity);

            // Restaurar es una transición lenta; reiniciar vuelve rápido al estado inicial.
            float speed = 1f / Mathf.Max(0.05f, target ? transitionSeconds : 0.6f);
            float goal = target ? 1f : Mathf.Clamp01(partial);
            if (!Mathf.Approximately(_blend, goal))
            {
                _blend = Mathf.MoveTowards(_blend, goal, Time.unscaledDeltaTime * speed);
                Apply(Mathf.SmoothStep(0f, 1f, _blend));
                if (Time.unscaledTime >= _nextEnvironmentUpdate || Mathf.Approximately(_blend, goal))
                {
                    RefreshEnvironment();
                    _nextEnvironmentUpdate = Time.unscaledTime + 0.35f;
                }
            }
            _lastTarget = target;
        }

        private void Apply(float t)
        {
            if (skyBlend != null)
            {
                skyBlend.SetFloat("_Blend", t);
                if (RenderSettings.skybox != skyBlend) RenderSettings.skybox = skyBlend;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(degradedFog, restoredFog, t);
            RenderSettings.fogDensity = Mathf.Lerp(degradedFogDensity, restoredFogDensity, t);
            RenderSettings.ambientMode = AmbientMode.Skybox;
            if (sun != null)
            {
                sun.color = Color.Lerp(degradedSun, restoredSun, t);
                sun.intensity = Mathf.Lerp(degradedSunIntensity, restoredSunIntensity, t);
            }
            if (degradedVolume != null) degradedVolume.weight = 1f - t;
            if (restoredVolume != null) restoredVolume.weight = t;
        }

        private void RefreshEnvironment()
        {
            DynamicGI.UpdateEnvironment();
            if (skyProbe != null && skyProbe.isActiveAndEnabled) skyProbe.RenderProbe();
        }
    }
}
