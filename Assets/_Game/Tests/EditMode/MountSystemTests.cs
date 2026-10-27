using System.Collections.Generic;
using NUnit.Framework;
using SoulsGame.Player;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Tests
{
    public class MountSystemTests
    {
        private static readonly List<ForbiddenZone> Zones = new List<ForbiddenZone>
        {
            new ForbiddenZone { Center = new Vector3(0f, 0f, 0f), Radius = 38f } // الحصن
        };

        private static readonly Vector3 Meadow = new Vector3(50f, 0f, -80f);

        [Test]
        public void Summon_FromLocomotion_Mounts()
        {
            var mount = new MountSystem();
            Assert.IsTrue(mount.TrySummon(Meadow, Zones, PlayerStateId.Locomotion, menuOpen: false));
            Assert.IsTrue(mount.IsMounted);
        }

        [Test]
        public void Summon_InForbiddenZone_Rejected()
        {
            var mount = new MountSystem();
            Assert.IsFalse(mount.TrySummon(new Vector3(10f, 0f, 5f), Zones,
                PlayerStateId.Locomotion, menuOpen: false), "داخل الحصن ممنوع");
            Assert.IsFalse(mount.IsMounted);
        }

        [Test]
        public void Summon_DuringCommitment_Rejected()
        {
            var mount = new MountSystem();
            foreach (var state in new[]
                     { PlayerStateId.AttackLight, PlayerStateId.AttackHeavy,
                       PlayerStateId.Dodge, PlayerStateId.Stagger, PlayerStateId.Death })
                Assert.IsFalse(mount.TrySummon(Meadow, Zones, state, menuOpen: false),
                    $"ممنوع أثناء {state}");
        }

        [Test]
        public void Summon_WhileMenuOpen_Rejected()
        {
            var mount = new MountSystem();
            Assert.IsFalse(mount.TrySummon(Meadow, Zones, PlayerStateId.Locomotion, menuOpen: true));
        }

        [Test]
        public void Toggle_Dismounts()
        {
            var mount = new MountSystem();
            mount.TrySummon(Meadow, Zones, PlayerStateId.Locomotion, false);

            mount.Dismount();

            Assert.IsFalse(mount.IsMounted);
        }

        [Test]
        public void RiderHit_ForcesDismount()
        {
            var mount = new MountSystem();
            mount.TrySummon(Meadow, Zones, PlayerStateId.Locomotion, false);

            Assert.IsTrue(mount.NotifyRiderHit(), "ضربة الراكب تُنزله");
            Assert.IsFalse(mount.IsMounted);

            Assert.IsFalse(mount.NotifyRiderHit(), "غير راكب: لا شيء");
        }
    }
}
