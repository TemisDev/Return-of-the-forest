using System;
using Bosque.Interaction;
using Bosque.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.Tests
{
    public sealed class ArroyoGameplayTests
    {
        private GameObject world;
        private PlayerInteractor actor;
        [SetUp] public void SetUp()
        {
            world = new GameObject("GameplayTestWorld");
            world.transform.position = new Vector3(1000, 0, 1000);
            var player = new GameObject("Actor");
            player.transform.SetParent(world.transform, false);
            actor = player.AddComponent<PlayerInteractor>();
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(world);
        private TestInteractionTarget Target(Vector3 offset)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(world.transform, false);
            go.transform.localPosition = offset;
            return go.AddComponent<TestInteractionTarget>();
        }
        private static RestorationProgress Progress() => new RestorationProgress(new[] { "a", "b", "c" });

        [Test] public void InRangeTargetCanBeInteractedWith()
        {
            var target = Target(new Vector3(0, .9f, 2));
            Physics.SyncTransforms(); actor.Scan();
            Assert.That(actor.Target, Is.EqualTo(target));
            Assert.That(actor.TryInteract(), Is.True);
            Assert.That(target.Count, Is.EqualTo(1));
        }
        [Test] public void LeavingRangeCancelsTargetAndRejectsStaleInteraction()
        {
            Target(new Vector3(0, .9f, 2));
            Physics.SyncTransforms(); actor.Scan();
            actor.transform.position += Vector3.forward * 10;
            Physics.SyncTransforms();
            Assert.That(actor.TryInteract(), Is.False);
            Assert.That(actor.Target, Is.Null);
        }
        [Test] public void InvalidAndDisabledTargetsAreIgnored()
        {
            var invalid = Target(new Vector3(0, .9f, 1)); invalid.Available = false;
            var disabled = Target(new Vector3(1, .9f, 1)); disabled.enabled = false;
            Physics.SyncTransforms(); actor.Scan();
            Assert.That(actor.Target, Is.Null);
        }
        [Test] public void NearestAvailableSurfaceIsSelected()
        {
            Target(new Vector3(0, .9f, 2.5f));
            var near = Target(new Vector3(0, .9f, 1.2f));
            Physics.SyncTransforms(); actor.Scan();
            Assert.That(actor.Target, Is.EqualTo(near));
        }
        [Test] public void WallPreventsInteraction()
        {
            Target(new Vector3(0, .9f, 2));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(world.transform, false);
            wall.transform.localPosition = new Vector3(0, .9f, 1);
            wall.transform.localScale = new Vector3(4, 3, .2f);
            Physics.SyncTransforms(); actor.Scan();
            Assert.That(actor.TryInteract(), Is.False);
        }
        [Test] public void UnknownAndRepeatedIdsNeverAdvanceProgress()
        {
            var progress = Progress();
            Assert.That(progress.TryRemove("unknown"), Is.False);
            Assert.That(progress.TryRemove("a"), Is.True);
            for (int i = 0; i < 10; i++) Assert.That(progress.TryRemove("a"), Is.False);
            Assert.That(progress.RemovedCount, Is.EqualTo(1));
            Assert.That(progress.IsRemoved("a"), Is.True);
        }
        [Test] public void AnyOrderRestoresExactlyOnceAndOnlyThenAllowsCompletion()
        {
            var progress = Progress(); int restorations = 0;
            progress.Restored += () => restorations++;
            Assert.That(progress.TryComplete(), Is.False);
            progress.TryRemove("c"); progress.TryRemove("a");
            Assert.That(progress.IsRestored, Is.False);
            Assert.That(progress.TryComplete(), Is.False);
            progress.TryRemove("b"); progress.TryRemove("b");
            Assert.That(progress.RemovedCount, Is.EqualTo(3));
            Assert.That(progress.TotalCount, Is.EqualTo(3));
            Assert.That(progress.IsRestored, Is.True);
            Assert.That(restorations, Is.EqualTo(1));
            Assert.That(progress.TryComplete(), Is.True);
            Assert.That(progress.TryComplete(), Is.False);
        }
        [Test] public void ResetStartsAnotherCleanCycleAndPublishesChanges()
        {
            var progress = Progress(); int changes = 0;
            progress.Changed += () => changes++;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                progress.TryRemove("a"); progress.TryRemove("b"); progress.TryRemove("c");
                progress.TryComplete(); progress.Reset();
                Assert.That(progress.RemovedCount, Is.Zero);
                Assert.That(progress.IsRestored, Is.False);
                Assert.That(progress.IsCompleted, Is.False);
            }
            Assert.That(changes, Is.EqualTo(15));
        }
        [Test] public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new RestorationProgress(new string[0]));
            Assert.Throws<ArgumentException>(() => new RestorationProgress(new[] { "a", "a" }));
            Assert.Throws<ArgumentException>(() => new RestorationProgress(new[] { " " }));
        }
    }

    public sealed class TestInteractionTarget : InteractionTarget
    {
        public bool Available = true;
        public int Count;
        public override string Prompt => "Prueba";
        public override bool CanInteract(PlayerInteractor actor) => Available;
        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!Available) return false;
            Count++; Available = false; return true;
        }
    }
}
