using TMPro;
using UnityEngine;

namespace Bosque.UI
{
    /// <summary>
    /// Recursos visuales de la interfaz (tipografías, fondos e iconos). Se genera con
    /// Bosque.PresentationEditor.PresentationBuilder y se carga desde Resources/BosqueUiTheme.
    /// </summary>
    [CreateAssetMenu(menuName = "Bosque/Tema de interfaz")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Tipografías (OFL)")]
        public TMP_FontAsset body;
        public TMP_FontAsset bold;
        public TMP_FontAsset title;
        public TMP_FontAsset italic;
        public TMP_FontAsset handwriting;
        [Tooltip("Serif de cartografía (Alegreya): nombres del mapa.")]
        public TMP_FontAsset mapSerif;
        public TMP_FontAsset mapSerifItalic;
        [Tooltip("Versalitas (Alegreya SC): regiones y títulos del mapa.")]
        public TMP_FontAsset mapCaps;
        public TMP_FontAsset mapCapsBold;
        [Header("Fondos")]
        public Sprite panel;
        public Sprite paper;
        public Sprite pill;
        public Sprite glow;
        [Header("Iconos")]
        public Texture2D interactionIcon;
        public Texture2D progressIcon;
        public Texture2D restorationIcon;
        public Texture2D exitIcon;
        [Header("Textos")]
        public TextAsset credits;

        public static readonly Color Cream = new Color(.96f, .94f, .87f);
        public static readonly Color CreamDim = new Color(.82f, .82f, .74f);
        public static readonly Color Moss = new Color(.69f, .82f, .5f);
        public static readonly Color MossDark = new Color(.33f, .45f, .25f);
        public static readonly Color Amber = new Color(.97f, .78f, .43f);
        public static readonly Color Panel = new Color(.05f, .09f, .075f, .86f);
        public static readonly Color PanelSolid = new Color(.06f, .1f, .085f, .97f);
        public static readonly Color Ink = new Color(.2f, .15f, .1f);
        public static readonly Color PaperTint = new Color(.95f, .91f, .8f);
    }
}
