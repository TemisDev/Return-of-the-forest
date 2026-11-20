using UnityEngine;

namespace Bosque.Audio
{
    /// <summary>Música ambiental en bucle con intensidad por estado (más presente al restaurar). Bus Music.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float degradedVolume = .22f;
        [SerializeField, Range(0f, 1f)] private float restoredVolume = .5f;
        [SerializeField] private float fadeSeconds = 6f;
        private AudioSource source;
        private float level, target;

        public float Level => level;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true; source.playOnAwake = false; source.spatialBlend = 0f;
            level = target = degradedVolume;
            Apply();
        }

        private void Start() { if (source.clip != null) source.Play(); }

        public void SetRestored(bool restored, bool instant = false)
        {
            target = restored ? restoredVolume : degradedVolume;
            if (instant) { level = target; Apply(); }
        }

        private void Update()
        {
            level = Mathf.MoveTowards(level, target, Time.deltaTime / Mathf.Max(.05f, fadeSeconds));
            Apply();
        }

        private void Apply() { if (source != null) source.volume = level * BosqueAudio.Effective(AudioBus.Music); }
    }
}
