using System.Collections;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bosque.Tests
{
    public sealed class ArroyoPlaySmokeTests
    {
        [UnityTest] public IEnumerator ActualExitVolumeCompletesOnlyAfterRestoringAndRestartIsClean()
        {
            EditorSceneManager.OpenScene("Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var player = Object.FindFirstObjectByType<ThirdPersonMotor>();
            var actor = player.GetComponent<PlayerInteractor>();
            var input = player.GetComponent<ThirdPersonInput>();
            input.enabled = false; // Test drives the existing motor; it does not claim hardware input coverage.
            player.Teleport(new Vector3(3, 0, 33), Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < 45; i++)
            {
                player.Simulate(Vector2.up, false, 0, .02f);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(session.Progress.IsCompleted, Is.False, "La salida debe permanecer bloqueada.");
            foreach (int z in new[] { 18, 26, 10 })
            {
                player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity);
                Physics.SyncTransforms();
                Assert.That(actor.TryInteract(), Is.True);
            }
            player.Teleport(new Vector3(3, 0, 33), Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < 60 && !session.Progress.IsCompleted; i++)
            {
                player.Simulate(Vector2.up, false, 0, .02f);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(session.Progress.IsCompleted, Is.True, "Posición final: " + player.transform.position);
            Assert.That(input.enabled, Is.False);
            Assert.That(actor.enabled, Is.False);
            session.Restart();
            Assert.That(session.Progress.RemovedCount, Is.Zero);
            Assert.That(input.enabled && actor.enabled, Is.True);
            yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
