using System;
using System.Collections.Generic;

namespace SoulsGame.Story
{
    // أعلام تقدم القصة — تُرفع ولا تُنزل أبداً (القصة تمشي للأمام فقط).
    public enum StoryFlag
    {
        CaveDiscovered,
        ChapelDiscovered,
        BossDefeated,
        BellFound,
        BellReturned,
        AshCarried,
        AshPlaced
    }

    // مخزن الأعلام: الأنظمة ترفع، خطوط المهام تقرأ، والحدث يُعلم المهتمين.
    public class StoryState
    {
        private readonly HashSet<StoryFlag> flags = new HashSet<StoryFlag>();

        public event Action<StoryFlag> FlagChanged;

        public bool IsSet(StoryFlag flag) => flags.Contains(flag);

        public void Set(StoryFlag flag)
        {
            if (!flags.Add(flag))
                return; // علم مرفوع لا يطلق الحدث ثانية
            FlagChanged?.Invoke(flag);
        }
    }
}
