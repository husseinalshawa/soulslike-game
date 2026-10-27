using System.Collections.Generic;
using SoulsGame.Player;
using UnityEngine;

namespace SoulsGame.Combat
{
    // غلاف القذيفة المتوهجة: يقود ProjectileLogic ضد أهداف الـ lock-on
    // المسجلة، ويطبق الضرر عبر ApplyDamage ثم يفنى.
    public class ProjectileAvatar : MonoBehaviour
    {
        private const float TargetRadius = 0.8f;

        private ProjectileLogic logic;
        private DamagePacket packet;

        public void Launch(Vector3 origin, Vector3 direction, float speed, float maxRange, DamagePacket damagePacket)
        {
            logic = new ProjectileLogic(origin, direction, speed, maxRange);
            packet = damagePacket;
            transform.position = origin;
        }

        // أهداف القذائف الصالحة: الأحياء المسجلون في الـ lock-on — ما عدا البوس
        // قبل بدء معركته (لا قنص للمعركة من خلف الضباب وهو خامل).
        // قائمة معاد تدويرها — لا تخصيص ذاكرة كل إطار (خيط اللعبة الرئيسي وحده)
        private static readonly List<(object target, Vector3 position, float radius)> reusableCandidates
            = new List<(object, Vector3, float)>();

        public static List<(object target, Vector3 position, float radius)> CollectCandidates()
        {
            var candidates = reusableCandidates;
            candidates.Clear();
            var targets = LockOnSystem.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!targets[i].IsAlive)
                    continue;
                if (targets[i] is Enemies.BossAvatar boss && !boss.FightActive)
                    continue;
                var component = targets[i] as Component;
                var damageable = component != null ? component.GetComponent<Damageable>() : null;
                if (damageable != null)
                    candidates.Add((damageable, targets[i].Position, TargetRadius));
            }
            return candidates;
        }

        private void Update()
        {
            if (logic == null)
                return;

            object hit = logic.Step(Time.deltaTime, CollectCandidates());
            transform.position = logic.Position;

            if (hit is Damageable target)
                target.ApplyDamage(packet);

            if (logic.Expired)
                Destroy(gameObject);
        }
    }
}
