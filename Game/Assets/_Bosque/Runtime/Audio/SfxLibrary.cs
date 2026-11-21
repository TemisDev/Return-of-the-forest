using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bosque.Audio
{
    /// <summary>Un efecto con variaciones. Reutilizable en cualquier zona.</summary>
    [Serializable]
    public sealed class SfxCue
    {
        public string id;
        public AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)] public float volume = .8f;
        public Vector2 pitch = new Vector2(.95f, 1.05f);
        [Tooltip("0 = 2D (interfaz), 1 = 3D (en el mundo)")] [Range(0f, 1f)] public float spatialBlend = 1f;
        public float minDistance = 1.5f;
        public float maxDistance = 25f;
        [Tooltip("Tiempo mínimo entre dos reproducciones del mismo efecto (evita repeticiones al mantener una tecla).")]
        public float cooldown = .05f;
        public AudioBus bus = AudioBus.Sfx;
    }

    [CreateAssetMenu(menuName = "Bosque/Audio/Biblioteca de efectos", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        public List<SfxCue> cues = new List<SfxCue>();
        private Dictionary<string, SfxCue> map;

        public SfxCue Get(string id)
        {
            if (map == null || map.Count != cues.Count)
            {
                map = new Dictionary<string, SfxCue>();
                foreach (var cue in cues) if (cue != null && !string.IsNullOrEmpty(cue.id)) map[cue.id] = cue;
            }
            return id != null && map.TryGetValue(id, out var c) ? c : null;
        }

        private void OnValidate() => map = null;
    }
}
