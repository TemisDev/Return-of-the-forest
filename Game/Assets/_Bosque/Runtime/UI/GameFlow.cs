using System;
using Bosque.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.UI
{
    /// <summary>Escenas del juego, flujo entre ellas (con fundido) y textos de presentación de cada zona.</summary>
    public static class GameFlow
    {
        public const string MenuScene = "MenuPrincipal", ArroyoScene = "ArroyoPlayable", ClaroScene = "ClaroVacio", RefugioScene = "RefugioPerdido", ValleScene = "ValleOlvidado";
        public const string Version = "V14";

        public sealed class ZoneInfo
        {
            public int number; public string title, subtitle;
        }

        public sealed class Note
        {
            public string zone, heading, body, lesson, reward;
        }

        public static ZoneInfo Zone(string scene)
        {
            switch (scene)
            {
                case ArroyoScene: return new ZoneInfo { number = 1, title = "El arroyo dormido", subtitle = "Devuelve el agua al cauce" };
                case ClaroScene: return new ZoneInfo { number = 2, title = "El claro vacío", subtitle = "Haz brotar plantas nativas donde haya humedad" };
                case RefugioScene: return new ZoneInfo { number = 3, title = "El refugio perdido", subtitle = "Devuelve un refugio a la fauna" };
                case ValleScene: return new ZoneInfo { number = 4, title = "El valle olvidado", subtitle = "Limpia todo el valle: residuos al reciclaje y troncos fuera del paso" };
                default: return null;
            }
        }

        /// <summary>Nota del cuaderno de la cuidadora que se revela al recuperar cada zona (GDD 3.1).</summary>
        public static Note NoteFor(string zone)
        {
            switch (zone)
            {
                case GameProgress.Arroyo:
                    return new Note
                    {
                        zone = "Zona 1 · El arroyo dormido", heading = "El agua va primero",
                        body = "Ramas, rocas y troncos ahogaban el cauce. Cuando el agua vuelve a correr, la tierra de las orillas recuerda cómo sostener la vida.",
                        lesson = "Recuperaste el agua: sin ella no hay plantas ni animales.",
                        reward = "Nueva habilidad: CRECIMIENTO [Q]. Haz brotar plantas nativas donde el suelo tenga humedad.",
                    };
                case GameProgress.Claro:
                    return new Note
                    {
                        zone = "Zona 2 · El claro vacío", heading = "No basta con sembrar",
                        body = "Cada planta nativa necesita su suelo y su humedad. Donde la tierra es oscura y fresca, las raíces se quedan; en el suelo seco, la semilla se pierde.",
                        lesson = "Recuperaste la vegetación adecuada: plantas en el lugar correcto.",
                        reward = "Las raíces abrieron un paso hacia el refugio.",
                    };
                case GameProgress.Refugio:
                    return new Note
                    {
                        zone = "Zona 3 · El refugio perdido", heading = "Un lugar donde volver",
                        body = "Un tronco hueco, piedras y matorrales: sombra para descansar y escondite para criar. Si el refugio está en su camino, los animales regresan.",
                        lesson = "Recuperaste el refugio: el corredor vuelve a estar conectado.",
                        reward = "Más allá del refugio espera el valle grande, el más castigado de todos.",
                    };
                default:
                    return new Note
                    {
                        zone = "Zona 4 · El valle olvidado", heading = "Lo que dejamos atrás",
                        body = "Plástico, latas, llantas y troncos tirados ahogaban el valle. Cada residuo en su contenedor es un pedazo de suelo que vuelve a respirar.",
                        lesson = "Recuperaste el valle: sin basura, el agua corre limpia y la fauna regresa.",
                        reward = "",
                    };
            }
        }

        public static void LoadScene(string scene)
        {
            if (Presentation.Instance != null) Presentation.Instance.LoadWithFade(scene);
            else { GamePause.Set(false); SceneManager.LoadScene(scene); }
        }

        public static void NewGame()
        {
            GameProgress.ResetAll();
            LoadScene(ArroyoScene);
        }

        public static void BackToMenu() => LoadScene(MenuScene);

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static bool VerifierRunning => Array.IndexOf(Environment.GetCommandLineArgs(), "-arroyo-verify") >= 0;
    }
}
