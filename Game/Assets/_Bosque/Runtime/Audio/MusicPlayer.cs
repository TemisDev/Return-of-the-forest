using UnityEngine;

namespace Bosque.Audio
{
    /// <summary>
    /// Música ambiental en bucle por estado (bus Music). Si tiene pista de lo degradado, la zona suena con ese pad
    /// sombrío y, al restaurarse, se funde en cruz con el tema del bosque (clip del AudioSource); si no, solo sube el
    /// volumen del tema. <see cref="Level"/> sube al restaurar en ambos casos.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float degradedVolume = .22f;
        [SerializeField, Range(0f, 1f)] private float restoredVolume = .5f;
        [SerializeField] private float fadeSeconds = 6f;
        [Tooltip("Pista de la zona degradada (opcional). El clip del AudioSource es el tema del bosque restaurado.")]
        [SerializeField] private AudioClip degradedClip;
        [SerializeField, Range(0f, 1f)] private float degradedTrackVolume = .42f;
        private AudioSource source, degradedSource;
        private float level, target, mix, mixTarget;

        public float Level => level;
        /// <summary>0 = pista de lo degradado, 1 = tema del bosque.</summary>
        public float Mix => mix;
        public AudioClip DegradedClip => degradedClip;

        public void SetDegradedClip(AudioClip clip) => degradedClip = clip;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true; source.playOnAwake = false; source.spatialBlend = 0f;
            if (degradedClip != null)
            {
                degradedSource = gameObject.AddComponent<AudioSource>();
                degradedSource.clip = degradedClip; degradedSource.loop = true; degradedSource.playOnAwake = false;
                degradedSource.spatialBlend = 0f; degradedSource.priority = source.priority;
            }
            level = target = degradedVolume; mix = mixTarget = degradedClip != null ? 0f : 1f;
            Apply();
        }

        private void Start()
        {
            if (source.clip != null) source.Play();
            if (degradedSource != null) degradedSource.Play();
        }

        public void SetRestored(bool restored, bool instant = false)
        {
            target = restored ? restoredVolume : degradedVolume;
            mixTarget = degradedSource == null || restored ? 1f : 0f;
            if (instant) { level = target; mix = mixTarget; Apply(); }
        }

        private void Update()
        {
            float step = Time.deltaTime / Mathf.Max(.05f, fadeSeconds);
            level = Mathf.MoveTowards(level, target, step);
            mix = Mathf.MoveTowards(mix, mixTarget, step);
            Apply();
        }

        private void Apply()
        {
            float bus = BosqueAudio.Effective(AudioBus.Music);
            if (degradedSource == null) { if (source != null) source.volume = level * bus; return; }
            // Fundido de igual potencia: no hay hueco de volumen a mitad de la transición.
            float a = Mathf.Sin(mix * Mathf.PI * .5f), b = Mathf.Cos(mix * Mathf.PI * .5f);
            source.volume = restoredVolume * a * bus;
            degradedSource.volume = degradedTrackVolume * b * bus;
        }
    }
}
