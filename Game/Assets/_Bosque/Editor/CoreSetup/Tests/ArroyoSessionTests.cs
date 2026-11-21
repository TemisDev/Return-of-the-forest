using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.Tests
{
    public sealed class ArroyoSessionTests
    {
        private GameObject root, degraded, restored, barrier;
        private ArroyoSession session;
        private ThirdPersonMotor motor;
        private PlayerInteractor interactor;
        private Transform spawn;
        private RemovableObstacle[] obstacles;
        [SetUp] public void SetUp()
        {
            root = new GameObject("SessionTest"); root.transform.position = new Vector3(1500, 0, 1500);
            var player = Child("Player");
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.center = Vector3.up * .9f;
            motor = player.AddComponent<ThirdPersonMotor>(); interactor = player.AddComponent<PlayerInteractor>();
            spawn = Child("Spawn").transform; spawn.localPosition = new Vector3(5, 0, 2);
            degraded = Child("Degraded"); restored = Child("Restored"); barrier = Child("Barrier");
            obstacles = new RemovableObstacle[3];
            for (int i = 0; i < 3; i++)
            {
                var go = Child("Obstacle" + i); go.transform.localPosition = new Vector3(i * 5, 0, 1.5f);
                var shape = go.AddComponent<BoxCollider>(); shape.size = new Vector3(1, .8f, 1);
                obstacles[i] = go.AddComponent<RemovableObstacle>(); obstacles[i].Configure("o" + i, shape);
            }
            session = Child("Session").AddComponent<ArroyoSession>();
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);
        private GameObject Child(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false); return go;
        }
        private void Configure() { Physics.SyncTransforms(); session.Configure(obstacles, degraded, restored, barrier, motor, spawn); }
        private void Restore()
        {
            for (int i = 0; i < 3; i++)
            {
                var cc = motor.GetComponent<CharacterController>(); cc.enabled = false;
                motor.transform.position = obstacles[i].transform.position + new Vector3(0, 0, -1.5f);
                cc.enabled = true; Physics.SyncTransforms();
                Assert.That(interactor.TryInteract(), Is.True);
            }
        }
        [Test] public void EnvironmentAndBarrierFollowProgressAndRestart()
        {
            Configure();
            Assert.That(barrier.activeSelf, Is.True);
            Assert.That(degraded.activeSelf, Is.True);
            Assert.That(restored.activeSelf, Is.False);
            Assert.That(session.TryExit(motor), Is.False);
            Restore();
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(3));
            Assert.That(degraded.activeSelf, Is.False);
            Assert.That(restored.activeSelf, Is.True);
            Assert.That(barrier.activeSelf, Is.False);
            Assert.That(session.TryExit(motor), Is.True);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                session.Restart();
                Assert.That(session.Progress.RemovedCount, Is.Zero);
                Assert.That(session.Progress.IsCompleted, Is.False);
                Assert.That(barrier.activeSelf && degraded.activeSelf && !restored.activeSelf, Is.True);
                Assert.That(Vector3.Distance(motor.transform.position, spawn.position), Is.LessThan(.01f));
                foreach (var obstacle in obstacles) Assert.That(obstacle.gameObject.activeSelf, Is.True);
                Restore(); Assert.That(session.TryExit(motor), Is.True);
            }
        }
        [Test] public void StandingOnObstacleCannotRemoveItsSupport()
        {
            Configure(); var cc = motor.GetComponent<CharacterController>(); cc.enabled = false;
            motor.transform.position = obstacles[0].transform.position + Vector3.up * .4f;
            cc.enabled = true; Physics.SyncTransforms();
            Assert.That(obstacles[0].TryInteract(interactor), Is.False);
            Assert.That(obstacles[0].gameObject.activeSelf, Is.True);
            Assert.That(session.Progress.RemovedCount, Is.Zero);
        }
        [Test] public void RemovedObstacleCannotCountTwiceOrAcceptForeignActorAtExit()
        {
            Configure(); Physics.SyncTransforms();
            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(obstacles[0].TryInteract(interactor), Is.False);
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(1));
            Assert.That(session.TryExit(null), Is.False);
        }
    }
}
