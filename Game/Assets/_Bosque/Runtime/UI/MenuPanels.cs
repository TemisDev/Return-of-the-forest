using System;
using Bosque.Audio;
using Bosque.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>Paneles compartidos entre el menú principal y la pausa: opciones, controles y créditos.</summary>
    public static class MenuPanels
    {
        private static RectTransform Card(Transform parent, string name, string heading, Vector2 size)
        {
            var card = Ui.Panel(parent, name, UiTheme.PanelSolid).rectTransform;
            card.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size);
            var h = Ui.Text(card, "Titulo", heading, 46, Ui.Theme?.mapCapsBold ?? Ui.Theme?.title, UiTheme.Cream, TextAlignmentOptions.Center); h.characterSpacing = 4;
            h.rectTransform.Place(new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -34), new Vector2(size.x - 80, 64));
            var line = Ui.Image(card, "Linea", UiTheme.Moss); line.rectTransform.Place(new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -104), new Vector2(120, 3));
            return card;
        }

        private static Button Back(RectTransform card, Action onBack)
        {
            var b = Ui.Button(card, "Volver", onBack, 300, 60, 28);
            ((RectTransform)b.transform).Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(300, 60));
            return b;
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100) + " %";

        public static GameObject Options(Transform parent, Action onBack)
        {
            var card = Card(parent, "PanelOpciones", "Opciones", new Vector2(880, 840));
            var list = Ui.Rect("Lista", card).Stretch(80, 80, 140, 120);
            Ui.Column(list, 10, TextAnchor.UpperCenter);
            var master = Ui.Slider(list, "Volumen general", 0, 1, BosqueAudio.GetVolume(AudioBus.Master), v => BosqueAudio.SetVolume(AudioBus.Master, v), Pct);
            var music = Ui.Slider(list, "Música", 0, 1, BosqueAudio.GetVolume(AudioBus.Music), v => BosqueAudio.SetVolume(AudioBus.Music, v), Pct);
            var amb = Ui.Slider(list, "Ambiente", 0, 1, BosqueAudio.GetVolume(AudioBus.Ambience), v => BosqueAudio.SetVolume(AudioBus.Ambience, v), Pct);
            var sfx = Ui.Slider(list, "Efectos", 0, 1, BosqueAudio.GetVolume(AudioBus.Sfx), v => BosqueAudio.SetVolume(AudioBus.Sfx, v), Pct);
            var sens = Ui.Slider(list, "Sensibilidad del ratón", .2f, 3f, CameraSettings.Sensitivity, v => CameraSettings.Sensitivity = v, v => v.ToString("0.0") + "×");
            var inv = Ui.Toggle(list, "Invertir eje vertical", CameraSettings.InvertY, v => CameraSettings.InvertY = v);
            var full = Ui.Toggle(list, "Pantalla completa", Screen.fullScreen, v =>
                Screen.fullScreenMode = v ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            var reset = Ui.Button(list, "Restablecer valores", () =>
            {
                BosqueAudio.ResetToDefaults(true); CameraSettings.ResetToDefaults();
                master.value = BosqueAudio.GetVolume(AudioBus.Master); music.value = BosqueAudio.GetVolume(AudioBus.Music);
                amb.value = BosqueAudio.GetVolume(AudioBus.Ambience); sfx.value = BosqueAudio.GetVolume(AudioBus.Sfx);
                sens.value = CameraSettings.Sensitivity; inv.isOn = CameraSettings.InvertY;
            }, 340, 52, 24);
            Back(card, onBack).Select();
            return card.gameObject;
        }

        public static GameObject Controls(Transform parent, Action onBack)
        {
            var card = Card(parent, "PanelControles", "Controles", new Vector2(880, 760));
            (string key, string what)[] rows =
            {
                ("W A S D", "Moverse"), ("Ratón", "Mirar alrededor"), ("Shift", "Caminar despacio"), ("Espacio", "Saltar"),
                ("E", "Interactuar: retirar, recoger, colocar"), ("Q", "Crecimiento (tras recuperar el arroyo)"),
                ("R", "Reiniciar la zona"), ("F1", "Ayuda de la zona"), ("Esc", "Pausa"),
            };
            var list = Ui.Rect("Lista", card).Stretch(90, 90, 140, 130);
            Ui.Column(list, 8, TextAnchor.UpperCenter);
            foreach (var (key, what) in rows)
            {
                var row = Ui.Rect("Fila_" + key, list); row.sizeDelta = new Vector2(700, 50);
                // V14: cada tecla dibujada como tecla (W A S D son cuatro).
                var keys = Ui.Rect("Teclas", row); keys.Place(new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(200, 44));
                var hk = keys.gameObject.AddComponent<HorizontalLayoutGroup>(); hk.spacing = 6; hk.childAlignment = TextAnchor.MiddleLeft;
                hk.childControlWidth = hk.childControlHeight = false; hk.childForceExpandWidth = false;
                foreach (var part in key.Split(' ')) UiKit.KeyCap(keys, part, 40);
                var w = Ui.Text(row, "Accion", what, 26, null, UiTheme.Cream, TextAlignmentOptions.MidlineLeft);
                w.rectTransform.Place(new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(220, 0), new Vector2(480, 50));
            }
            Back(card, onBack).Select();
            return card.gameObject;
        }

        public static GameObject Credits(Transform parent, Action onBack)
        {
            var card = Card(parent, "PanelCreditos", "Créditos", new Vector2(1100, 900));
            var view = Ui.Rect("Vista", card).Stretch(70, 70, 130, 120);
            view.gameObject.AddComponent<RectMask2D>();
            var content = Ui.Rect("Contenido", view);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            var text = Ui.Text(content, "Texto", Ui.Theme != null && Ui.Theme.credits != null ? Ui.Theme.credits.text : "Bosque de regreso", 24, null, UiTheme.Cream, TextAlignmentOptions.Top);
            text.rectTransform.Stretch(); text.richText = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content.gameObject.AddComponent<VerticalLayoutGroup>().childControlHeight = true;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 40; scroll.movementType = ScrollRect.MovementType.Clamped;
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // recibe la rueda del ratón
            Back(card, onBack).Select();
            return card.gameObject;
        }
    }
}
