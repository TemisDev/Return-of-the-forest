using Bosque.Session;
using NUnit.Framework;

namespace Bosque.Tests
{
    public sealed class ArroyoTimingTests
    {
        [Test] public void TimingSummaryUsesNearestRankAndCountsFramesOverBudget()
        {
            var sample = new ArroyoBuildProbe.Sample();
            foreach (float dt in new[] { .01f, .02f, .03f, .04f }) sample.RecordFrame(dt);
            sample.Finish(.1f);
            Assert.That(sample.frames, Is.EqualTo(4));
            Assert.That(sample.meanFps, Is.EqualTo(40).Within(.01));
            Assert.That(sample.p50FrameMs, Is.EqualTo(20).Within(.01));
            Assert.That(sample.p95FrameMs, Is.EqualTo(40).Within(.01));
            Assert.That(sample.p99FrameMs, Is.EqualTo(40).Within(.01));
            Assert.That(sample.minFps, Is.EqualTo(25).Within(.01));
            Assert.That(sample.framesOver60Budget, Is.EqualTo(3));
        }
        [Test] public void InvalidFrameDurationsCannotPolluteTheReport()
        {
            var sample = new ArroyoBuildProbe.Sample();
            foreach (float dt in new[] { 0, -1, float.NaN, float.PositiveInfinity }) sample.RecordFrame(dt);
            sample.RecordFrame(.01f); sample.Finish(.01f);
            Assert.That(sample.frames, Is.EqualTo(1));
            Assert.That(sample.p99FrameMs, Is.EqualTo(10).Within(.01));
            Assert.That(sample.framesOver60Budget, Is.Zero);
        }
    }
}
