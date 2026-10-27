using System.Collections.Generic;
using NUnit.Framework;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class LockOnSelectorTests
    {
        private class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }
            public bool IsAlive { get; set; } = true;
        }

        private static readonly Vector3 Origin = Vector3.zero;
        private static readonly Vector3 Forward = Vector3.forward;

        private static ITargetable Select(IReadOnlyList<ITargetable> candidates) =>
            LockOnSelector.Select(Origin, Forward, candidates, maxDistance: 15f, maxAngleDegrees: 60f);

        [Test]
        public void Select_PicksNearestInCone()
        {
            var near = new FakeTarget { Position = new Vector3(1f, 0f, 5f) };
            var far = new FakeTarget { Position = new Vector3(-1f, 0f, 10f) };

            Assert.AreSame(near, Select(new ITargetable[] { far, near }));
        }

        [Test]
        public void Select_IgnoresOutOfAngleAndDeadAndFar()
        {
            var behind = new FakeTarget { Position = new Vector3(0f, 0f, -5f) };          // خارج الزاوية
            var dead = new FakeTarget { Position = new Vector3(0f, 0f, 5f), IsAlive = false };
            var tooFar = new FakeTarget { Position = new Vector3(0f, 0f, 30f) };          // أبعد من 15

            Assert.IsNull(Select(new ITargetable[] { behind, dead, tooFar }));
        }

        [Test]
        public void Select_EmptyCandidates_ReturnsNull()
        {
            Assert.IsNull(Select(new ITargetable[0]));
        }

        [Test]
        public void Switch_PicksNextInDirection()
        {
            var center = new FakeTarget { Position = new Vector3(0f, 0f, 6f) };
            var right = new FakeTarget { Position = new Vector3(4f, 0f, 6f) };
            var left = new FakeTarget { Position = new Vector3(-4f, 0f, 6f) };
            var all = new ITargetable[] { left, center, right };

            var toRight = LockOnSelector.Switch(center, +1, Origin, Forward, all, 15f, 60f);
            Assert.AreSame(right, toRight);

            var toLeft = LockOnSelector.Switch(center, -1, Origin, Forward, all, 15f, 60f);
            Assert.AreSame(left, toLeft);
        }

        [Test]
        public void Switch_NoTargetOnThatSide_KeepsCurrent()
        {
            var center = new FakeTarget { Position = new Vector3(0f, 0f, 6f) };
            var right = new FakeTarget { Position = new Vector3(4f, 0f, 6f) };
            var all = new ITargetable[] { center, right };

            var result = LockOnSelector.Switch(right, +1, Origin, Forward, all, 15f, 60f);
            Assert.AreSame(right, result, "لا هدف أبعد يميناً: يبقى الحالي");
        }
    }
}
