namespace SoulsGame.Story
{
    // مصدر محتوى القصة الواحد المختبر (مواصفة المرحلة 5 §4-§5) —
    // الأسطر حرفية من المواصفة، وكل شخصية تحمل ثلث السر ولا تقوله كاملاً.
    public static class StoryLibrary
    {
        public const string RustedBell = "Rusted Bell";
        public const string RustedBellDescription =
            "A handbell that no longer rings. Someone carried it a long way from the water.";
        public const string WardensAshes = "Warden's Ashes";
        public const string WardensAshesDescription =
            "Still warm. They smell of wet stone and old hymns.";

        public const int BellReward = 500;

        public static NpcQuestline Mara() => new NpcQuestline
        {
            NpcName = "Mara",
            Stages =
            {
                new QuestStage
                {
                    Lines = new[]
                    {
                        "Still peddling, still listening. The bells, traveler — do you not hear them ring beneath the ground?"
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.CaveDiscovered),
                    Lines = new[]
                    {
                        "You walked the dark under the rocks?",
                        "My rusted bell slipped from my pack there, years past. It no longer rings… yet I'd have it back."
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.BellFound) && i.Has(RustedBell),
                    Lines = new[]
                    {
                        "Ah… cold little thing.",
                        "It rang once for the chanters, before the water took their choir.",
                        "Keep walking, traveler. Some songs want finishing."
                    },
                    OnComplete = (s, i, w) =>
                    {
                        i.Remove(RustedBell);
                        w.Add(BellReward);
                        s.Set(StoryFlag.BellReturned);
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.BellReturned),
                    Lines = new[] { "The ringing is softer now. Or perhaps I am." }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.AshPlaced),
                    Lines = new[]
                    {
                        "There — you hear it too now, don't you? One last peal.",
                        "The meadow remembers."
                    }
                }
            }
        };

        public static NpcQuestline Valion() => new NpcQuestline
        {
            NpcName = "Ser Valion",
            Stages =
            {
                new QuestStage
                {
                    Lines = new[]
                    {
                        "Turn back. Beyond that fog stands what remains of a holy man.",
                        "I came to end him… my sword had other thoughts."
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.BossDefeated),
                    Lines = new[]
                    {
                        "So the Warden kneels at last. Do not celebrate — you have slain a cantor, not a beast.",
                        "Take his ashes. Return them to where he once sang.",
                        "I would carry them myself, had I the legs for marshes."
                    },
                    OnComplete = (s, i, w) =>
                    {
                        i.Add(WardensAshes);
                        s.Set(StoryFlag.AshCarried);
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.AshCarried),
                    Lines = new[]
                    {
                        "The chapel drowned north-east of the meadow.",
                        "Mind the water — it keeps what it catches."
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.AshPlaced),
                    Lines = new[]
                    {
                        "It is done, then. A knight who could not strike, saved by a stranger who could.",
                        "I can live with that. Barely."
                    }
                }
            }
        };

        public static NpcQuestline Sila() => new NpcQuestline
        {
            NpcName = "Sila",
            Stages =
            {
                new QuestStage
                {
                    Lines = new[]
                    {
                        "I see nothing, and so I see it all.",
                        "The meadow did not wither, child. It fell silent. There is a difference."
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.BossDefeated),
                    Lines = new[]
                    {
                        "The last voice is gone, and you silenced it. Cruel… or kind.",
                        "That depends on what you do with what he leaves behind."
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.AshCarried),
                    Lines = new[]
                    {
                        "Ash in your pocket, song in the drowned stone.",
                        "Go. Finish what the water interrupted."
                    }
                },
                new QuestStage
                {
                    Condition = (s, i) => s.IsSet(StoryFlag.AshPlaced),
                    Lines = new[]
                    {
                        "Listen… color. I told you I see it all.",
                        "The chanter rests, the choir is whole, and the meadow — the meadow remembers its song. As do I, at last."
                    }
                }
            }
        };
    }
}
