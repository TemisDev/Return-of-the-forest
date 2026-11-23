using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bosque.Audio
{
    /// <summary>
    /// Reproduce efectos de una <see cref="SfxLibrary"/> con un grupo fijo de AudioSources (sin crear objetos en
    /// tiempo de juego), variación de tono, enfriamiento por efecto y sin repetir la misma variación dos veces seguidas.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SfxLibrary library;
        [SerializeField, Range(4, 32)] private int voices = 14;
        private AudioSource[] pool;
        private int next;
        private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        private readonly Dictionary<string, int> lastClip = new Dictionary<string, int>();
        private readonly Dictionary<AudioSource, (AudioBus bus, float volume)> playing = new Dictionary<AudioSource, (AudioBus, float)>();

        /// <summary>Se emite cada vez que un efecto suena de verdad (id del efecto). Útil para pruebas y depuración.</summary>
        public event Action<string> Played;
        public SfxLibrary Library => library;
        public int ActiveVoices { get { int n = 0; if (pool != null) foreach (var s in pool) if (s.isPlaying) n++; return n; } }

        public void Configure(SfxLibrary lib) { library = lib; lastPlayed.Clear(); }

        private void Awake() => EnsurePool();
        private void OnEnable() => BosqueAudio.Changed += ApplyVolumes;
        private void OnDisable() => BosqueAudio.Changed -= ApplyVolumes;

        private void EnsurePool()
        {
            if (pool != null) return;
            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var go = new GameObject("Voz " + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.rolloffMode = AudioRolloffMode.Linear; s.dopplerLevel = 0f;
                pool[i] = s;
            }
        }

        /// <summary>Reproduce el efecto en una posición. Devuelve false si no existe, no tiene clips o está en enfriamiento.</summary>
        public bool Play(string id, Vector3 position, float volumeScale = 1f)
        {
            if (library == null) return false;
            var cue = library.Get(id);
            if (cue == null || cue.clips == null || cue.clips.Length == 0) return false;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float t) && now - t < cue.cooldown) return false;
            lastPlayed[id] = now;
            EnsurePool();
            int clipIndex = UnityEngine.Random.Range(0, cue.clips.Length);
            if (cue.clips.Length > 1 && lastClip.TryGetValue(id, out int prev) && prev == clipIndex)
                clipIndex = (clipIndex + 1) % cue.clips.Length;
            lastClip[id] = clipIndex;
            var source = NextVoice();
            source.transform.position = position;
            source.clip = cue.clips[clipIndex];
            source.spatialBlend = cue.spatialBlend;
            source.minDistance = cue.minDistance; source.maxDistance = cue.maxDistance;
            source.pitch = UnityEngine.Random.Range(cue.pitch.x, cue.pitch.y);
            float baseVolume = cue.volume * Mathf.Clamp01(volumeScale);
            playing[source] = (cue.bus, baseVolume);
            source.volume = baseVolume * BosqueAudio.Effective(cue.bus);
            source.Play();
            Played?.Invoke(id);
            return true;
        }

        private AudioSource NextVoice()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var s = pool[(next + i) % pool.Length];
                if (!s.isPlaying) { next = (next + i + 1) % pool.Length; return s; }
            }
            var steal = pool[next]; next = (next + 1) % pool.Length; steal.Stop(); return steal;
        }

        private void ApplyVolumes()
        {
            foreach (var kv in playing) if (kv.Key != null) kv.Key.volume = kv.Value.volume * BosqueAudio.Effective(kv.Value.bus);
        }

        /// <summary>Corta todos los efectos (reinicio de la zona).</summary>
        public void StopAll() { if (pool != null) foreach (var s in pool) s.Stop(); }
    }
}
