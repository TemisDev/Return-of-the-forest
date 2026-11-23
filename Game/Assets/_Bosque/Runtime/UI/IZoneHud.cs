namespace Bosque.UI
{
    /// <summary>
    /// Estado que cada zona expone a la interfaz común (ZoneHudView). La lógica sigue en la sesión de la zona;
    /// esto solo describe qué mostrar.
    /// </summary>
    public interface IZoneHud
    {
        int ZoneNumber { get; }
        string ZoneTitle { get; }
        string Objective { get; }
        int ProgressDone { get; }
        int ProgressTotal { get; }
        string ProgressLabel { get; }
        /// <summary>Tecla del aviso de acción ("E", "Q") o null si no hay acción disponible.</summary>
        string PromptKey { get; }
        string PromptText { get; }
        /// <summary>Pista tras un intento incorrecto, o null.</summary>
        string HintTitle { get; }
        string HintText { get; }
        string Tip { get; }
        bool HelpVisible { get; }
        string HelpTitle { get; }
        string HelpText { get; }
        /// <summary>La partida terminó (las tres zonas): se muestra el final.</summary>
        bool ShowEnding { get; }
        void RestartZone();
    }
}
