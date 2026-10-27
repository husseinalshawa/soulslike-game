using NUnit.Framework;
using SoulsGame.Progression;
using SoulsGame.Story;

namespace SoulsGame.Tests
{
    // خطوط الشخصيات الثلاث من StoryLibrary — المرحلة النشطة هي آخر مرحلة
    // شرطها متحقق، وفعل الإكمال يقع مرة واحدة فقط.
    public class QuestlineTests
    {
        private StoryState state;
        private KeyItemInventory items;
        private SoulsWallet wallet;

        [SetUp]
        public void SetUp()
        {
            state = new StoryState();
            items = new KeyItemInventory();
            wallet = new SoulsWallet();
        }

        [Test]
        public void ActiveStage_IsLastMatching()
        {
            var mara = StoryLibrary.Mara();
            Assert.AreSame(mara.Stages[0], mara.ActiveStage(state, items), "حالة فارغة = مرحلة 0");

            state.Set(StoryFlag.CaveDiscovered);
            Assert.AreSame(mara.Stages[1], mara.ActiveStage(state, items), "اكتشاف الكهف = مرحلة الجرس");

            items.Add(StoryLibrary.RustedBell);
            state.Set(StoryFlag.BellFound);
            Assert.AreSame(mara.Stages[2], mara.ActiveStage(state, items), "الجرس بالحوزة = مرحلة التسليم");
        }

        [Test]
        public void CompleteStage_FiresOnce()
        {
            var mara = StoryLibrary.Mara();
            state.Set(StoryFlag.CaveDiscovered);
            state.Set(StoryFlag.BellFound);
            items.Add(StoryLibrary.RustedBell);
            var stage = mara.ActiveStage(state, items);

            mara.CompleteStage(stage, state, items, wallet);
            Assert.AreEqual(500, wallet.Current, "تسليم الجرس = 500 روح");
            Assert.IsFalse(items.Has(StoryLibrary.RustedBell), "مارا أخذت الجرس");
            Assert.IsTrue(state.IsSet(StoryFlag.BellReturned));
            Assert.AreSame(mara.Stages[3], mara.ActiveStage(state, items), "تقدمت للمرحلة 3");

            mara.CompleteStage(stage, state, items, wallet);
            Assert.AreEqual(500, wallet.Current, "الفعل لا يقع مرتين");
        }

        [Test]
        public void Valion_GivesAshes_AfterBossDefeated()
        {
            var valion = StoryLibrary.Valion();
            Assert.AreSame(valion.Stages[0], valion.ActiveStage(state, items));

            state.Set(StoryFlag.BossDefeated);
            var stage = valion.ActiveStage(state, items);
            Assert.AreSame(valion.Stages[1], stage);

            valion.CompleteStage(stage, state, items, wallet);
            Assert.IsTrue(items.Has(StoryLibrary.WardensAshes), "فاليون أعطى الرماد");
            Assert.IsTrue(state.IsSet(StoryFlag.AshCarried));
        }

        [Test]
        public void Sila_FinalStage_RequiresAshPlaced()
        {
            var sila = StoryLibrary.Sila();
            state.Set(StoryFlag.BossDefeated);
            state.Set(StoryFlag.AshCarried);
            Assert.AreSame(sila.Stages[2], sila.ActiveStage(state, items));

            state.Set(StoryFlag.AshPlaced);
            Assert.AreSame(sila.Stages[3], sila.ActiveStage(state, items), "الخاتمة بعد وضع الرماد");
        }

        [Test]
        public void FullScenario_AllThreeLinesProgress()
        {
            var mara = StoryLibrary.Mara();
            var valion = StoryLibrary.Valion();
            var sila = StoryLibrary.Sila();

            // البداية: الجميع على مرحلة 0
            Assert.AreSame(mara.Stages[0], mara.ActiveStage(state, items));
            Assert.AreSame(valion.Stages[0], valion.ActiveStage(state, items));
            Assert.AreSame(sila.Stages[0], sila.ActiveStage(state, items));

            // اكتشاف الكهف → مارا تطلب الجرس
            state.Set(StoryFlag.CaveDiscovered);
            Assert.AreSame(mara.Stages[1], mara.ActiveStage(state, items));

            // التقاط الجرس وتسليمه
            items.Add(StoryLibrary.RustedBell);
            state.Set(StoryFlag.BellFound);
            mara.CompleteStage(mara.ActiveStage(state, items), state, items, wallet);
            Assert.AreEqual(500, wallet.Current);

            // قتل البوس → فاليون يعطي الرماد، سيلا تتحول
            state.Set(StoryFlag.BossDefeated);
            Assert.AreSame(sila.Stages[1], sila.ActiveStage(state, items));
            valion.CompleteStage(valion.ActiveStage(state, items), state, items, wallet);
            Assert.IsTrue(items.Has(StoryLibrary.WardensAshes));
            Assert.AreSame(valion.Stages[2], valion.ActiveStage(state, items), "فاليون يرشدك للكنيسة");
            Assert.AreSame(sila.Stages[2], sila.ActiveStage(state, items));

            // وضع الرماد (يمثله المذبح لاحقاً): الثلاثة على خاتمتهم
            items.Remove(StoryLibrary.WardensAshes);
            state.Set(StoryFlag.AshPlaced);
            Assert.AreSame(mara.Stages[4], mara.ActiveStage(state, items));
            Assert.AreSame(valion.Stages[3], valion.ActiveStage(state, items));
            Assert.AreSame(sila.Stages[3], sila.ActiveStage(state, items));
        }

        [Test]
        public void Library_LinesAndNames_Pinned()
        {
            Assert.AreEqual("Mara", StoryLibrary.Mara().NpcName);
            Assert.AreEqual("Ser Valion", StoryLibrary.Valion().NpcName);
            Assert.AreEqual("Sila", StoryLibrary.Sila().NpcName);
            Assert.AreEqual(5, StoryLibrary.Mara().Stages.Count);
            Assert.AreEqual(4, StoryLibrary.Valion().Stages.Count);
            Assert.AreEqual(4, StoryLibrary.Sila().Stages.Count);
            StringAssert.Contains("bells", StoryLibrary.Mara().Stages[0].Lines[0]);
            StringAssert.Contains("ashes", StoryLibrary.Valion().Stages[1].Lines[1]);
            StringAssert.Contains("meadow remembers", StoryLibrary.Sila().Stages[3].Lines[1]);
        }
    }
}
