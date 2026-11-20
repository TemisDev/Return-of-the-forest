using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bosque.Audio
{
    /// <summary>
    /// Ambiente sonoro de una zona con dos estados (degradado / restaurado). Los bucles suenan siempre y solo se
    /// mezclan sus volúmenes, así que el cambio es progresivo y nunca reinicia ni duplica un sonido.
    /// No conoce el gameplay: un adaptador de zona llama a <see cref="SetRestored"/>.
    /// </summary>
    public sealed class AmbienceController : MonoBehaviour
    {
        [Serializable]
        public sealed class LoopLayer
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = .5f;
            [Tooltip("Peso en degradado (0) y restaurado (1). Ej.: viento seco 1→0,2; río 0→1.")]
            public Vector2 weightDegradedRestored = new Vector2(1f, 0f);
            [Tooltip("Vacío = 2D. Con ancla, suena en 3D desde esa posición (río, cascada...).")]
            public Transform anchor;
            public Vector3 position;
            public bool spatial;
            public float minDistance = 4f, maxDistance = 40f;
            [NonSerialized] public AudioSource source;
        }

        [Serializable]
        public sealed class RandomOneShots
        {
            public string name;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] public float volume = .4f;
            public Vector2 weightDegradedRestored = new Vector2(0f, 1f);
            public Vector2 intervalSeconds = new Vector2(3f, 9f);
            [Tooltip("Distancia al oyente a la que aparecen (en 3D).")]
            public Vector2 radius = new Vector2(6f, 18f);
            public Vector2 height = new Vector2(1f, 6f);
            public Vector2 pitch = new Vector2(.92f, 1.08f);
            [NonSerialized] public float nextTime;
        }

        public List<LoopLayer> loops = new List<LoopLayer>();
        public List<RandomOneShots> oneShots = new List<RandomOneShots>();
        [SerializeField] private float restoreFadeSeconds = 5f;
        [SerializeField] private float resetFadeSeconds = 1.2f;
        [SerializeField, Range(2, 12)] private int oneShotVoices = 6;

        private float blend, target;
        private AudioSource[] voices;
        private int nextVoice;
        private Transform listener;

        public float Blend => blend;
        public bool Restored => target > .5f;
        public int ActiveLoops { get { int n = 0; foreach (var l in loops) if (l.source != null && l.source.isPlaying) n++; return n; } }

        private void Awake()
        {
            foreach (var l in loops)
            {
                if (l.clip == null) continue;
                var go = new GameObject("Bucle " + l.name);
                go.transform.SetParent(l.anchor != null ? l.anchor : transform, false);
                if (l.anchor == null && l.spatial) go.transform.position = l.position;
                var s = go.AddComponent<AudioSource>();
                s.clip = l.clip; s.loop = true; s.playOnAwake = false; s.dopplerLevel = 0f;
                s.spatialBlend = l.spatial || l.anchor != null ? 1f : 0f;
                s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = l.minDistance; s.maxDistance = l.maxDistance;
                l.source = s;
            }
            voices = new AudioSource[oneShotVoices];
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("Puntual " + i); go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 1f; s.dopplerLevel = 0f;
                s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 3f; s.maxDistance = 35f; voices[i] = s;
            }
            Apply();
        }

        private void Start()
        {
            // Cada bucle arranca en un punto distinto para que dos zonas iguales no suenen en fase.
            foreach (var l in loops)
                if (l.source != null) { l.source.time = UnityEngine.Random.Range(0f, l.clip.length * .9f); l.source.Play(); }
            foreach (var o in oneShots) o.nextTime = Time.time + UnityEngine.Random.Range(o.intervalSeconds.x, o.intervalSeconds.y);
        }

        /// <summary>Cambia de estado. Con <paramref name="instant"/> no hay fundido (inicio o reinicio de la zona).</summary>
        public void SetRestored(bool restored, bool instant = false)
        {
            target = restored ? 1f : 0f;
            if (instant) { blend = target; Apply(); }
        }

        private void Update()
        {
            if (!Mathf.Approximately(blend, target))
            {
                float seconds = target > blend ? restoreFadeSeconds : resetFadeSeconds;
                blend = Mathf.MoveTowards(blend, target, Time.deltaTime / Mathf.Max(.05f, seconds));
            }
            Apply();
            if (listener == null) { var l = FindFirstObjectByType<AudioListener>(); listener = l != null ? l.transform : transform; }
            foreach (var o in oneShots)
            {
                if (Time.time < o.nextTime) continue;
                o.nextTime = Time.time + UnityEngine.Random.Range(o.intervalSeconds.x, o.intervalSeconds.y);
                float w = Weight(o.weightDegradedRestored);
                if (w < .05f || o.clips.Length == 0) continue;
                var s = voices[nextVoice]; nextVoice = (nextVoice + 1) % voices.Length;
                var dir = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) * Vector3.forward;
                s.transform.position = listener.position + dir * UnityEngine.Random.Range(o.radius.x, o.radius.y) +
                                       Vector3.up * UnityEngine.Random.Range(o.height.x, o.height.y);
                s.clip = o.clips[UnityEngine.Random.Range(0, o.clips.Length)];
                s.pitch = UnityEngine.Random.Range(o.pitch.x, o.pitch.y);
                s.volume = o.volume * w * BosqueAudio.Effective(AudioBus.Ambience);
                s.Play();
            }
        }

        private float Weight(Vector2 w) => Mathf.Lerp(w.x, w.y, Mathf.SmoothStep(0f, 1f, blend));

        private void Apply()
        {
            float bus = BosqueAudio.Effective(AudioBus.Ambience);
            foreach (var l in loops) if (l.source != null) l.source.volume = l.volume * Weight(l.weightDegradedRestored) * bus;
        }
    }
}
