using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>HUD de zona (uGUI) alimentado por IZoneHud: objetivo, progreso, aviso de acción, pista y ayuda.</summary>
    public sealed class ZoneHudView
    {
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI zoneLabel, objective, progress, promptKey, promptText, hintTitle, hintText, tip, helpTitle, helpText;
        private readonly RectTransform pips, prompt, hint, help, side, workBar;
        private readonly TextMeshProUGUI sideTitle, sideText;
        private readonly Image workFill;
        private readonly List<Image> pipImages = new List<Image>();
        private IZoneHud model;
        private int lastDone = -1, lastTotal = -1;

        public IZoneHud Model => model;
        public bool Visible => group.alpha > .5f;
        public string ObjectiveText => objective.text;
        public string PromptShown => prompt.gameObject.activeSelf ? promptText.text : null;
        public string HintShown => hint.gameObject.activeSelf ? hintText.text : null;
        public string SideShown => side.gameObject.activeSelf ? sideText.text : null;
        public bool HelpShown => help.gameObject.activeSelf;

        public ZoneHudView(Transform root)
        {
            var t = Ui.Theme;
            var rt = Ui.Rect("HUD", root).Stretch();
            group = rt.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;

            var card = Ui.Panel(rt, "Objetivo").rectTransform;
            card.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -36), new Vector2(620, 236));
            var accent = Ui.Image(card, "Acento", UiTheme.Moss); accent.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -30), new Vector2(5, 30));
            zoneLabel = Ui.Text(card, "Zona", "", 24, t?.bold, UiTheme.Moss); zoneLabel.characterSpacing = 6;
            zoneLabel.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -28), new Vector2(560, 36));
            objective = Ui.Text(card, "Texto", "", 30, t?.body, UiTheme.Cream);
            objective.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -74), new Vector2(560, 96));
            pips = Ui.Rect("Progreso", card); pips.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 24), new Vector2(200, 40));
            var h = pips.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 8; h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = false;
            progress = Ui.Text(card, "Cuenta", "", 26, t?.body, UiTheme.CreamDim, TextAlignmentOptions.MidlineLeft);
            progress.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(240, 24), new Vector2(360, 40));

            prompt = Ui.Image(rt, "Aviso", UiTheme.Panel, t?.pill).rectTransform;
            prompt.Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 150), new Vector2(760, 84));
            var keyBg = Ui.Image(prompt, "Tecla", UiTheme.Moss, t?.pill); keyBg.rectTransform.Place(new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(62, 56));
            promptKey = Ui.Text(keyBg.transform, "Letra", "E", 34, t?.title, UiTheme.PanelSolid, TextAlignmentOptions.Center); promptKey.rectTransform.Stretch();
            promptText = Ui.Text(prompt, "Texto", "", 32, t?.bold, UiTheme.Cream, TextAlignmentOptions.MidlineLeft);
            promptText.rectTransform.Stretch(100, 24, 0, 0);

            hint = Ui.Panel(rt, "Pista").rectTransform;
            hint.Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 254), new Vector2(760, 150));
            var hAccent = Ui.Image(hint, "Acento", UiTheme.Amber); hAccent.rectTransform.Place(new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, 0), new Vector2(6, 150));
            hintTitle = Ui.Text(hint, "Titulo", "", 24, t?.bold, UiTheme.Amber); hintTitle.rectTransform.Stretch(30, 24, 16, 100);
            hintText = Ui.Text(hint, "Texto", "", 27, t?.body, UiTheme.Cream); hintText.rectTransform.Stretch(30, 24, 52, 12);

            tip = Ui.Text(rt, "Consejo", "", 23, t?.italic, new Color(1, 1, 1, .78f), TextAlignmentOptions.BottomLeft);
            tip.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 30), new Vector2(1100, 40));
            var keys = Ui.Text(rt, "Teclas", "Esc: pausa   ·   F1: ayuda   ·   R: reiniciar zona", 22, t?.body, new Color(1, 1, 1, .6f), TextAlignmentOptions.BottomRight);
            keys.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(700, 40));

            side = Ui.Panel(rt, "Lateral").rectTransform;
            side.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -36), new Vector2(470, 330));
            var sAccent = Ui.Image(side, "Acento", UiTheme.Moss); sAccent.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -30), new Vector2(5, 30));
            sideTitle = Ui.Text(side, "Titulo", "", 24, t?.bold, UiTheme.Moss); sideTitle.characterSpacing = 4;
            sideTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -28), new Vector2(400, 36));
            sideText = Ui.Text(side, "Texto", "", 25, t?.body, UiTheme.Cream); sideText.richText = true; sideText.lineSpacing = 4;
            sideText.rectTransform.Stretch(30, 24, 74, 18);
            side.gameObject.SetActive(false);

            workBar = Ui.Image(prompt, "Barra", new Color(1, 1, 1, .15f), t?.pill).rectTransform;
            workBar.Place(new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -8), new Vector2(700, 12));
            workFill = Ui.Image(workBar, "Relleno", UiTheme.Amber, t?.pill);
            workFill.rectTransform.anchorMin = Vector2.zero; workFill.rectTransform.anchorMax = new Vector2(0, 1); workFill.rectTransform.pivot = new Vector2(0, .5f);
            workFill.rectTransform.offsetMin = workFill.rectTransform.offsetMax = Vector2.zero;
            workBar.gameObject.SetActive(false);

            help = Ui.Panel(rt, "Ayuda", UiTheme.PanelSolid).rectTransform;
            help.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(900, 470));
            helpTitle = Ui.Text(help, "Titulo", "", 40, t?.title, UiTheme.Cream, TextAlignmentOptions.Top); helpTitle.rectTransform.Stretch(40, 40, 30, 380);
            helpText = Ui.Text(help, "Texto", "", 28, t?.body, UiTheme.Cream); helpText.rectTransform.Stretch(60, 50, 110, 30);
            group.alpha = 0;
        }

        public void Bind(IZoneHud zone) { model = zone; lastDone = lastTotal = -1; }

        public void Tick(bool show)
        {
            if (model == null || (model as UnityEngine.Object) == null) { model = null; group.alpha = 0; return; }
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1 : 0, Time.unscaledDeltaTime * 4f);
            zoneLabel.text = "ZONA " + model.ZoneNumber + " · " + model.ZoneTitle.ToUpperInvariant();
            objective.text = model.Objective;
            if (model.ProgressDone != lastDone || model.ProgressTotal != lastTotal)
            {
                lastDone = model.ProgressDone; lastTotal = model.ProgressTotal;
                // Con totales grandes (zona 4) solo se muestra la cuenta: los puntos no caben.
                int shownPips = lastTotal <= 12 ? lastTotal : 0;
                while (pipImages.Count < shownPips)
                {
                    var p = Ui.Image(pips, "Pip", Color.white, Ui.Theme?.pill); p.rectTransform.sizeDelta = new Vector2(26, 26);
                    pipImages.Add(p);
                }
                for (int i = 0; i < pipImages.Count; i++)
                {
                    pipImages[i].gameObject.SetActive(i < shownPips);
                    pipImages[i].color = i < lastDone ? UiTheme.Moss : new Color(1f, 1f, 1f, .18f);
                }
                progress.text = lastDone + " / " + lastTotal + "  " + model.ProgressLabel;
                progress.rectTransform.anchoredPosition = new Vector2(30 + shownPips * 34 + (shownPips > 0 ? 14 : 0), 24);
            }
            bool hasPrompt = !string.IsNullOrEmpty(model.PromptKey);
            prompt.gameObject.SetActive(hasPrompt);
            if (hasPrompt) { promptKey.text = model.PromptKey; promptText.text = model.PromptText; }
            bool hasHint = !string.IsNullOrEmpty(model.HintText);
            hint.gameObject.SetActive(hasHint);
            if (hasHint) { hintTitle.text = model.HintTitle; hintText.text = model.HintText; hint.anchoredPosition = new Vector2(0, hasPrompt ? 254 : 150); }
            tip.text = model.Tip;
            var extra = model as IZoneHudExtra;
            side.gameObject.SetActive(extra != null);
            if (extra != null)
            {
                sideTitle.text = extra.SideTitle; sideText.text = extra.SideText;
                float w = extra.WorkProgress;
                workBar.gameObject.SetActive(hasPrompt && w > 0f);
                if (w > 0f) workFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(w), 1);
            }
            else workBar.gameObject.SetActive(false);
            help.gameObject.SetActive(model.HelpVisible);
            if (model.HelpVisible) { helpTitle.text = model.HelpTitle; helpText.text = model.HelpText; }
        }
    }

    /// <summary>Página del cuaderno de la cuidadora que aparece al recuperar una zona.</summary>
    public sealed class NotebookView
    {
        private readonly RectTransform root;
        private readonly TextMeshProUGUI zone, heading, body, lesson, reward;
        private readonly Button button;
        private Action onContinue;
        public bool Visible => root.gameObject.activeSelf;
        public float ShownAt { get; private set; }
        public GameFlow.Note Current { get; private set; }

        public NotebookView(Transform parent)
        {
            var t = Ui.Theme;
            root = Ui.Rect("Cuaderno", parent).Stretch();
            Ui.Image(root, "Velo", new Color(0, 0, 0, .55f), null, true).rectTransform.Stretch();
            var page = Ui.Image(root, "Pagina", UiTheme.PaperTint, t?.paper, true).rectTransform;
            page.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 10), new Vector2(1040, 760));
            page.localRotation = Quaternion.Euler(0, 0, -.8f);
            zone = Ui.Text(page, "Zona", "", 24, t?.bold, new Color(.42f, .35f, .25f), TextAlignmentOptions.Top); zone.characterSpacing = 5;
            zone.rectTransform.Stretch(80, 80, 56, 650);
            heading = Ui.Text(page, "Encabezado", "", 58, t?.handwriting, UiTheme.Ink, TextAlignmentOptions.Top);
            heading.rectTransform.Stretch(80, 80, 100, 560);
            body = Ui.Text(page, "Texto", "", 36, t?.handwriting, UiTheme.Ink); body.lineSpacing = 6;
            body.rectTransform.Stretch(110, 110, 210, 300);
            var sign = Ui.Text(page, "Firma", "— C.", 34, t?.handwriting, new Color(.35f, .27f, .18f), TextAlignmentOptions.TopRight);
            sign.rectTransform.Stretch(110, 120, 445, 260);
            lesson = Ui.Text(page, "Aprendizaje", "", 27, t?.bold, new Color(.25f, .38f, .18f), TextAlignmentOptions.Top);
            lesson.rectTransform.Stretch(90, 90, 520, 180);
            reward = Ui.Text(page, "Recompensa", "", 26, t?.body, new Color(.38f, .26f, .12f), TextAlignmentOptions.Top);
            reward.rectTransform.Stretch(90, 90, 575, 110);
            button = Ui.Button(page, "Continuar  [E]", () => Continue(), 360, 62, 28);
            ((RectTransform)button.transform).Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(360, 62));
            root.gameObject.SetActive(false);
        }

        public void Show(GameFlow.Note note, Action next, string buttonLabel = "Continuar  [E]")
        {
            Current = note; onContinue = next;
            zone.text = note.zone.ToUpperInvariant(); heading.text = note.heading; body.text = note.body;
            lesson.text = note.lesson; reward.text = note.reward;
            button.GetComponentInChildren<TextMeshProUGUI>().text = buttonLabel;
            root.gameObject.SetActive(true); ShownAt = Time.unscaledTime;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            button.Select();
        }

        public void Hide() { root.gameObject.SetActive(false); onContinue = null; }

        public void Continue()
        {
            if (!Visible) return;
            var next = onContinue; Hide(); next?.Invoke();
        }
    }

    /// <summary>Pantalla final (las tres zonas recuperadas).</summary>
    public sealed class EndingView
    {
        private readonly RectTransform root, card;
        private GameObject credits;
        public bool Visible => root.gameObject.activeSelf;
        public bool CreditsOpen => credits != null;
        public const string Message = "Agua, plantas nativas y refugio están conectados: juntos permiten que la fauna vuelva. " +
            "Aquí la recuperación fue instantánea; en la realidad toma años, seguimiento y el conocimiento de las comunidades locales. " +
            "Cuidar y mantener el bosque importa tanto como repararlo.";

        private TextMeshProUGUI extra;
        /// <summary>Línea adicional (tiempo y medalla del valle).</summary>
        public void SetExtra(string text) { if (extra != null) extra.text = text ?? ""; }

        public EndingView(Transform parent)
        {
            var t = Ui.Theme;
            root = Ui.Rect("Final", parent).Stretch();
            Ui.Image(root, "Velo", new Color(.02f, .05f, .04f, .7f), null, true).rectTransform.Stretch();
            card = Ui.Panel(root, "Tarjeta", UiTheme.PanelSolid).rectTransform;
            card.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1320, 900));
            var title = Ui.Text(card, "Titulo", "El bosque regresa", 84, t?.title, UiTheme.Cream, TextAlignmentOptions.Top); title.rectTransform.Stretch(40, 40, 40, 760);
            var sub = Ui.Text(card, "Subtitulo", "LAS CUATRO ZONAS ESTÁN RECUPERADAS  ·  ODS 15 · VIDA DE ECOSISTEMAS TERRESTRES", 23, t?.bold, UiTheme.Moss, TextAlignmentOptions.Top);
            sub.characterSpacing = 4; sub.rectTransform.Stretch(40, 40, 150, 700);
            (string z, string what)[] rows =
            {
                ("El arroyo dormido", "el agua vuelve a correr cuando se retiran los obstáculos del cauce."),
                ("El claro vacío", "las plantas nativas prosperan en el suelo húmedo adecuado."),
                ("El refugio perdido", "troncos, piedras y matorrales dan cobijo en el paso de la fauna."),
                ("El valle olvidado", "sin residuos ni troncos en el paso, el agua y la vida vuelven al valle."),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var ic = Ui.Icon(card, "Icono", t?.restorationIcon, UiTheme.Moss);
                ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -216 - i * 66), new Vector2(44, 44));
                var tx = Ui.Text(card, "Fila", "<b>" + rows[i].z + "</b> — " + rows[i].what, 30, t?.body, UiTheme.Cream, TextAlignmentOptions.MidlineLeft);
                tx.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -212 - i * 66), new Vector2(1020, 56));
            }
            var msg = Ui.Text(card, "Mensaje", Message, 28, t?.italic, UiTheme.CreamDim, TextAlignmentOptions.Top);
            msg.rectTransform.Stretch(120, 120, 500, 190);
            extra = Ui.Text(card, "Extra", "", 28, t?.bold, UiTheme.Amber, TextAlignmentOptions.Top);
            extra.rectTransform.Stretch(120, 120, 640, 140);
            var buttons = Ui.Rect("Botones", card); buttons.Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 50), new Vector2(1200, 70));
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 24; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = false;
            Ui.Button(buttons, "Créditos", OpenCredits, 300, 66, 28);
            Ui.Button(buttons, "Jugar de nuevo", GameFlow.NewGame, 340, 66, 28);
            Ui.Button(buttons, "Menú principal", GameFlow.BackToMenu, 340, 66, 28);
            root.gameObject.SetActive(false);
        }

        public void Show()
        {
            root.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            root.GetComponentInChildren<Button>().Select();
        }

        public void Hide() { CloseCredits(); root.gameObject.SetActive(false); }

        public void OpenCredits()
        {
            if (credits != null) return;
            card.gameObject.SetActive(false);
            credits = MenuPanels.Credits(root, CloseCredits);
        }

        public void CloseCredits()
        {
            if (credits == null) return;
            UnityEngine.Object.Destroy(credits); credits = null; card.gameObject.SetActive(true);
        }
    }

    /// <summary>Menú de pausa: continuar, reiniciar zona, opciones, controles, menú principal y salir.</summary>
    public sealed class PauseView
    {
        private readonly RectTransform root, main;
        private GameObject sub;
        public bool Visible => root.gameObject.activeSelf;
        public bool SubPanelOpen => sub != null;

        public PauseView(Transform parent, Action resume, Action restart)
        {
            var t = Ui.Theme;
            root = Ui.Rect("Pausa", parent).Stretch();
            Ui.Image(root, "Velo", new Color(0, 0, 0, .6f), null, true).rectTransform.Stretch();
            main = Ui.Panel(root, "Tarjeta", UiTheme.PanelSolid).rectTransform;
            main.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 720));
            var title = Ui.Text(main, "Titulo", "Pausa", 64, t?.title, UiTheme.Cream, TextAlignmentOptions.Top); title.rectTransform.Stretch(20, 20, 40, 600);
            var list = Ui.Rect("Botones", main).Stretch(60, 60, 150, 50);
            Ui.Column(list, 16, TextAnchor.UpperCenter);
            Ui.Button(list, "Continuar", resume, 440);
            Ui.Button(list, "Reiniciar zona", restart, 440);
            Ui.Button(list, "Opciones", () => Open(MenuPanels.Options(root, Close)), 440);
            Ui.Button(list, "Controles", () => Open(MenuPanels.Controls(root, Close)), 440);
            Ui.Button(list, "Menú principal", GameFlow.BackToMenu, 440);
            Ui.Button(list, "Salir del juego", GameFlow.Quit, 440);
            root.gameObject.SetActive(false);
        }

        private void Open(GameObject panel) { Close(); sub = panel; main.gameObject.SetActive(false); }

        public void Close()
        {
            if (sub != null) { UnityEngine.Object.Destroy(sub); sub = null; }
            main.gameObject.SetActive(true);
            if (Visible) main.GetComponentInChildren<Button>().Select();
        }

        public void Show() { root.gameObject.SetActive(true); Close(); }
        public void Hide() { if (sub != null) { UnityEngine.Object.Destroy(sub); sub = null; } main.gameObject.SetActive(true); root.gameObject.SetActive(false); }
    }

    /// <summary>Rótulo de entrada a cada zona ("ZONA 2 · El claro vacío").</summary>
    public sealed class TitleCardView
    {
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI number, title, subtitle;
        private float start = -100f;
        public const float Duration = 4.2f;
        public bool Visible => group.alpha > .01f;

        public TitleCardView(Transform parent)
        {
            var t = Ui.Theme;
            var rt = Ui.Rect("Rotulo", parent).Stretch();
            group = rt.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.alpha = 0;
            var band = Ui.Image(rt, "Banda", new Color(0, 0, 0, .45f), t?.glow); band.rectTransform.Place(new Vector2(.5f, .62f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1500, 300));
            number = Ui.Text(rt, "Numero", "", 30, t?.bold, UiTheme.Moss, TextAlignmentOptions.Center); number.characterSpacing = 12;
            number.rectTransform.Place(new Vector2(.5f, .62f), new Vector2(.5f, .5f), new Vector2(0, 86), new Vector2(1200, 50));
            title = Ui.Text(rt, "Titulo", "", 96, t?.title, UiTheme.Cream, TextAlignmentOptions.Center);
            title.rectTransform.Place(new Vector2(.5f, .62f), new Vector2(.5f, .5f), new Vector2(0, 6), new Vector2(1400, 120));
            subtitle = Ui.Text(rt, "Subtitulo", "", 34, t?.italic, UiTheme.CreamDim, TextAlignmentOptions.Center);
            subtitle.rectTransform.Place(new Vector2(.5f, .62f), new Vector2(.5f, .5f), new Vector2(0, -78), new Vector2(1300, 50));
        }

        public void Show(GameFlow.ZoneInfo zone)
        {
            number.text = "ZONA " + zone.number; title.text = zone.title; subtitle.text = zone.subtitle;
            start = Time.unscaledTime;
        }

        public void Hide() { start = -100f; group.alpha = 0; }

        public void Tick()
        {
            float k = Time.unscaledTime - start;
            group.alpha = k < 0 || k > Duration ? 0 : Mathf.Min(Mathf.Clamp01(k / .9f), Mathf.Clamp01((Duration - k) / 1.1f));
        }
    }
}

namespace Bosque.UI
{
    /// <summary>
    /// Mapa de la zona (M), como una hoja de papel topográfico sobre la mesa (referencias: Firewatch, Red Dead
    /// Redemption 2): cartela con el título, rosa de los vientos, escala gráfica, nombres en tipografía de
    /// cartografía (ríos en cursiva azul siguiendo el cauce, regiones en versalitas), iconos de cartografía y, al lado,
    /// un panel con el avance de cada sector y la leyenda.
    /// </summary>
    public sealed class MapView
    {
        private const float Size = 900f, PanelWidth = 500f, Gap = 30f;
        private readonly RectTransform root, frame, playerMark, pulse;
        private readonly RawImage image;
        private readonly RectTransform markersRoot;
        private readonly TextMeshProUGUI title, subtitle, panelTitle, panelSubtitle, scaleMid, scaleEnd;
        private readonly RectTransform scaleBar;
        private readonly List<(RectTransform root, Image icon, TextMeshProUGUI label)> marks = new List<(RectTransform, Image, TextMeshProUGUI)>();
        private readonly List<(GameObject root, Image swatch, TextMeshProUGUI name, TextMeshProUGUI count, Image fill, TextMeshProUGUI note)> sectorRows =
            new List<(GameObject, Image, TextMeshProUGUI, TextMeshProUGUI, Image, TextMeshProUGUI)>();
        private readonly RectTransform sectorList;
        private Rect lastWorld;
        public bool Visible => root.gameObject.activeSelf;
        public int MarkersShown { get; private set; }

        public MapView(Transform parent)
        {
            var t = Ui.Theme;
            root = Ui.Rect("Mapa", parent).Stretch();
            Ui.Image(root, "Velo", new Color(.02f, .03f, .02f, .8f), null, false).rectTransform.Stretch();
            float total = Size + Gap + PanelWidth;
            var sheetPos = new Vector2(-total * .5f + Size * .5f, -6f);

            // Hoja de papel con sombra.
            var shadow = Ui.Image(root, "Sombra", new Color(0, 0, 0, .55f), t?.panel).rectTransform;
            shadow.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), sheetPos + new Vector2(10, -14), new Vector2(Size + 26, Size + 26));
            frame = Ui.Rect("Hoja", root); frame.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), sheetPos, new Vector2(Size, Size));
            image = frame.gameObject.AddComponent<RawImage>(); image.raycastTarget = false;
            markersRoot = Ui.Rect("Marcadores", frame).Stretch();

            // Cartela del título (arriba a la izquierda, sobre el papel).
            var cartouche = Ui.Image(frame, "Cartela", new Color(.95f, .91f, .81f, .82f), t?.panel).rectTransform;
            cartouche.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -44), new Vector2(430, 150));
            Ui.Image(cartouche, "Filete", new Color(MapIcons.Ink.r, MapIcons.Ink.g, MapIcons.Ink.b, .55f)).rectTransform
                .Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -66), new Vector2(394, 1.5f));
            title = MapText(cartouche, "Titulo", "", 33, t?.mapCapsBold, MapIcons.Ink, TextAlignmentOptions.TopLeft);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -14), new Vector2(400, 44));
            title.characterSpacing = 5f;
            subtitle = MapText(cartouche, "Subtitulo", "", 16, t?.mapSerifItalic, new Color(.33f, .25f, .17f), TextAlignmentOptions.TopLeft);
            subtitle.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(19, -72), new Vector2(400, 24));

            // Rosa de los vientos (arriba a la derecha).
            var rose = Ui.Image(frame, "Rosa", Color.white, MapIcons.Rose).rectTransform;
            rose.Place(new Vector2(1, 0), new Vector2(.5f, .5f), new Vector2(-104, 104), new Vector2(140, 140));
            var n = MapText(rose, "N", "N", 28, t?.mapCapsBold, new Color(.5f, .14f, .08f), TextAlignmentOptions.Center);
            n.rectTransform.Place(new Vector2(.5f, 1), new Vector2(.5f, 0), new Vector2(0, 0), new Vector2(60, 34));

            // Escala gráfica (abajo a la izquierda): 0-50 m en cinco tramos alternos.
            scaleBar = Ui.Rect("Escala", cartouche); scaleBar.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -104), new Vector2(200, 8));
            Ui.Image(scaleBar, "Borde", MapIcons.Ink).rectTransform.Stretch(-2, -2, -2, -2);
            for (int i = 0; i < 5; i++)
            {
                var seg = Ui.Image(scaleBar, "Tramo", i % 2 == 0 ? MapIcons.Ink : MapIcons.Paper).rectTransform;
                seg.anchorMin = new Vector2(i / 5f, 0); seg.anchorMax = new Vector2((i + 1) / 5f, 1); seg.offsetMin = seg.offsetMax = Vector2.zero;
            }
            var zero = MapText(scaleBar, "0", "0", 17, t?.mapSerif, MapIcons.Ink, TextAlignmentOptions.Bottom);
            zero.rectTransform.Place(new Vector2(0, 0), new Vector2(.5f, 1), new Vector2(0, -3), new Vector2(40, 22));
            scaleMid = MapText(scaleBar, "Medio", "25", 17, t?.mapSerif, MapIcons.Ink, TextAlignmentOptions.Bottom);
            scaleMid.rectTransform.Place(new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -3), new Vector2(40, 22));
            scaleEnd = MapText(scaleBar, "Fin", "50 m", 17, t?.mapSerif, MapIcons.Ink, TextAlignmentOptions.Bottom);
            scaleEnd.rectTransform.Place(new Vector2(1, 0), new Vector2(.5f, 1), new Vector2(8, -3), new Vector2(60, 22));
            var grid = MapText(scaleBar, "Cuadricula", "cuadrícula 25 m\ncurvas cada 1 m", 14, t?.mapSerifItalic, new Color(.33f, .25f, .17f), TextAlignmentOptions.Left);
            grid.rectTransform.Place(new Vector2(1, .5f), new Vector2(0, .5f), new Vector2(40, -6), new Vector2(160, 40));
            grid.lineSpacing = -12f;

            // La Guardiana: flecha con un pulso.
            pulse = Ui.Image(frame, "Pulso", new Color(1f, .78f, .35f, .6f), MapIcons.Ring).rectTransform; pulse.sizeDelta = new Vector2(40, 40);
            playerMark = Ui.Image(frame, "Guardiana", Color.white, MapIcons.Player).rectTransform; playerMark.sizeDelta = new Vector2(34, 34);

            // Panel lateral: sectores y leyenda.
            var panel = Ui.Panel(root, "Panel", UiTheme.PanelSolid).rectTransform;
            panel.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(total * .5f - PanelWidth * .5f, -6f), new Vector2(PanelWidth, Size));
            var kicker = Ui.Text(panel, "Seccion", "MAPA", 19, t?.bold, UiTheme.Moss); kicker.characterSpacing = 18f;
            kicker.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -34), new Vector2(PanelWidth - 76, 26));
            panelTitle = Ui.Text(panel, "Zona", "", 38, t?.title, UiTheme.Cream);
            panelTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -62), new Vector2(PanelWidth - 76, 48));
            panelSubtitle = Ui.Text(panel, "Detalle", "", 18, t?.italic, UiTheme.CreamDim);
            panelSubtitle.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -112), new Vector2(PanelWidth - 76, 50));
            Divider(panel, -170);
            Header(panel, "Sectores", -188);
            sectorList = Ui.Rect("Sectores", panel); sectorList.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -224), new Vector2(PanelWidth - 76, 340));
            Divider(panel, -560);
            Header(panel, "Leyenda", -578);
            var legend = new (Sprite icon, string text, Color tint, float size)[]
            {
                (MapIcons.Player, "Tú", Color.white, 30), (MapIcons.Station, "Punto de reciclaje", Color.white, 30),
                (MapIcons.Litter, "Residuo ubicado", Color.white, 24), (MapIcons.Wood, "Tronco caído ubicado", Color.white, 24),
                (MapIcons.Bridge, "Puente", Color.white, 34), (MapIcons.Ford, "Vado (agua baja)", Color.white, 34),
            };
            for (int i = 0; i < legend.Length; i++)
            {
                float x = 38 + (i % 2) * 214, y = -620 - (i / 2) * 46;
                var ic = Ui.Image(panel, "Icono", legend[i].tint, legend[i].icon).rectTransform;
                ic.Place(new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(x + 16, y), new Vector2(legend[i].size, legend[i].size));
                var lt = Ui.Text(panel, "Texto", legend[i].text, 19, t?.body, UiTheme.Cream, TextAlignmentOptions.Left);
                lt.rectTransform.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(x + 40, y), new Vector2(176, 40));
                lt.textWrappingMode = TextWrappingModes.Normal; lt.lineSpacing = -14;
            }
            var hint = Ui.Text(panel, "Ayuda", "Lo que ves de cerca o marcas con el rastreo (T) queda ubicado en el mapa.", 17, t?.italic, UiTheme.CreamDim);
            hint.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(38, 70), new Vector2(PanelWidth - 76, 48));
            var close = Ui.Text(panel, "Cerrar", "<b>M</b> o <b>Esc</b>  cerrar el mapa", 19, t?.body, UiTheme.Moss);
            close.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(38, 30), new Vector2(PanelWidth - 76, 30));
            root.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI MapText(Transform parent, string name, string text, float size, TMP_FontAsset font, Color color, TextAlignmentOptions align)
        {
            var tx = Ui.Text(parent, name, text, size, font, color, align);
            tx.textWrappingMode = TextWrappingModes.NoWrap;
            Halo(tx);
            return tx;
        }

        /// <summary>Halo de papel: los nombres se leen sobre curvas, copas y agua (como en los mapas impresos).</summary>
        private static void Halo(TextMeshProUGUI tx)
        {
            var m = tx.fontMaterial;
            m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(.95f, .91f, .81f, .9f));
            m.SetFloat(ShaderUtilities.ID_UnderlayDilate, .55f);
            m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .45f);
        }

        private static void SetFont(TextMeshProUGUI l, TMP_FontAsset f)
        {
            if (f == null) f = Ui.Theme?.body;
            if (f == null || l.font == f) return;
            l.font = f; Halo(l);
        }

        private static void Divider(RectTransform panel, float y) =>
            Ui.Image(panel, "Linea", new Color(1, 1, 1, .12f)).rectTransform.Place(new Vector2(.5f, 1), new Vector2(.5f, .5f), new Vector2(0, y), new Vector2(PanelWidth - 76, 2));

        private static void Header(RectTransform panel, string text, float y)
        {
            var h = Ui.Text(panel, "Titulo_" + text, text.ToUpperInvariant(), 17, Ui.Theme?.bold, UiTheme.CreamDim); h.characterSpacing = 14f;
            h.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, y), new Vector2(PanelWidth - 76, 24));
        }

        public void Show() => root.gameObject.SetActive(true);
        public void Hide() => root.gameObject.SetActive(false);

        private Vector2 ToMap(Rect world, Vector3 p) =>
            new Vector2((p.x - world.x) / world.width * Size - Size * .5f, (p.z - world.y) / world.height * Size - Size * .5f);

        public void Tick(IZoneMap map)
        {
            if (!Visible || map == null) return;
            image.texture = map.MapTexture;
            var w = map.MapWorldRect;
            if (w != lastWorld)
            {
                // Escala gráfica de 50 m (o 20 m si la zona es pequeña) al tamaño real de la hoja.
                lastWorld = w;
                float len = w.width > 120 ? 50f : 20f;
                scaleBar.sizeDelta = new Vector2(len / w.width * Size, 10);
                scaleMid.text = (len / 2).ToString("0"); scaleEnd.text = len.ToString("0") + " m";
            }
            if (map.MapPlayer != null)
            {
                var pp = ToMap(w, map.MapPlayer.position);
                playerMark.anchoredPosition = pp; pulse.anchoredPosition = pp;
                playerMark.localRotation = Quaternion.Euler(0, 0, -map.MapPlayer.eulerAngles.y);
                float k = Mathf.Repeat(Time.unscaledTime / 1.4f, 1f);
                pulse.sizeDelta = Vector2.one * Mathf.Lerp(30f, 86f, k);
                pulse.GetComponent<Image>().color = new Color(1f, .78f, .35f, .7f * (1 - k));
            }
            TickInfo(map as IZoneMapInfo);
            var t = Ui.Theme;
            int i = 0;
            foreach (var m in map.MapMarkers)
            {
                if (i >= marks.Count)
                {
                    var r = Ui.Rect("Marca", markersRoot); r.sizeDelta = Vector2.zero;
                    var icon = Ui.Image(r, "Icono", Color.white, null);
                    var label = MapText(r, "Texto", "", 20, t?.mapSerif, MapIcons.Ink, TextAlignmentOptions.Center);
                    label.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(320, 40));
                    marks.Add((r, icon, label));
                }
                var (mr, ic, l) = marks[i++];
                mr.gameObject.SetActive(true);
                mr.anchoredPosition = ToMap(w, m.position);
                mr.localRotation = Quaternion.identity;
                l.text = m.label ?? ""; l.characterSpacing = 0; l.rectTransform.anchoredPosition = Vector2.zero; l.rectTransform.localRotation = Quaternion.identity;
                ic.rectTransform.localRotation = Quaternion.identity; ic.color = Color.white;
                switch (m.kind)
                {
                    case MarkerKind.Region:
                        ic.enabled = false;
                        SetFont(l, t?.mapCaps); l.fontSize = 27; l.characterSpacing = 16; l.fontStyle = FontStyles.Normal;
                        var rc = m.color.a > 0 ? m.color : MapIcons.Ink; rc.a = .78f; l.color = rc;
                        break;
                    case MarkerKind.Label:
                        ic.enabled = false;
                        SetFont(l, t?.mapSerifItalic); l.fontSize = m.big ? 19 : 22; l.color = MapIcons.WaterInk; l.characterSpacing = m.big ? 2 : 6;
                        l.rectTransform.localRotation = Quaternion.Euler(0, 0, m.angle);
                        break;
                    case MarkerKind.Station:
                        ic.enabled = true; ic.sprite = MapIcons.Station; ic.rectTransform.sizeDelta = new Vector2(34, 34);
                        SetFont(l, t?.mapCapsBold); l.fontSize = 18; l.color = MapIcons.Ink; l.characterSpacing = 3;
                        l.text = Short(m.label); l.rectTransform.anchoredPosition = new Vector2(0, -30);
                        break;
                    case MarkerKind.Bridge: case MarkerKind.Ford:
                        ic.enabled = true; ic.sprite = MapIcons.For(m.kind); ic.rectTransform.sizeDelta = m.kind == MarkerKind.Bridge ? new Vector2(34, 34) : new Vector2(30, 30);
                        ic.rectTransform.localRotation = Quaternion.Euler(0, 0, m.angle); l.text = "";
                        break;
                    case MarkerKind.Litter: case MarkerKind.Wood:
                        ic.enabled = true; ic.sprite = MapIcons.For(m.kind); ic.rectTransform.sizeDelta = new Vector2(20, 20); l.text = "";
                        break;
                    default:
                        ic.enabled = true; ic.sprite = MapIcons.Dot; ic.color = m.color; ic.rectTransform.sizeDelta = m.big ? new Vector2(20, 20) : new Vector2(12, 12);
                        SetFont(l, t?.mapSerif); l.fontSize = 18; l.color = MapIcons.Ink; l.rectTransform.anchoredPosition = new Vector2(0, -20);
                        break;
                }
                // Orden de dibujo: nombres debajo, iconos encima.
                if (m.kind == MarkerKind.Region || m.kind == MarkerKind.Label) mr.SetAsFirstSibling(); else mr.SetAsLastSibling();
            }
            MarkersShown = i;
            for (; i < marks.Count; i++) marks[i].root.gameObject.SetActive(false);
            pulse.SetAsLastSibling(); playerMark.SetAsLastSibling();
        }

        private static string Short(string station) => string.IsNullOrEmpty(station) ? "" : station.Replace("Puesto de la ", "").Replace("Puesto del ", "");

        private void TickInfo(IZoneMapInfo info)
        {
            panelTitle.text = info?.MapTitle ?? "Mapa"; panelSubtitle.text = info?.MapSubtitle ?? "";
            title.text = (info?.MapTitle ?? "").ToUpperInvariant(); subtitle.text = info?.MapSubtitle ?? "";
            int i = 0;
            if (info != null)
                foreach (var s in info.MapSectors)
                {
                    if (i >= sectorRows.Count) sectorRows.Add(SectorRow(i));
                    var (go, sw, nm, ct, fill, note) = sectorRows[i++];
                    go.SetActive(true);
                    sw.color = s.color; nm.text = s.name;
                    bool clean = s.total > 0 && s.cleaned >= s.total;
                    ct.text = clean ? "<color=#B0D180>limpio</color>" : $"{s.cleaned}<color=#9A9A8C>/{s.total}</color>";
                    fill.rectTransform.anchorMax = new Vector2(s.total > 0 ? (float)s.cleaned / s.total : 0, 1);
                    fill.color = clean ? UiTheme.Moss : s.color;
                    note.text = clean ? "Sector recuperado" : s.hidden > 0 ? (s.hidden == 1 ? "1 por ubicar" : $"{s.hidden} por ubicar") : "Todo ubicado: falta recoger";
                }
            for (; i < sectorRows.Count; i++) sectorRows[i].root.SetActive(false);
        }

        private (GameObject, Image, TextMeshProUGUI, TextMeshProUGUI, Image, TextMeshProUGUI) SectorRow(int index)
        {
            var t = Ui.Theme;
            var row = Ui.Rect("Sector_" + index, sectorList); row.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -index * 82), new Vector2(PanelWidth - 76, 76));
            var sw = Ui.Image(row, "Color", Color.white, MapIcons.Dot); sw.rectTransform.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(0, -16), new Vector2(14, 14));
            var nm = Ui.Text(row, "Nombre", "", 24, t?.bold, UiTheme.Cream, TextAlignmentOptions.Left);
            nm.rectTransform.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(24, -16), new Vector2(260, 32));
            var ct = Ui.Text(row, "Cuenta", "", 22, t?.bold, UiTheme.Cream, TextAlignmentOptions.Right);
            ct.rectTransform.Place(new Vector2(1, 1), new Vector2(1, .5f), new Vector2(0, -16), new Vector2(140, 32));
            var bg = Ui.Image(row, "Barra", new Color(1, 1, 1, .1f), t?.pill).rectTransform; bg.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(24, -42), new Vector2(PanelWidth - 100, 8));
            var fill = Ui.Image(bg, "Relleno", Color.white, t?.pill); fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var note = Ui.Text(row, "Nota", "", 17, t?.italic, UiTheme.CreamDim, TextAlignmentOptions.Left);
            note.rectTransform.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(24, -62), new Vector2(PanelWidth - 100, 24));
            return (row.gameObject, sw, nm, ct, fill, note);
        }
    }
}
