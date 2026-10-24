using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerLabTrailTests
    {
        [Test]
        public void TrailUsesRecordedWorldPositionsAndClearDoesNotEraseMeasurements()
        {
            var trace = new PlayerMotionTrace();
            for (int i = 0; i < 8; i++) trace.Record(.1f, new Vector2(i, i * i), Vector2.zero, 1, 0, 0, "fall");
            var root = new GameObject("Trail test");
            try
            {
                var line = root.AddComponent<LineRenderer>();
                var draw = typeof(PlayerSchemeWindow).Assembly.GetType("Game.Player.Editor.PlayerLabTrail")
                    .GetMethod("DrawTrace", BindingFlags.Static | BindingFlags.NonPublic);
                double before = trace.Time;
                draw.Invoke(null, new object[] { line, trace, 5f, double.NegativeInfinity, -.02f });
                Assert.That(line.positionCount, Is.EqualTo(8));
                Assert.That(line.GetPosition(7), Is.EqualTo(new Vector3(7, 49, -.02f)));
                draw.Invoke(null, new object[] { line, trace, 5f, trace[7].Time, -.02f });
                Assert.That(line.positionCount, Is.Zero);
                Assert.That(trace.Count, Is.EqualTo(8)); Assert.That(trace.Time, Is.EqualTo(before));
                trace.Record(.1f, new Vector2(8, 64), Vector2.zero, 1, 0, 0, "fall");
                draw.Invoke(null, new object[] { line, trace, 5f, trace[7].Time, -.02f });
                Assert.That(line.positionCount, Is.EqualTo(1));
                Assert.That(line.GetPosition(0).x, Is.EqualTo(8));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TrailExpiresByPhysicsTimeAndHandlesRingWrapAndEmptyTrace()
        {
            var trace = new PlayerMotionTrace();
            for (int i = 0; i < 600; i++) trace.Record(.02f, new Vector2(i, 0), Vector2.zero, 1, 0, 0, "move");
            var root = new GameObject("Trail lifetime test");
            try
            {
                var line = root.AddComponent<LineRenderer>();
                var draw = typeof(PlayerSchemeWindow).Assembly.GetType("Game.Player.Editor.PlayerLabTrail")
                    .GetMethod("DrawTrace", BindingFlags.Static | BindingFlags.NonPublic);
                draw.Invoke(null, new object[] { line, trace, .1f, double.NegativeInfinity, 0f });
                Assert.That(line.positionCount, Is.InRange(4, 6));
                Assert.That(line.GetPosition(line.positionCount - 1).x, Is.EqualTo(599));
                int count = line.positionCount;
                draw.Invoke(null, new object[] { line, trace, .1f, double.NegativeInfinity, 0f });
                Assert.That(line.positionCount, Is.EqualTo(count), "仅重绘不推进时间");
                trace.Clear();
                draw.Invoke(null, new object[] { line, trace, .1f, double.NegativeInfinity, 0f });
                Assert.That(line.positionCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
