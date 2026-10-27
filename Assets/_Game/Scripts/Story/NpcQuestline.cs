using System;
using System.Collections.Generic;
using SoulsGame.Progression;

namespace SoulsGame.Story
{
    // مرحلة حوار واحدة: شرط ظهورها (null = دائماً)، أسطرها، وفعل اختياري
    // يقع مرة واحدة عند بلوغ آخر سطر.
    public class QuestStage
    {
        public Func<StoryState, KeyItemInventory, bool> Condition;
        public string[] Lines;
        public Action<StoryState, KeyItemInventory, SoulsWallet> OnComplete;
        public bool Completed;
    }

    // خط مهام شخصية: المرحلة النشطة هي آخر مرحلة شرطها متحقق —
    // لا سجل مهام، تغيّر الكلام هو التقدم.
    public class NpcQuestline
    {
        public string NpcName;
        public List<QuestStage> Stages = new List<QuestStage>();

        public QuestStage ActiveStage(StoryState state, KeyItemInventory items)
        {
            QuestStage active = null;
            foreach (var stage in Stages)
                if (stage.Condition == null || stage.Condition(state, items))
                    active = stage;
            return active;
        }

        public void CompleteStage(QuestStage stage, StoryState state,
            KeyItemInventory items, SoulsWallet wallet)
        {
            if (stage == null || stage.Completed)
                return;
            stage.Completed = true;
            stage.OnComplete?.Invoke(state, items, wallet);
        }
    }
}
