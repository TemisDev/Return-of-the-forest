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
        private readonly RectTransform pips, prompt, hint, help;
        private readonly List<Image> pipImages = new List<Image>();
        private IZoneHud model;
        private int lastDone = -1, lastTotal = -1;

        public IZoneHud Model => model;
        public bool Visible => group.alpha > .5f;
        public string ObjectiveText => objective.text;
        public string PromptShown => prompt.gameObject.activeSelf ? promptText.text : null;
        public string HintShown => hint.gameObject.activeSelf ? hintText.text : null;

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
                while (pipImages.Count < lastTotal)
                {
                    var p = Ui.Image(pips, "Pip", Color.white, Ui.Theme?.pill); p.rectTransform.sizeDelta = new Vector2(26, 26);
                    pipImages.Add(p);
                }
                for (int i = 0; i < pipImages.Count; i++)
                {
                    pipImages[i].gameObject.SetActive(i < lastTotal);
                    pipImages[i].color = i < lastDone ? UiTheme.Moss : new Color(1f, 1f, 1f, .18f);
                }
                progress.text = lastDone + " / " + lastTotal + "  " + model.ProgressLabel;
                progress.rectTransform.anchoredPosition = new Vector2(30 + lastTotal * 34 + 14, 24);
            }
            bool hasPrompt = !string.IsNullOrEmpty(model.PromptKey);
            prompt.gameObject.SetActive(hasPrompt);
            if (hasPrompt) { promptKey.text = model.PromptKey; promptText.text = model.PromptText; }
            bool hasHint = !string.IsNullOrEmpty(model.HintText);
            hint.gameObject.SetActive(hasHint);
            if (hasHint) { hintTitle.text = model.HintTitle; hintText.text = model.HintText; hint.anchoredPosition = new Vector2(0, hasPrompt ? 254 : 150); }
            tip.text = model.Tip;
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

        public EndingView(Transform parent)
        {
            var t = Ui.Theme;
            root = Ui.Rect("Final", parent).Stretch();
            Ui.Image(root, "Velo", new Color(.02f, .05f, .04f, .7f), null, true).rectTransform.Stretch();
            card = Ui.Panel(root, "Tarjeta", UiTheme.PanelSolid).rectTransform;
            card.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1320, 900));
            var title = Ui.Text(card, "Titulo", "El bosque regresa", 84, t?.title, UiTheme.Cream, TextAlignmentOptions.Top); title.rectTransform.Stretch(40, 40, 40, 760);
            var sub = Ui.Text(card, "Subtitulo", "LAS TRES ZONAS ESTÁN RECUPERADAS  ·  ODS 15 · VIDA DE ECOSISTEMAS TERRESTRES", 23, t?.bold, UiTheme.Moss, TextAlignmentOptions.Top);
            sub.characterSpacing = 4; sub.rectTransform.Stretch(40, 40, 150, 700);
            (string z, string what)[] rows =
            {
                ("El arroyo dormido", "el agua vuelve a correr cuando se retiran los obstáculos del cauce."),
                ("El claro vacío", "las plantas nativas prosperan en el suelo húmedo adecuado."),
                ("El refugio perdido", "troncos, piedras y matorrales dan cobijo en el paso de la fauna."),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var ic = Ui.Icon(card, "Icono", t?.restorationIcon, UiTheme.Moss);
                ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -232 - i * 76), new Vector2(48, 48));
                var tx = Ui.Text(card, "Fila", "<b>" + rows[i].z + "</b> — " + rows[i].what, 30, t?.body, UiTheme.Cream, TextAlignmentOptions.MidlineLeft);
                tx.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -228 - i * 76), new Vector2(1020, 56));
            }
            var msg = Ui.Text(card, "Mensaje", Message, 30, t?.italic, UiTheme.CreamDim, TextAlignmentOptions.Top);
            msg.rectTransform.Stretch(120, 120, 480, 200);
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
