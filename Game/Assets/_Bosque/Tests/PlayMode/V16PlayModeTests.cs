using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Bosque.Audio;
using Bosque.UI;
using Bosque.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bosque.Tests.PlayMode
{
    /// <summary>
    /// V16: lo sólido no se atraviesa en ninguna zona, el inventario se abre en todas, y la música de lo degradado
    /// pasa al tema del bosque al restaurar.
    /// </summary>
    public sealed class V16PlayModeTests
    {
        private static readonly Regex SolidName = new Regex(@"^(SM_(BoulderDry|RockMossLarge|RockMoss|Stump|DeadLog|TreeBroadleaf_[AB])(_LOD0)?)(\s|$)");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            GameProgress.ResetAll(); GamePause.Set(false); FallenWood.AutoTick = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Presentation.Instance != null) Presentation.Instance.CloseInventory();
            GamePause.Set(false); GameProgress.ResetAll();
            yield return null;
        }

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null; yield return null;
        }

        /// <summary>Rocas, peñascos, tocones, troncos y árboles visibles tienen un colisionador que los cubre.</summary>
        [UnityTest]
        public IEnumerator SolidPropsAreSolidInEveryZone([Values(GameFlow.ArroyoScene, GameFlow.ClaroScene, GameFlow.RefugioScene, GameFlow.ValleScene)] string scene)
        {
            yield return Load(scene);
            Physics.SyncTransforms();
            int checkedCount = 0; var missing = new System.Collections.Generic.List<string>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!SolidName.IsMatch(r.name)) continue;
                var b = r.bounds;
                if (Mathf.Max(b.size.x, b.size.z) < .45f && b.size.y < .6f) continue;
                checkedCount++;
                bool own = r.GetComponentsInChildren<Collider>().Concat(r.GetComponentsInParent<Collider>()).Any(c => !c.isTrigger && !(c is CharacterController));
                bool trunk = r.name.Contains("Tree") && (r.transform.Find("ColisionTronco") != null || r.transform.parent != null && r.transform.parent.Find("ColisionTronco") != null);
                bool near = Physics.OverlapBox(b.center, b.extents * .4f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                    .Any(c => !(c is TerrainCollider) && !(c is CharacterController) && c.bounds.size.sqrMagnitude <= b.size.sqrMagnitude * 12f);
                if (!own && !trunk && !near) missing.Add($"{r.name} en {b.center:F1}");
            }
            Assert.That(missing, Is.Empty, $"{scene}: se atraviesan {missing.Count} de {checkedCount}: {string.Join(", ", missing.Take(12))}");
            if (scene != GameFlow.ValleScene) Assert.That(checkedCount, Is.GreaterThan(20), "Hay rocas y árboles que comprobar");
        }

        /// <summary>Los árboles del valle (Terrain) tienen la cápsula sobre el tronco: caminar contra uno detiene a la Guardiana.</summary>
        [UnityTest]
        public IEnumerator ValleyTreesBlockAtTheTrunk()
        {
            GameProgress.CompleteZone(GameProgress.Arroyo);
            yield return Load(GameFlow.ValleScene);
            var terrain = Object.FindFirstObjectByType<Terrain>();
            var td = terrain.terrainData;
            Assert.That(terrain.GetComponent<TerrainCollider>().enabled);
            int hits = 0, tested = 0;
            foreach (var t in td.treeInstances.Take(400))
            {
                var proto = td.treePrototypes[t.prototypeIndex].prefab;
                var cap = proto.GetComponent<CapsuleCollider>(); if (cap == null) continue;
                var basePos = Vector3.Scale(t.position, td.size) + terrain.transform.position;
                var center = basePos + Quaternion.Euler(0, t.rotation * Mathf.Rad2Deg, 0) * new Vector3(cap.center.x * t.widthScale, 0, cap.center.z * t.widthScale);
                var from = center + new Vector3(3f, 1.1f, 0);
                // Los árboles del Terrain colisionan a través del TerrainCollider: vale el impacto a la altura del tronco,
                // a menos de un radio y medio de su eje (no el suelo).
                if (Physics.Raycast(from, Vector3.left, out var hit, 3.2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    float ground = terrain.SampleHeight(hit.point) + terrain.transform.position.y;
                    var flat = hit.point - center; flat.y = 0;
                    if (hit.point.y > ground + .4f && flat.magnitude < cap.radius * t.widthScale * 1.6f + .15f) hits++;
                }
                if (++tested >= 40) break;
            }
            Assert.That(tested, Is.GreaterThan(20));
            Assert.That(hits, Is.GreaterThanOrEqualTo(tested * .9f), $"Troncos con colisión: {hits}/{tested}");
        }

        [UnityTest]
        public IEnumerator InventoryOpensInEveryZoneAndShowsWhatYouCarry()
        {
            foreach (var scene in new[] { GameFlow.ArroyoScene, GameFlow.RefugioScene, GameFlow.ValleScene })
            {
                yield return Load(scene);
                var P = Presentation.Instance;
                for (int i = 0; i < 4 && !P.InZone; i++) yield return null;
                P.OpenInventory(); yield return null;
                Assert.That(P.Inventory.Visible, scene + ": se abre");
                Assert.That(GamePause.IsPaused, scene + ": pausa el juego");
                Assert.That(P.Inventory.AbilitiesText, Does.Contain("Interactuar"));
                Assert.That(P.Inventory.SlotsShown, Is.GreaterThanOrEqualTo(1));
                if (scene == GameFlow.ValleScene)
                {
                    var bag = Object.FindFirstObjectByType<LitterBag>();
                    Assert.That(P.Inventory.SlotsShown, Is.EqualTo(bag.Capacity), "Valle: una casilla por espacio de la bolsa");
                    P.CloseInventory(); yield return null;
                    var item = Object.FindObjectsByType<LitterItem>(FindObjectsSortMode.None).First(x => x.Slots == 1);
                    bag.Add(item); item.gameObject.SetActive(false);
                    P.OpenInventory(); yield return null;
                    Assert.That(P.Inventory.BagText, Does.Contain(WasteKinds.Name(item.Kind)), "Muestra lo recogido por tipo");
                    Assert.That(P.Inventory.AbilitiesText, Does.Contain("Rastreo"));
                }
                P.CloseInventory(); yield return null;
                Assert.That(P.Inventory.Visible, Is.False); Assert.That(GamePause.IsPaused, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator MusicStartsSombreAndCrossfadesWhenRestored()
        {
            yield return Load(GameFlow.ArroyoScene);
            var music = Object.FindFirstObjectByType<MusicPlayer>();
            Assert.That(music.DegradedClip, Is.Not.Null, "Pista de lo degradado");
            Assert.That(music.Mix, Is.EqualTo(0f).Within(.01f));
            music.SetRestored(true, true);
            Assert.That(music.Mix, Is.EqualTo(1f).Within(.01f), "Al restaurar suena el tema del bosque");
        }
    }
}
