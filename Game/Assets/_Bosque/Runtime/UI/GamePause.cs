using System;
using UnityEngine;

namespace Bosque.UI
{
    /// <summary>Pausa global: detiene el tiempo y el audio del juego. La interfaz usa tiempo no escalado.</summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }
        public static event Action<bool> Changed;

        public static void Set(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            Changed?.Invoke(paused);
        }
    }
}
