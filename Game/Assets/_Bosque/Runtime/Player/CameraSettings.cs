using UnityEngine;

namespace Bosque.Player
{
    /// <summary>Preferencias de cámara del menú de opciones (persisten en PlayerPrefs; no es progreso de partida).</summary>
    public static class CameraSettings
    {
        public const float DefaultSensitivity = 1f;
        private const string SensKey = "bosque.camara.sensibilidad", InvertKey = "bosque.camara.invertir";
        private static bool loaded;
        private static float sensitivity = DefaultSensitivity;
        private static bool invertY;

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensKey, DefaultSensitivity), .2f, 3f);
            invertY = PlayerPrefs.GetInt(InvertKey, 0) == 1;
        }

        public static float Sensitivity
        {
            get { Load(); return sensitivity; }
            set { Load(); sensitivity = Mathf.Clamp(value, .2f, 3f); PlayerPrefs.SetFloat(SensKey, sensitivity); PlayerPrefs.Save(); }
        }

        public static bool InvertY
        {
            get { Load(); return invertY; }
            set { Load(); invertY = value; PlayerPrefs.SetInt(InvertKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void ResetToDefaults() { Sensitivity = DefaultSensitivity; InvertY = false; }
    }
}
