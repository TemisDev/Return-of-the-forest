using Bosque.Session;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.Tests
{
    public sealed class ArroyoVisualTests
    {
        [SetUp] public void SetUp() => EditorSceneManager.OpenScene("Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity");
        [TearDown] public void TearDown() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        [Test] public void VisualPrefabsHaveMeshesUrpMaterialsAndNoGameplayComponents()
        {
            string[] names = { "Rock_01", "TreeTrunk_01", "Stump_01", "PineCanopy_01", "GrassClump_01", "RiverReeds_01", "ShrubCluster_01" };
            foreach (var name in names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Bosque/Art/Prefabs/VIS_" + name + ".prefab");
                Assert.That(prefab, Is.Not.Null, name);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty, name);
                Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty, name);
                Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true).Length, Is.GreaterThan(0));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true)) Assert.That(filter.sharedMesh, Is.Not.Null);
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.That(material, Is.Not.Null, name);
                        Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"), name);
                    }
            }
        }
        [Test] public void ArtMountsRemainVisualOnlyAndFollowExistingRestorationState()
        {
            var permanent = GameObject.Find("Gameplay/VisualIntegrationV3");
            Assert.That(permanent, Is.Not.Null);
            var session = Object.FindFirstObjectByType<ArroyoSession>(); session.InitializeFromScene();
            var stateRoot = GameObject.Find("ArroyoGreybox").transform;
            Transform dry = null, alive = null;
            foreach (var item in stateRoot.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != "ArtV3") continue;
                Assert.That(item.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(item.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
                if (item.parent.name == "DegradedVisuals") dry = item;
                if (item.parent.name == "RestoredVisuals") alive = item;
            }
            Assert.That(dry, Is.Not.Null); Assert.That(alive, Is.Not.Null);
            Assert.That(dry.gameObject.activeInHierarchy && !alive.gameObject.activeInHierarchy, Is.True);
            session.Progress.TryRemove("arroyo.log"); session.Progress.TryRemove("arroyo.rocks"); session.Progress.TryRemove("arroyo.branches");
            Assert.That(!dry.gameObject.activeInHierarchy && alive.gameObject.activeInHierarchy, Is.True);
            session.Restart();
            Assert.That(dry.gameObject.activeInHierarchy && !alive.gameObject.activeInHierarchy, Is.True);
            Assert.That(permanent.GetComponentsInChildren<Collider>(true), Is.Empty);
        }
        [Test] public void ActiveVisualBudgetStaysBoundedInBothStates()
        {
            var session = Object.FindFirstObjectByType<ArroyoSession>(); session.InitializeFromScene();
            for (int state = 0; state < 2; state++)
            {
                int activeRenderers = 0, triangles = 0;
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!renderer.enabled) continue;
                    activeRenderers++;
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        for (int submesh = 0; submesh < filter.sharedMesh.subMeshCount; submesh++)
                            triangles += (int)filter.sharedMesh.GetIndexCount(submesh) / 3;
                }
                Debug.Log("[ArteV6] Estado " + state + ": " + activeRenderers + " renderers, " + triangles + " triángulos.");
                // QA6-016: presupuesto del arte V6 (antes V3: <180 renderers, <20 000 triángulos). Es una guarda de
                // regresión con ~10 % de margen sobre lo medido en la integración (degradado 590 / 2,04 M;
                // restaurado 1100 / 2,70 M, sumando todas las mallas de LOD). La aceptación real de rendimiento es
                // el p95 del verificador de builds (Tools/Verify-ArroyoV4.ps1) en el equipo de referencia.
                Assert.That(activeRenderers, Is.LessThan(1250)); Assert.That(triangles, Is.LessThan(3000000));
                if (state == 0)
                { session.Progress.TryRemove("arroyo.log"); session.Progress.TryRemove("arroyo.rocks"); session.Progress.TryRemove("arroyo.branches"); }
            }
        }
        [Test] public void PolishedHudAndEffectHaveRequiredArtReferences()
        {
            var hud = Object.FindFirstObjectByType<ArroyoPrototypeHud>();
            Assert.That(hud.HasArtIcons, Is.True, "Falta configurar uno de los cuatro iconos.");
            var effects = Object.FindObjectsByType<RestorationFeedback>(FindObjectsSortMode.None);
            Assert.That(effects.Length, Is.EqualTo(1));
            var particles = effects[0].GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            Assert.That(particles.main.loop, Is.False);
            Assert.That(particles.main.playOnAwake, Is.False);
            Assert.That(particles.main.maxParticles, Is.LessThanOrEqualTo(24));
            Assert.That(particles.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name,
                Is.EqualTo("Universal Render Pipeline/Particles/Unlit"));
        }
    }
}
