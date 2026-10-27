using NUnit.Framework;
using SoulsGame.Story;

namespace SoulsGame.Tests
{
    public class DialogueRunnerTests
    {
        [Test]
        public void Begin_ShowsFirstLine_AdvanceWalksLines()
        {
            var runner = new DialogueRunner();
            int completed = 0;
            int changed = 0;
            runner.SessionChanged += () => changed++;

            runner.Begin("Mara", new[] { "Line one.", "Line two." }, () => completed++);

            Assert.IsTrue(runner.IsActive);
            Assert.AreEqual("Mara", runner.SpeakerName);
            Assert.AreEqual("Line one.", runner.CurrentLine);
            Assert.AreEqual(1, changed, "البدء يطلق تغيّر الجلسة");

            Assert.IsTrue(runner.Advance(), "ما زالت الجلسة حية بعد السطر الأول");
            Assert.AreEqual("Line two.", runner.CurrentLine);
            Assert.AreEqual(0, completed, "لا إكمال قبل آخر سطر");

            Assert.IsFalse(runner.Advance(), "آخر سطر ينهي الجلسة");
            Assert.IsFalse(runner.IsActive);
            Assert.AreEqual(1, completed, "فعل الإكمال استُدعي مرة واحدة");
            Assert.AreEqual(3, changed, "تقدم + نهاية يطلقان الحدث");
        }

        [Test]
        public void Abort_EndsWithoutCompletion()
        {
            var runner = new DialogueRunner();
            int completed = 0;
            runner.Begin("Valion", new[] { "A line." }, () => completed++);

            runner.Abort();

            Assert.IsFalse(runner.IsActive);
            Assert.AreEqual(0, completed, "الإجهاض (موت) لا ينفذ فعل الإكمال");
        }

        [Test]
        public void Begin_EmptyLines_StaysInactive()
        {
            var runner = new DialogueRunner();
            runner.Begin("Sila", new string[0], null);
            Assert.IsFalse(runner.IsActive);
        }
    }
}
