using System.Collections.Generic;
using System.Linq;
using Bosque.Interaction;
using Bosque.World;
using Bosque.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bosque.UI
{
    /// <summary>
    /// Inventario (Tab o I), en todas las zonas: lo que llevas (bolsa del valle con sus casillas, o el elemento de
    /// refugio que cargas), las habilidades con su tecla, las notas del cuaderno ya reveladas y el recorrido por las
    /// cuatro zonas. Lee el estado del juego al abrirse y en cada fotograma mientras está abierto (no guarda copia).
    /// </summary>
    public sealed class InventoryView
    {
        private const float W = 1500, H = 820;
        private readonly RectTransform root, slotsRoot;
        private readonly TextMeshProUGUI zoneTitle, bagTitle, bagNote, abilitiesText, notesText, zonesText;
        private readonly List<(Image card, Image band, TextMeshProUGUI label)> slots = new List<(Image, Image, TextMeshProUGUI)>();
        public bool Visible => root.gameObject.activeSelf;
        public int SlotsShown { get; private set; }
        public string BagText => bagNote.text;
        public string AbilitiesText => abilitiesText.text;

        public InventoryView(Transform parent)
        {
            var t = Ui.Theme;
            root = Ui.Rect("Inventario", parent).Stretch();
            Ui.Image(root, "Velo", new Color(.01f, .02f, .015f, .78f), null, true).rectTransform.Stretch();
            var card = UiKit.GlassPanel(root, "Tarjeta", UiKit.GlassSolid).rectTransform;
            card.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(W, H));

            Ui.Image(card, "Rombo", UiTheme.Amber, UiKit.Diamond).rectTransform.Place(new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(64, -60), new Vector2(14, 14));
            var kicker = Ui.Text(card, "Seccion", "Inventario", 22, t?.mapCapsBold ?? t?.bold, UiTheme.Moss, TextAlignmentOptions.MidlineLeft); kicker.characterSpacing = 8;
            kicker.rectTransform.Place(new Vector2(0, 1), new Vector2(0, .5f), new Vector2(84, -60), new Vector2(600, 34));
            zoneTitle = Ui.Text(card, "Zona", "", 52, t?.mapCapsBold ?? t?.title, UiTheme.Cream, TextAlignmentOptions.TopLeft);
            zoneTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -82), new Vector2(1000, 70));
            Ui.Image(card, "Linea", new Color(1, 1, 1, .12f)).rectTransform.Place(new Vector2(.5f, 1), new Vector2(.5f, .5f), new Vector2(0, -168), new Vector2(W - 120, 1.5f));

            // Columna izquierda: lo que llevas.
            float colW = (W - 180) / 2f;
            var left = Ui.Rect("Mochila", card); left.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -196), new Vector2(colW, 560));
            bagTitle = Header(left, "Lo que llevas", 0);
            slotsRoot = Ui.Rect("Casillas", left); slotsRoot.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -44), new Vector2(colW, 300));
            var grid = slotsRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150, 132); grid.spacing = new Vector2(12, 12); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            bagNote = Ui.Text(left, "Nota", "", 22, t?.italic, UiTheme.CreamDim);
            bagNote.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -340), new Vector2(colW, 70));
            Header(left, "Habilidades", -420);
            abilitiesText = Ui.Text(left, "Habilidades", "", 24, t?.body, UiTheme.Cream); abilitiesText.richText = true; abilitiesText.lineSpacing = 10;
            abilitiesText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -460), new Vector2(colW, 120));

            // Columna derecha: recorrido y cuaderno.
            var right = Ui.Rect("Cuaderno", card); right.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -196), new Vector2(colW, 560));
            Header(right, "Recorrido", 0);
            zonesText = Ui.Text(right, "Zonas", "", 25, t?.body, UiTheme.Cream); zonesText.richText = true; zonesText.lineSpacing = 12;
            zonesText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -44), new Vector2(colW, 200));
            Header(right, "Cuaderno de la cuidadora", -250);
            notesText = Ui.Text(right, "Notas", "", 24, t?.handwriting ?? t?.body, new Color(.93f, .89f, .78f)); notesText.richText = true; notesText.lineSpacing = 8;
            notesText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -294), new Vector2(colW, 270));

            var keys = new KeyHintRow(card, "Teclas", TextAnchor.MiddleRight);
            keys.Rect.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 30), new Vector2(700, 40));
            keys.Set("Tab: cerrar · Esc: cerrar");
            root.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI Header(RectTransform parent, string text, float y)
        {
            var h = Ui.Text(parent, "Titulo_" + text, text.ToUpperInvariant(), 18, Ui.Theme?.bold, UiTheme.Moss); h.characterSpacing = 12;
            h.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(700, 28));
            return h;
        }

        public void Show() { root.gameObject.SetActive(true); Tick(); }
        public void Hide() => root.gameObject.SetActive(false);

        public void Tick()
        {
            if (!Visible) return;
            var zone = GameFlow.Zone(SceneManager.GetActiveScene().name);
            zoneTitle.text = zone != null ? zone.title : "Bosque de regreso";

            // Lo que llevas.
            var bag = Object.FindFirstObjectByType<LitterBag>();
            var carrier = Object.FindFirstObjectByType<ItemCarrier>();
            int shown = 0;
            if (bag != null)
            {
                foreach (var item in bag.Items)
                    for (int s = 0; s < Mathf.Max(1, item.Slots); s++)
                        Slot(shown++, s == 0 ? item.Label : "(" + item.Label + ")", WasteKinds.BinColor(item.Kind), s == 0 ? WasteKinds.Name(item.Kind) : "");
                for (; shown < bag.Capacity; ) Slot(shown++, "", new Color(1, 1, 1, .1f), "vacío", empty: true);
                bagTitle.text = $"LO QUE LLEVAS  ·  BOLSA {bag.Used}/{bag.Capacity}";
                bagNote.text = bag.IsEmpty ? "La bolsa está vacía. Recoge residuos con E; cada uno va a su contenedor." :
                    bag.Free == 0 ? "La bolsa está llena: llévala a un punto de reciclaje." :
                    "Separa en el punto de reciclaje: " + string.Join(" · ", WasteKinds.All.Where(k => bag.CountOf(k) > 0).Select(k => $"{WasteKinds.Name(k)} {bag.CountOf(k)}"));
            }
            else if (carrier != null)
            {
                bagTitle.text = "LO QUE LLEVAS";
                if (carrier.Holding != null) Slot(shown++, carrier.Holding.Label, new Color(.55f, .4f, .25f), "refugio");
                for (int i = 0; i < 3; i++) Slot(shown++, "", new Color(1, 1, 1, .1f), "vacío", empty: true);
                bagNote.text = carrier.Holding != null ? "Llévalo a un lugar marcado para el refugio y pulsa E." : "Las manos libres. Recoge troncos, piedras o ramas con E.";
            }
            else
            {
                bagTitle.text = "LO QUE LLEVAS";
                Slot(shown++, "Cuaderno", new Color(.75f, .65f, .45f), "de la cuidadora");
                bagNote.text = "Aquí solo necesitas observar y actuar con E.";
            }
            for (int i = shown; i < slots.Count; i++) slots[i].card.gameObject.SetActive(false);
            SlotsShown = shown;

            // Habilidades.
            bool growth = GameProgress.GrowthUnlocked || Object.FindFirstObjectByType<GrowthAbility>()?.Unlocked == true;
            bool valle = Object.FindFirstObjectByType<TrackingPulse>() != null;
            string Row(string key, string name, string what, bool on) =>
                on ? $"<color=#F7C76E><b>{key}</b></color>   <b>{name}</b>  <color=#BDBDAE>{what}</color>"
                   : $"<color=#6E6E66>{key}   {name}  (aún no)</color>";
            abilitiesText.text = string.Join("\n", new[]
            {
                Row("E", "Interactuar", "retirar, recoger, colocar", true),
                Row("Q", "Crecimiento", "plantas nativas en suelo húmedo", growth),
                Row("T", "Rastreo", "marca lo pendiente cercano", valle),
                Row("M", "Mapa", "del valle", valle),
            });

            // Recorrido y cuaderno.
            (string id, int n, string title)[] zs = { (GameProgress.Arroyo, 1, "El arroyo dormido"), (GameProgress.Claro, 2, "El claro vacío"), (GameProgress.Refugio, 3, "El refugio perdido"), (GameProgress.Valle, 4, "El valle olvidado") };
            zonesText.text = string.Join("\n", zs.Select(z =>
            {
                bool done = GameProgress.IsZoneComplete(z.id), here = zone != null && zone.number == z.n;
                string mark = done ? "<color=#B0D180>◆</color>" : here ? "<color=#F7C76E>◆</color>" : "<color=#55554E>◇</color>";
                string state = done ? "<color=#B0D180>recuperada</color>" : here ? "<color=#F7C76E>aquí</color>" : "<color=#77776E>por recorrer</color>";
                return $"{mark}  {z.n}. {z.title}   <size=80%>{state}</size>";
            }));
            var notes = zs.Where(z => GameProgress.IsZoneComplete(z.id)).Select(z => GameFlow.NoteFor(z.id)).Where(n => n != null).ToList();
            notesText.text = notes.Count == 0 ? "<i>Aún no hay notas: recupera una zona para leer la primera.</i>"
                : string.Join("\n", notes.Select(n => $"«{n.heading}»  <size=78%><color=#BDB59C>— {n.lesson}</color></size>"));
        }

        private void Slot(int i, string label, Color color, string kind, bool empty = false)
        {
            while (slots.Count <= i)
            {
                var c = UiKit.GlassPanel(slotsRoot, "Casilla", new Color(1, 1, 1, .05f), false);
                var band = Ui.Image(c.transform, "Color", Color.white, UiKit.Fill); band.type = Image.Type.Sliced;
                band.rectTransform.Place(new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -10), new Vector2(126, 34));
                var tx = Ui.Text(c.transform, "Texto", "", 19, Ui.Theme?.body, UiTheme.Cream, TextAlignmentOptions.Center); tx.richText = true;
                tx.rectTransform.Stretch(10, 10, 50, 8); tx.lineSpacing = -10;
                slots.Add((c, band, tx));
            }
            var (card, b, l) = slots[i];
            card.gameObject.SetActive(true);
            b.color = color;
            l.text = empty ? $"<color=#77776E>{kind}</color>" : $"{label}\n<size=80%><color=#BDBDAE>{kind}</color></size>";
        }
    }
}
