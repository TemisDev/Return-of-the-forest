using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.Tests
{
    public sealed class ArroyoSceneTests
    {
        private ArroyoSession session;
        private ThirdPersonMotor player;
        private PlayerInteractor actor;
        [SetUp] public void SetUp()
        {
            EditorSceneManager.OpenScene("Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity");
            session = Object.FindFirstObjectByType<ArroyoSession>();
            Assert.That(session, Is.Not.Null, "Falta integrar ArroyoGameplaySetup.");
            session.InitializeFromScene();
            player = Object.FindFirstObjectByType<ThirdPersonMotor>();
            actor = player.GetComponent<PlayerInteractor>();
        }
        [TearDown] public void TearDown() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        [Test] public void PhysicalBarrierBlocksWalkingAndJumpingUntilRestored()
        {
            foreach (bool jump in new[] { false, true })
            {
                player.Teleport(new Vector3(3, 0, 33), Quaternion.identity);
                Physics.SyncTransforms();
                for (int i = 0; i < 60; i++) player.Simulate(Vector2.up, jump && i == 1, 0, .02f);
                Assert.That(player.transform.position.z, Is.LessThan(35));
                Assert.That(session.Progress.IsCompleted, Is.False);
            }
        }
        [Test] public void RealObstaclesRestoreAndCanBeResetWithoutDuplicatingComponents()
        {
            for (int cycle = 0; cycle < 2; cycle++)
            {
                foreach (int z in new[] { 26, 10, 18 })
                {
                    player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity);
                    Physics.SyncTransforms(); actor.Scan();
                    Assert.That(actor.Target, Is.Not.Null, "No alcanza el obstáculo en Z " + z);
                    Assert.That(actor.TryInteract(), Is.True);
                    Assert.That(actor.TryInteract(), Is.False);
                }
                Assert.That(session.Progress.IsRestored, Is.True);
                player.Teleport(new Vector3(3, 0, 33), Quaternion.identity);
                Physics.SyncTransforms();
                for (int i = 0; i < 40; i++) player.Simulate(Vector2.up, false, 0, .02f);
                Assert.That(player.transform.position.z, Is.GreaterThan(35.5f));
                Assert.That(session.TryExit(player), Is.True);
                session.Restart();
                Assert.That(session.Progress.RemovedCount, Is.Zero);
                Assert.That(player.transform.position, Is.EqualTo(new Vector3(3, 0, 0)));
                Assert.That(player.GetComponents<ThirdPersonMotor>().Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<RemovableObstacle>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
            }
        }
    }
}
