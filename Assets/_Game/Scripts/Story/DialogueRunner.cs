using System;
using System.Collections.Generic;

namespace SoulsGame.Story
{
    // جلسة حوار واحدة: أسطر المرحلة النشطة تتقدم بزر التفاعل، وعلى آخر سطر
    // يقع فعل إكمال المرحلة ثم تنتهي. الموت يجهضها بلا إكمال.
    public class DialogueRunner
    {
        private IReadOnlyList<string> lines;
        private int index;
        private Action onComplete;

        public bool IsActive { get; private set; }
        public string SpeakerName { get; private set; }
        public string CurrentLine =>
            IsActive && lines != null && index < lines.Count ? lines[index] : string.Empty;

        // يطلق عند البدء وكل تقدم ونهاية — لوحة الحوار تستهلكه.
        public event Action SessionChanged;

        public void Begin(string speaker, IReadOnlyList<string> sessionLines, Action completeAction)
        {
            if (sessionLines == null || sessionLines.Count == 0)
                return;
            SpeakerName = speaker;
            lines = sessionLines;
            index = 0;
            onComplete = completeAction;
            IsActive = true;
            SessionChanged?.Invoke();
        }

        // يعيد true وما زالت الجلسة حية؛ على آخر سطر: إكمال ثم نهاية وfalse.
        public bool Advance()
        {
            if (!IsActive)
                return false;

            index++;
            if (index < lines.Count)
            {
                SessionChanged?.Invoke();
                return true;
            }

            var complete = onComplete;
            End();
            complete?.Invoke();
            return false;
        }

        // إنهاء بلا فعل إكمال (الموت منتصف الحوار).
        public void Abort()
        {
            if (!IsActive)
                return;
            End();
        }

        private void End()
        {
            IsActive = false;
            lines = null;
            onComplete = null;
            SessionChanged?.Invoke();
        }
    }
}
