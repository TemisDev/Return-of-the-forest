using Bosque.Session;
using Bosque.Interaction;
using Bosque.Player;
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
        [TestCase(false)] [TestCase(true)] public void MovingInteractionDoesNotInterruptTheController(bool jump)
        {
            var player = session.Player;
            var actor = player.GetComponent<PlayerInteractor>();
            player.Teleport(new Vector3(-.6f, 0, 10), Quaternion.identity);
            Physics.SyncTransforms();
            player.Simulate(Vector2.zero, false, 0, .02f);
            player.Simulate(Vector2.up, jump, 0, .02f);
            Physics.SyncTransforms();
            Assert.That(actor.TryInteract(), Is.True, "Interaccion desde la orilla durante movimiento/salto.");
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(1));
            var before = player.transform.position;
            for (int i = 0; i < 12; i++) player.Simulate(Vector2.up, false, 0, .02f);
            Assert.That(player.transform.position.z, Is.GreaterThan(before.z + .5f));
            Assert.That(player.GetComponent<CharacterController>().enabled, Is.True);
        }
        [Test] public void DecorationDoesNotHideTargetsFromNineBankApproaches()
        {
            var player = session.Player;
            var actor = player.GetComponent<PlayerInteractor>();
            foreach (int z in new[] { 10, 18, 26 })
                foreach (float offset in new[] { -.8f, 0, .8f })
                {
                    player.Teleport(new Vector3(-.6f, 0, z + offset), Quaternion.identity);
                    Physics.SyncTransforms(); actor.Scan();
                    Assert.That(actor.Target, Is.Not.Null, "Decoracion tapa objetivo Z=" + z + ", offset=" + offset);
                }
            Assert.That(session.Progress.RemovedCount, Is.Zero, "Escanear no retira nada.");
        }
        [Test] public void CameraOrbitRemainsFiniteAndUnobstructedAlongV4Bank()
        {
            var player = session.Player;
            var camera = Object.FindFirstObjectByType<ThirdPersonCamera>();
            int samples = 0;
            for (int state = 0; state < 2; state++)
            {
                foreach (float x in new[] { -.6f, 3, 5.2f })
                    foreach (int z in new[] { 3, 10, 18, 26, 33 })
                    {
                        player.Teleport(new Vector3(x, 0, z), Quaternion.identity);
                        Physics.SyncTransforms(); camera.Configure(player.transform);
                        for (int angle = 0; angle < 16; angle++)
                        {
                            camera.Step(new Vector2(187.5f, 0), .02f);
                            for (int frame = 0; frame < 30; frame++) camera.Step(Vector2.zero, .02f);
                            var focus = player.transform.position + Vector3.up * 1.35f;
                            var offset = camera.transform.position - focus;
                            Assert.That(float.IsNaN(offset.sqrMagnitude) || float.IsInfinity(offset.sqrMagnitude), Is.False);
                            Assert.That(offset.magnitude, Is.InRange(.19f, 4.5f));
                            foreach (var hit in Physics.RaycastAll(focus, offset.normalized, offset.magnitude - .1f, ~0, QueryTriggerInteraction.Ignore))
                                Assert.That(hit.transform == player.transform || hit.transform.IsChildOf(player.transform), Is.True,
                                    "Camara detras de collider " + hit.transform.name + " en " + player.transform.position);
                            samples++;
                        }
                    }
                if (state == 0) Remove(3);
            }
            Assert.That(samples, Is.EqualTo(480));
        }
    }
}
