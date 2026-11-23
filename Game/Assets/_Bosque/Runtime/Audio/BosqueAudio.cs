using System;
using UnityEngine;

namespace Bosque.Audio
{
    public enum AudioBus { Master = 0, Music = 1, Ambience = 2, Sfx = 3 }

    /// <summary>
    /// Volúmenes por bus (lineales 0..1) con persistencia. API para el futuro menú de opciones:
    /// <c>BosqueAudio.SetVolume(AudioBus.Music, 0.5f)</c>. Las fuentes del juego multiplican su volumen
    /// por <see cref="Effective"/> y escuchan <see cref="Changed"/>.
    /// </summary>
    public static class BosqueAudio
    {
        private static readonly float[] Volumes = { 1f, .8f, 1f, 1f };
        private static bool loaded;
        public static event Action Changed;

        private static string Key(AudioBus bus) => "bosque.audio." + bus.ToString().ToLowerInvariant();

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
                Volumes[(int)bus] = Mathf.Clamp01(PlayerPrefs.GetFloat(Key(bus), Volumes[(int)bus]));
        }

        public static float GetVolume(AudioBus bus) { Load(); return Volumes[(int)bus]; }

        public static void SetVolume(AudioBus bus, float linear, bool persist = true)
        {
            Load();
            Volumes[(int)bus] = Mathf.Clamp01(linear);
            if (persist) { PlayerPrefs.SetFloat(Key(bus), Volumes[(int)bus]); PlayerPrefs.Save(); }
            Changed?.Invoke();
        }

        /// <summary>Volumen final de un bus (Master × bus).</summary>
        public static float Effective(AudioBus bus) =>
            bus == AudioBus.Master ? GetVolume(AudioBus.Master) : GetVolume(AudioBus.Master) * GetVolume(bus);

        /// <summary>Restaura los valores por defecto (para pruebas o un botón "restablecer").</summary>
        public static void ResetToDefaults(bool persist = false)
        {
            loaded = true;
            Volumes[0] = 1f; Volumes[1] = .8f; Volumes[2] = 1f; Volumes[3] = 1f;
            if (persist) foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus))) PlayerPrefs.SetFloat(Key(bus), Volumes[(int)bus]);
            Changed?.Invoke();
        }
    }
}
