using Bosque.Session;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.Tests
{
    public sealed class RestorationFeedbackTests
    {
        private GameObject root;
        private ArroyoSession session;
        private RestorationFeedback feedback;
        private ParticleSystem particles;
        [SetUp] public void SetUp()
        {
            EditorSceneManager.OpenScene("Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity");
            session = Object.FindFirstObjectByType<ArroyoSession>(); session.InitializeFromScene();
            root = new GameObject("FeedbackTest");
            particles = root.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            feedback = root.AddComponent<RestorationFeedback>(); feedback.Configure(session, particles);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(root); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
        private void Restore()
        {
            session.Progress.TryRemove("arroyo.log"); session.Progress.TryRemove("arroyo.rocks"); session.Progress.TryRemove("arroyo.branches");
        }
        [Test] public void EffectRunsOncePerCycleAndRestartClearsIt()
        {
            Restore(); Assert.That(feedback.PresentationCount, Is.EqualTo(1));
            session.Progress.TryRemove("arroyo.log"); session.Progress.TryComplete();
            Assert.That(feedback.PresentationCount, Is.EqualTo(1));
            session.Restart(); Assert.That(particles.particleCount, Is.Zero);
            Restore(); Assert.That(feedback.PresentationCount, Is.EqualTo(2));
        }
        [Test] public void DisablingAndRebindingDoesNotAccumulateEventSubscriptions()
        {
            feedback.enabled = false; feedback.enabled = true;
            feedback.Configure(session, particles); feedback.Configure(session, particles);
            Restore(); Assert.That(feedback.PresentationCount, Is.EqualTo(1));
            feedback.enabled = false; session.Restart(); Restore();
            Assert.That(feedback.PresentationCount, Is.EqualTo(1));
        }
    }
}
