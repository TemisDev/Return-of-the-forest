using Bosque.Session;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.Tests
{
    public sealed class ArroyoV4Tests
    {
        private ArroyoSession session;
        [SetUp] public void SetUp()
        {
            EditorSceneManager.OpenScene("Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity");
            session = Object.FindFirstObjectByType<ArroyoSession>(); session.InitializeFromScene();
        }
        [TearDown] public void TearDown() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        private void Remove(int count)
        {
            string[] ids = { "arroyo.log", "arroyo.rocks", "arroyo.branches" };
            for (int i = 0; i < count; i++) session.Progress.TryRemove(ids[i]);
        }
        [Test] public void V4MountAndFoamAreVisualOnlyTransparentAndAboveWater()
        {
            var mount = GameObject.Find("Gameplay/ArtV4Integration");
            Assert.That(mount, Is.Not.Null, "Integrar Arte V4 antes de validar.");
            Assert.That(mount.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(mount.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Remove(3);
            var root = GameObject.Find("ArroyoGreybox").transform;
            Renderer water = null;
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == "Water") water = node.GetComponent<Renderer>();
            Assert.That(water, Is.Not.Null);
            int foamCount = 0;
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (!node.name.StartsWith("V4Foam_")) continue;
                foamCount++;
                var renderer = node.GetComponentInChildren<Renderer>();
                Assert.That(renderer.sharedMaterial.GetFloat("_Surface"), Is.EqualTo(1));
                Assert.That(renderer.sharedMaterial.GetFloat("_ZWrite"), Is.Zero);
                Assert.That(renderer.sharedMaterial.renderQueue, Is.GreaterThanOrEqualTo(3000));
                Assert.That(renderer.bounds.min.y - water.bounds.max.y, Is.InRange(.006f, .1f));
                Assert.That(node.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            Assert.That(foamCount, Is.EqualTo(4));
        }
        [TestCase(1)] [TestCase(3)] public void ResetBeforeExitRestoresWholePuzzle(int removed)
        {
            Remove(removed); session.Restart();
            Assert.That(session.Progress.RemovedCount, Is.Zero);
            Assert.That(session.Progress.IsRestored || session.Progress.IsCompleted, Is.False);
            Assert.That(session.TryExit(session.Player), Is.False);
            Assert.That(session.Player.transform.position, Is.EqualTo(new Vector3(3, 0, 0)));
        }
        [Test] public void FallingAfterRestorationKeepsProgressAndAllowsExit()
        {
            Remove(3);
            session.Player.Teleport(new Vector3(7, -8, 20), Quaternion.identity);
            session.Player.Simulate(Vector2.zero, false, 0, .02f);
            Assert.That(session.Player.transform.position, Is.EqualTo(new Vector3(3, 0, 2)));
            Assert.That(session.Progress.IsRestored, Is.True);
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(3));
            Assert.That(session.TryExit(session.Player), Is.True);
        }
    }
}
