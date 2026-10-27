using UnityEngine;

namespace SoulsGame.Story
{
    // شخصية حوار ثابتة في العالم: لا Hurtbox ولا تسجيل في الـ lock-on —
    // لا تُستهدف ولا تُقتل. زر التفاعل ضمن المدى يبدأ حوار مرحلتها النشطة.
    public class NpcAvatar : MonoBehaviour
    {
        public const float TalkRadius = 2.5f;

        public NpcQuestline Questline;

        public bool InRange(Vector3 playerPos) =>
            Vector3.Distance(playerPos, transform.position) <= TalkRadius;
    }
}
