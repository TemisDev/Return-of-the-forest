using Bosque.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>
    /// Menú principal: título, Comenzar (con introducción breve), Opciones, Controles, Créditos y Salir.
    /// La cámara gira despacio alrededor del bosque restaurado del fondo. Avisa que el progreso no se guarda (GDD 1.4).
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private AmbienceController ambience;
        [SerializeField] private MusicPlayer music;
        [SerializeField] private Vector3 orbitCenter = new Vector3(0, 1.1f, 0);
        [SerializeField] private float orbitRadius = 6f, orbitHeight = 1.9f, orbitDegreesPerSecond = 2.2f;

        public static readonly string[] IntroPages =
        {
            "Hace años, este bosque era un corredor de vida: el agua bajaba por el arroyo, las plantas nativas cubrían el claro y los animales tenían dónde refugiarse.",
            "El abandono lo fragmentó. El arroyo se quedó dormido, el claro se secó y la fauna perdió su refugio.",
            "Una joven guardiana regresa con el cuaderno de su antigua cuidadora. Sus notas enseñan a observar… y a reparar, paso a paso.",
        };

        private RectTransform main, intro;
        private GameObject panel;
        private TextMeshProUGUI introText, introCount;
        private Button introNext;
        private int page;
        private float angle;
        public bool IntroVisible => intro != null && intro.gameObject.activeSelf;
        public int IntroPage => page;
        public bool PanelOpen => panel != null;

        public void Configure(Transform cam, AmbienceController amb, MusicPlayer mus) { cameraRig = cam; ambience = amb; music = mus; }

        private void Start()
        {
            Time.timeScale = 1f; AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (ambience != null) ambience.SetRestored(true, true);
            if (music != null) music.SetRestored(true, false);
            var t = Ui.Theme;
            var canvas = Ui.Canvas("LienzoMenu", 20, transform);
            // V14: sin cajas. Un degradado lateral deja ver el bosque; título en versalitas y menú de solo texto.
            var shade = Ui.Image(canvas.transform, "Sombra", new Color(0, 0, 0, .82f), UiKit.FadeRight).rectTransform;
            shade.anchorMin = new Vector2(0, 0); shade.anchorMax = new Vector2(0, 1); shade.pivot = new Vector2(0, .5f); shade.sizeDelta = new Vector2(1250, 0); shade.anchoredPosition = Vector2.zero;
            var floor = Ui.Image(canvas.transform, "SombraInferior", new Color(0, 0, 0, .5f), UiKit.FadeUp).rectTransform;
            floor.anchorMin = Vector2.zero; floor.anchorMax = new Vector2(1, 0); floor.pivot = new Vector2(.5f, 0); floor.sizeDelta = new Vector2(0, 220); floor.anchoredPosition = Vector2.zero;

            main = Ui.Rect("Principal", canvas.transform).Stretch();
            Ui.Image(main, "Rombo", UiTheme.Amber, UiKit.Diamond).rectTransform.Place(new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(148, -180), new Vector2(14, 14));
            var kicker = UiKit.Shadowed(Ui.Text(main, "Antetitulo", "Un viaje por el bosque andino", 24, t?.mapCapsBold ?? t?.bold, UiTheme.Moss, TextAlignmentOptions.MidlineLeft));
            kicker.characterSpacing = 9; kicker.rectTransform.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(168, -180), new Vector2(900, 36));
            var title = UiKit.Shadowed(Ui.Text(main, "Titulo", "Bosque de regreso", 112, t?.mapCapsBold ?? t?.title, UiTheme.Cream, TextAlignmentOptions.TopLeft), .5f);
            title.characterSpacing = 1; title.textWrappingMode = TextWrappingModes.NoWrap;
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(134, -204), new Vector2(1200, 140));
            Ui.Image(main, "Linea", new Color(.96f, .92f, .8f, .35f)).rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(142, -350), new Vector2(460, 1.5f));
            var sub = UiKit.Shadowed(Ui.Text(main, "Subtitulo", "Reconecta un bosque andino: agua, plantas nativas y refugio", 30, t?.italic, UiTheme.CreamDim));
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(144, -366), new Vector2(1000, 50));
            var list = Ui.Rect("Botones", main); list.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -460), new Vector2(560, 380));
            Ui.Column(list, 4, TextAnchor.UpperLeft);
            var start = Ui.MenuItem(list, "Comenzar", ShowIntro, 560, 66, 38);
            Ui.MenuItem(list, "Opciones", () => Open(MenuPanels.Options(canvas.transform, Close)));
            Ui.MenuItem(list, "Controles", () => Open(MenuPanels.Controls(canvas.transform, Close)));
            Ui.MenuItem(list, "Créditos", () => Open(MenuPanels.Credits(canvas.transform, Close)));
            Ui.MenuItem(list, "Salir", GameFlow.Quit);
            var ods = UiKit.Shadowed(Ui.Text(main, "ODS", "ODS 15 · Vida de ecosistemas terrestres", 21, t?.mapCapsBold ?? t?.bold, UiTheme.Moss, TextAlignmentOptions.BottomLeft)); ods.characterSpacing = 5;
            ods.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(146, 104), new Vector2(900, 34));
            var notice = UiKit.Shadowed(Ui.Text(main, "Aviso", "El progreso no se guarda al cerrar el juego. Una partida completa dura unos 15 minutos.", 21, t?.body, new Color(1, 1, 1, .7f), TextAlignmentOptions.BottomLeft));
            notice.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(146, 70), new Vector2(1100, 34));
            var keys = new KeyHintRow(main, "Teclas", TextAnchor.MiddleRight);
            keys.Rect.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-44, 64), new Vector2(700, 40));
            keys.Set("Enter: aceptar · Esc: volver");
            var ver = Ui.Text(main, "Version", GameFlow.Version, 19, t?.body, new Color(1, 1, 1, .4f), TextAlignmentOptions.BottomRight);
            ver.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-44, 26), new Vector2(300, 30));

            // Introducción: tres páginas sobre papel, con Siguiente y Saltar.
            intro = Ui.Rect("Introduccion", canvas.transform).Stretch();
            Ui.Image(intro, "Velo", new Color(0, 0, 0, .6f), null, true).rectTransform.Stretch();
            var paper = Ui.Image(intro, "Pagina", UiTheme.PaperTint, t?.paper, true).rectTransform;
            paper.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1100, 560));
            paper.localRotation = Quaternion.Euler(0, 0, .6f);
            introText = Ui.Text(paper, "Texto", "", 44, t?.handwriting, UiTheme.Ink, TextAlignmentOptions.Center); introText.lineSpacing = 8;
            introText.rectTransform.Stretch(110, 110, 80, 170);
            introCount = Ui.Text(paper, "Pagina", "", 24, t?.bold, new Color(.42f, .35f, .25f), TextAlignmentOptions.Bottom);
            introCount.rectTransform.Stretch(0, 0, 400, 120);
            var row = Ui.Rect("Botones", paper); row.Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 36), new Vector2(800, 66));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 24; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = false;
            Ui.Button(row, "Saltar", StartGame, 260, 60, 26);
            introNext = Ui.Button(row, "Siguiente", NextPage, 320, 60, 28);
            intro.gameObject.SetActive(false);
            start.Select();
            if (cameraRig != null) angle = Mathf.Atan2(cameraRig.position.x - orbitCenter.x, cameraRig.position.z - orbitCenter.z) * Mathf.Rad2Deg;
        }

        private void Open(GameObject p) { Close(); panel = p; main.gameObject.SetActive(false); }

        public void Close()
        {
            if (panel != null) { Destroy(panel); panel = null; }
            main.gameObject.SetActive(true);
            main.GetComponentInChildren<Button>().Select();
        }

        public void ShowIntro()
        {
            page = 0; intro.gameObject.SetActive(true); main.gameObject.SetActive(false); ShowPage();
        }

        private void ShowPage()
        {
            introText.text = IntroPages[page];
            introCount.text = (page + 1) + " / " + IntroPages.Length;
            introNext.GetComponentInChildren<TextMeshProUGUI>().text = page == IntroPages.Length - 1 ? "Comenzar" : "Siguiente";
            introNext.Select();
        }

        public void NextPage()
        {
            if (page < IntroPages.Length - 1) { page++; ShowPage(); }
            else StartGame();
        }

        public void StartGame() => GameFlow.NewGame();

        private void Update()
        {
            if (cameraRig != null)
            {
                angle += orbitDegreesPerSecond * Time.unscaledDeltaTime;
                var a = angle * Mathf.Deg2Rad;
                cameraRig.position = orbitCenter + new Vector3(Mathf.Sin(a) * orbitRadius, orbitHeight, Mathf.Cos(a) * orbitRadius);
                cameraRig.rotation = Quaternion.LookRotation(orbitCenter + Vector3.up * .3f - cameraRig.position);
            }
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (panel != null) Close();
            else if (IntroVisible) { intro.gameObject.SetActive(false); main.gameObject.SetActive(true); main.GetComponentInChildren<Button>().Select(); }
        }
    }
}
