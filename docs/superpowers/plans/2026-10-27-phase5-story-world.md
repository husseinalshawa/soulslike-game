# خطة تنفيذ المرحلة 5 — القصة والعالم

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** ثلاث شخصيات NPC بحوار مبهم يتقدم بأعلام قصة، الكنيسة الغارقة بمستنقع
يبطئ الحركة وأعداء جدد وموقع راحة رابع، وخاتمة مذبح تغيّر لون العالم.

**Architecture:** كل قاعدة لعب منطق C#‎ صافٍ (StoryState، NpcQuestline،
DialogueRunner، KeyItemInventory، MarshZone، ThemeShift، AltarLogic) بأغلفة
MonoBehaviour رفيعة (NpcAvatar، KeyItemPickup) — نفس عقد المراحل 1-4.
المحتوى (حوارات وقطع) في `StoryLibrary` الثابتة المختبرة مثل `GearLibrary`.

**Tech Stack:** Unity 6 (بناء برمجي عبر GameBootstrap) + LocalTests (dotnet 8 + NUnit + UnityShim).

**Spec:** `docs/superpowers/specs/2026-10-27-phase5-story-world-design.md`

## Global Constraints

- لا ملفات ‎.unity/.prefab/.meta يدوية — كل شيء يُبنى في `GameBootstrap`
  بنمط «كائن معطّل → اربط الحقول → فعّل».
- `./scripts/run-tests.sh` أخضر بعد كل مهمة (119 اختباراً قائماً + الجديد).
- الشيم لا يشغّل `Update` ولا `OnEnable` — الاختبارات تستدعي المنطق مباشرة
  وتسجل في السجلات الساكنة يدوياً عند الحاجة.
- نصوص اللعبة الظاهرة بالإنجليزية (كاتفاق YOU DIED/Toast)؛ أسطر الحوار
  **حرفياً من المواصفة §4** وأوصاف القطع من §5.
- أعلام القصة لا تُنزَل أبداً؛ قطع القصة لا تُسقط بالموت.
- الأرقام المثبتة: مركز الكنيسة (120, 80)، نصف قطر المستنقع 35، عمقه 1.2،
  إبطاء الماء ×0.65، مدى التفاعل مع الشخصيات والمذبح 2.5م، مكافآت 500/3000
  روح، مرتّلون ×3 (صحة 200، ضرر 30، poise 40، 150 روحاً، windup 0.8)،
  تحول اللون 8 ثوانٍ، المواقع تصبح 4 والأعداء 11.

## Review Focus

1. **الموت منتصف حوار** — يجب إنهاء الجلسة بلا تنفيذ فعل الإكمال وبلا بقاء
   `InputLocked` بعد الإحياء. (اختباره في المهمة 7.)
2. **فعل الإكمال مرة واحدة** — إعادة محادثة مارا بعد تسليم الجرس لا تمنح
   500 روح ثانية. (المهمة 3.)
3. **المذبح الفارغ والمزدوج** — بلا رماد = صامت؛ بعد الوضع = لا 3000 ثانية.
   (المهمة 6.)
4. **تصادم أولوية E** — شخصية واقفة قرب موقع راحة: الحديث يجب ألا يُجلِس
   أيضاً في نفس الضغطة. (المهمة 7.)
5. **الراحة/الإحياء لا يرجعان القصة** — بعد `BellReturned` ثم موت وراحة،
   مرحلة مارا تبقى 3 والجرس لا يعود. (المهمة 8 — سيناريو كامل.)

---

### Task 1: StoryState + KeyItemInventory

**Files:**
- Create: `Assets/_Game/Scripts/Story/StoryState.cs`
- Create: `Assets/_Game/Scripts/Story/KeyItemInventory.cs`
- Test: `Assets/_Game/Tests/EditMode/StoryStateTests.cs`

**Interfaces:**
- Produces:
  - `enum StoryFlag { CaveDiscovered, ChapelDiscovered, BossDefeated, BellFound, BellReturned, AshCarried, AshPlaced }` (namespace `SoulsGame.Story`)
  - `class StoryState { bool IsSet(StoryFlag); void Set(StoryFlag); event Action<StoryFlag> FlagChanged; }` — `Set` لعلمٍ مرفوع لا يطلق الحدث ثانية.
  - `class KeyItemInventory { bool Has(string name); void Add(string name); bool Remove(string name); int Count { get; } }` — `Add` لمكرر لا يضيف؛ `Remove` لغائب يعيد false.

- [ ] **Step 1: اختبارات فاشلة** — `StoryStateTests`:
  - `Flags_StartUnset_SetRaisesEventOnce`: علم غير مرفوع؛ `Set` يرفع ويطلق الحدث؛ `Set` ثانية لا تطلقه.
  - `KeyItems_AddHasRemove_NoDuplicates`: Add/Has/Remove، مكرر لا يضيف (Count يبقى 1)، إزالة غائب false.
- [ ] **Step 2: شغّل وتأكد من الفشل** — `./scripts/run-tests.sh` → فشل ترجمة (الأنواع غير معرفة).
- [ ] **Step 3: نفّذ** الصنفين كما في Produces (قاموس/HashSet داخلياً).
- [ ] **Step 4: شغّل وتأكد من النجاح** — 121/121.
- [ ] **Step 5: Commit** — `feat: story flags and key item inventory`

### Task 2: DialogueRunner

**Files:**
- Create: `Assets/_Game/Scripts/Story/DialogueRunner.cs`
- Test: `Assets/_Game/Tests/EditMode/DialogueRunnerTests.cs`

**Interfaces:**
- Produces: `class DialogueRunner { bool IsActive; string SpeakerName; string CurrentLine; void Begin(string speaker, IReadOnlyList<string> lines, Action onComplete); bool Advance(); void Abort(); event Action SessionChanged; }`
  - `Begin` بقائمة فارغة لا يفعل شيئاً. `Advance` يعيد true وما زالت الجلسة حية؛ على آخر سطر: يستدعي `onComplete` ثم ينهي ويعيد false. `Abort` ينهي **بلا** `onComplete`. `SessionChanged` يطلق عند البدء وكل تقدم ونهاية (الواجهة تستهلكه).

- [ ] **Step 1: اختبارات فاشلة** — `DialogueRunnerTests`:
  - `Begin_ShowsFirstLine_AdvanceWalksLines`: سطران؛ بعد Begin السطر الأول؛ Advance → الثاني وtrue؛ Advance → false وIsActive=false وonComplete استُدعي مرة.
  - `Abort_EndsWithoutCompletion`: Begin ثم Abort → IsActive=false وonComplete لم يُستدعَ.
  - `Begin_EmptyLines_StaysInactive`.
- [ ] **Step 2: شغّل — فشل ترجمة.**
- [ ] **Step 3: نفّذ الصنف.**
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: dialogue runner`

### Task 3: NpcQuestline + StoryLibrary (المحتوى)

**Files:**
- Create: `Assets/_Game/Scripts/Story/NpcQuestline.cs`
- Create: `Assets/_Game/Scripts/Story/StoryLibrary.cs`
- Test: `Assets/_Game/Tests/EditMode/QuestlineTests.cs`

**Interfaces:**
- Consumes: StoryState/StoryFlag/KeyItemInventory (مهمة 1)، `SoulsWallet` القائمة (`Add(int)`).
- Produces:
  - `class QuestStage { Func<StoryState, KeyItemInventory, bool> Condition; string[] Lines; Action<StoryState, KeyItemInventory, SoulsWallet> OnComplete; bool Completed; }`
  - `class NpcQuestline { string NpcName; List<QuestStage> Stages; QuestStage ActiveStage(StoryState, KeyItemInventory); void CompleteStage(QuestStage, StoryState, KeyItemInventory, SoulsWallet); }`
    - `ActiveStage` = **آخر** مرحلة شرطها متحقق (المرحلة 0 شرطها null = دائماً).
    - `CompleteStage` يستدعي `OnComplete` فقط إن لم تكن `Completed` ثم يعلّمها.
  - `static class StoryLibrary { NpcQuestline Mara(); NpcQuestline Valion(); NpcQuestline Sila(); const string RustedBell = "Rusted Bell"; const string WardensAshes = "Warden's Ashes"; const string RustedBellDescription = …; const string WardensAshesDescription = …; }` — الأسطر والشروط والأفعال **حرفياً من جدولي المواصفة §4-§5**: مارا 5 مراحل (مرحلة 2: شرط `BellFound` وHas(bell)؛ الفعل يزيل الجرس +500 روح + يرفع `BellReturned`)، فاليون 4 (مرحلة 1: شرط `BossDefeated`؛ الفعل يضيف الرماد + يرفع `AshCarried`)، سيلا 4.

- [ ] **Step 1: اختبارات فاشلة** — `QuestlineTests`:
  - `ActiveStage_IsLastMatching`: حالة فارغة → مرحلة 0 لمارا؛ ارفع `CaveDiscovered` → مرحلة 1؛ أضف الجرس وارفع `BellFound` → مرحلة 2.
  - `CompleteStage_FiresOnce`: أكمل مرحلة مارا 2 مرتين → المحفظة +500 مرة واحدة، الجرس أُزيل، `BellReturned` مرفوع، و`ActiveStage` صارت 3.
  - `Valion_GivesAshes_AfterBossDefeated`: قبل العلم مرحلة 0؛ بعده مرحلة 1؛ إكمالها يضيف `WardensAshes` ويرفع `AshCarried`.
  - `Sila_FinalStage_RequiresAshPlaced`.
  - `FullScenario_AllThreeLinesProgress`: سيناريو القصة كاملاً بالترتيب (اكتشاف كهف → جرس → تسليم → بوس → رماد → وضع) مع فحص مرحلة الثلاثة عند كل خطوة.
- [ ] **Step 2: شغّل — فشل ترجمة.**
- [ ] **Step 3: نفّذ** الأصناف والمحتوى (الأسطر من المواصفة حرفياً).
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: questlines and story content library`

### Task 4: MarshZone + إبطاء الحركة + ThemeShift

**Files:**
- Create: `Assets/_Game/Scripts/World/MarshZone.cs`
- Create: `Assets/_Game/Scripts/World/ThemeShift.cs`
- Modify: `Assets/_Game/Scripts/Player/PlayerMotor.cs` (خاصية مضاعف سرعة الأرضية)
- Modify: `Assets/_Game/Scripts/Player/PlayerAvatar.cs` (حقل `marsh` + تغذية المضاعف في Update)
- Test: `Assets/_Game/Tests/EditMode/MarshThemeTests.cs`

**Interfaces:**
- Produces:
  - `class MarshZone { MarshZone(Vector3 center, float radius); bool Contains(Vector3 p); const float SpeedMultiplier = 0.65f; }` — مقارنة أفقية (xz) فقط.
  - `PlayerMotor.TerrainSpeedMultiplier { get; set; } = 1f` — يضرب سرعات Locomotion/Block (مشي/ركض/عدوّ) **ولا يمس التفادي ولا اندفاعة المهارة**.
  - `PlayerAvatar.marsh` (حقل `MarshZone` عام يسنده البناء).
  - `class ThemeShift { ThemeShift(WorldTheme from, WorldTheme to, float duration); void Tick(float dt); float T { get; } bool Done { get; } WorldTheme Blend(WorldTheme into); }` — `Blend` يكتب lerp كل حقول ألوان `WorldTheme` عند `T` في نسخة عمل ويعيدها.

- [ ] **Step 1: اختبارات فاشلة** — `MarshThemeTests`:
  - `MarshZone_ContainsByPlanarDistance`: داخل عند 34م أفقياً ولو اختلف y؛ خارج عند 36م.
  - `ThemeShift_LerpsAndFinishes`: بين سمتين بلون سماء أسود→أبيض، مدة 8؛ بعد Tick(4) T≈0.5 وBlend يعطي رمادياً (0.5)؛ بعد Tick(4) Done وT=1.
- [ ] **Step 2: شغّل — فشل ترجمة.**
- [ ] **Step 3: نفّذ** الصنفين وعدّل Motor/Avatar (المضاعف في حالة Locomotion/Block فقط).
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: marsh slow zone and theme shift`

### Task 5: عالم الكنيسة — heightmap/scatter/builder

**Files:**
- Modify: `Assets/_Game/Scripts/World/WorldHeightmap.cs` (منخفض المستنقع + ثوابته)
- Modify: `Assets/_Game/Scripts/World/WorldScatter.cs` (إقصاء المستنقع)
- Modify: `Assets/_Game/Scripts/World/WorldBuilder.cs` (`BuildChapel` + سطح الماء)
- Test: `Assets/_Game/Tests/EditMode/ChapelWorldTests.cs`

**Interfaces:**
- Produces:
  - `WorldHeightmap.MarshCenter = new Vector3(120f, 0f, 80f)`، `MarshRadius = 35f`، `MarshDepth = 1.2f` (ثوابت عامة) — `GetHeight` ينخفض تدريجياً (smoothstep) حتى −1.2 في المركز.
  - `WorldBuilder.BuildChapel(Transform parent, WorldTheme theme)` يعيد `ChapelBuildResult { Vector3 AltarPosition; GameObject WaterPlane; }` — أرضية حجرية، جدران متهدمة بفتحات، صفا أعمدة، كتلة مذبح عند (120, _, 90)، مستوى ماء شفاف عند `MarshWaterY`.
  - `WorldHeightmap.MarshWaterY` (ثابت محسوب: ارتفاع حافة المستنقع − 0.3).
  - `WorldScatter` يقصي دائرة المستنقع (نصف قطر + 4م احتياط).

- [ ] **Step 1: اختبارات فاشلة** — `ChapelWorldTests`:
  - `Marsh_DepressesHeight_SmoothlyToDepth`: ارتفاع المركز ≈ ارتفاع ما قبل المستنقع − 1.2 (قارن بنقطة بعيدة مماثلة النمط عبر إعادة الحساب بدون التعديل غير ممكن — بدلاً: ارتفاع المركز أقل من ارتفاع نقطة على الحافة بـ ≥1.0، والحافة (نصف قطر+1م) غير متأثرة مقارنة بنقطة أبعد قليلاً بفرق < 0.05).
  - `Marsh_WaterAboveFloor_BelowRim`: `MarshWaterY` فوق ارتفاع المركز وتحت ارتفاع الحافة.
  - `Scatter_ExcludesMarshCircle`: لا عنصر نثر ضمن نصف القطر + 4.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ** (نمط المنخفض كنمط منصة الحصن المعكوس؛ `BuildChapel` بنمط بناء الحصن).
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: sunken chapel terrain, water and structure`

### Task 6: NpcAvatar + KeyItemPickup + AltarLogic

**Files:**
- Create: `Assets/_Game/Scripts/Story/NpcAvatar.cs`
- Create: `Assets/_Game/Scripts/Story/KeyItemPickup.cs`
- Create: `Assets/_Game/Scripts/Story/AltarLogic.cs`
- Modify: `Assets/_Game/Scripts/Player/PlayerAvatar.cs` (خاصية `KeyItems`)
- Test: `Assets/_Game/Tests/EditMode/NpcAltarTests.cs`

**Interfaces:**
- Consumes: StoryLibrary/NpcQuestline (مهمة 3)، StoryState/KeyItemInventory (مهمة 1).
- Produces:
  - `PlayerAvatar.KeyItems { get; } = new KeyItemInventory()`.
  - `class NpcAvatar : MonoBehaviour { NpcQuestline Questline; const float TalkRadius = 2.5f; bool InRange(Vector3 playerPos); }` — **لا** Hurtbox ولا تسجيل lock-on.
  - `class KeyItemPickup : MonoBehaviour { string itemName; string description; StoryFlag flagOnCollect; PlayerAvatar player; Toast toast; int TryCollect(Vector3 playerPos); }` — نصف قطر 1.5، مرة واحدة، حارس موت (نمط `GearPickup` بعد إصلاحه)، يرفع العلم ويعرض "{name} — {description}".
  - `static class AltarLogic { enum Result { Silent, Placed, AlreadyPlaced } Result TryPlace(StoryState state, KeyItemInventory items); }` — `Placed` فقط عند امتلاك `WardensAshes` و`AshPlaced` غير مرفوع: يزيل الرماد ويرفع العلم.

- [ ] **Step 1: اختبارات فاشلة** — `NpcAltarTests`:
  - `Altar_SilentWithoutAshes_PlacedOnce`: بلا رماد Silent؛ بعد إضافته Placed (العلم ارتفع والرماد أُزيل)؛ ثالثة AlreadyPlaced.
  - `KeyItemPickup_CollectsOnce_SetsFlag_DeadGuard`: التقاط يضيف ويرفع العلم مرة؛ ميّت لا يلتقط (عبر بناء bootstrap للاعب حقيقي).
  - `Npc_InRangeByDistance`: 2.4م true، 2.6م false.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ الأصناف الثلاثة والخاصية.**
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: npc avatar, key item pickup and altar logic`

### Task 7: تكامل GameLoop + لوحة الحوار

**Files:**
- Modify: `Assets/_Game/Scripts/Core/GameLoop.cs`
- Modify: `Assets/_Game/Scripts/UI/HudBuilder.cs` (لوحة حوار أسفل الشاشة)
- Create: `Assets/_Game/Scripts/UI/DialoguePanel.cs`
- Test: إضافات على `Assets/_Game/Tests/EditMode/NpcAltarTests.cs`

**Interfaces:**
- Consumes: DialogueRunner (مهمة 2)، NpcAvatar/AltarLogic (مهمة 6)، StoryState (مهمة 1).
- Produces:
  - `GameLoop.Story { get; } = new StoryState()` و`GameLoop.Dialogue { get; } = new DialogueRunner()`.
  - `GameLoop.npcs` (مصفوفة `NpcAvatar[]` يسندها البناء) و`GameLoop.altarPosition` (`Vector3?`) و`GameLoop.dialoguePanel`.
  - **أولوية `HandleInteract`** (ميت = لا شيء): حوار نشط → `Dialogue.Advance()`؛ قائمة مفتوحة → إغلاق؛ شخصية ضمن 2.5م → `StartDialogue(npc)`؛ المذبح ضمن 2.5م → `InteractAltar()`؛ موقع راحة → جلوس. **return بعد كل فرع.**
  - `StartDialogue`: يحل المرحلة النشطة، `Begin(name, lines, onComplete: CompleteStage(...))`، يرفع `InputLocked` و`AggroSuppressed`؛ نهاية الجلسة (طبيعية أو Abort) تنزلهما — **إلا إذا كانت قائمة الرفع فتحت** (لا تتقاطعان: الحوار لا يبدأ وقائمة مفتوحة أصلاً).
  - `InteractAltar`: حسب `AltarLogic.TryPlace`: Silent → toast "The altar is silent."؛ Placed → راية "THE MEADOW REMEMBERS" (مسار راية البوس) + 3000 روح + بدء `ThemeShift` (يملكه البناء — المهمة 8)؛ AlreadyPlaced → toast "Only a faint, warm hum remains."
  - ربط الأعلام: اكتشاف site 2 → `CaveDiscovered`، site 3 → `ChapelDiscovered`؛ `OnPlayerDied` يستدعي `Dialogue.Abort()` ويفك القفل.
  - `DialoguePanel { Show(string speaker, string line); Hide(); bool Visible; }` — تبنيها `HudBuilder` (شريط أسفل: اسم + سطر + "E ▸")، يحدّثها GameLoop عبر `SessionChanged`.

- [ ] **Step 1: اختبارات فاشلة** (lifecycle عبر bootstrap — البناء يُستكمل في المهمة 8، فاختبارات هذه المهمة تبني الكائنات يدوياً حيث يلزم أو تؤجَّل أسطرها للبناء إن تعذر؛ المضمون المطلوب):
  - `Interact_DialoguePriority_TalkDoesNotRest`: شخصية ضمن المدى وموقع راحة ضمن المدى → E يبدأ الحوار ولا يفتح قائمة الرفع ولا يملأ الصحة.
  - `Dialogue_LocksInput_AdvanceToEnd_Unlocks`: أثناء الجلسة `InputLocked`؛ التقدم حتى النهاية يفكه ويستدعي فعل المرحلة.
  - `Death_MidDialogue_AbortsWithoutCompletion`: موت أثناء الجلسة → الجلسة انتهت، لا مكافأة، وبعد `Respawn` الإدخال حر.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ** التكامل واللوحة.
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: dialogue sessions, interact priority and story flags in game loop`

### Task 8: البناء الكامل في GameBootstrap + السيناريو النهائي

**Files:**
- Modify: `Assets/_Game/Scripts/Core/GameBootstrap.cs`
- Modify: `Assets/_Game/Scripts/Editor/SoulsGameEditorMenu.cs` (أصل إحصاءات المرتّل)
- Test: إضافات على `Assets/_Game/Tests/EditMode/BootstrapLifecycleTests.cs` (أو ملف `Phase5LifecycleTests.cs`)

**Interfaces:**
- Consumes: كل ما سبق.
- Produces (في `GameBootstrap`):
  - حقل `chanterStats` (`CharacterStats`، fallback: صحة 200، poise 40، سرعة 2.5، مكافأة 150) وحقل `chanterWeapon` (fallback: ضرر 30، poise 30، timeline (0.8, 0.3, 1.2, 1.2)).
  - `BuildChapelArea()`: `WorldBuilder.BuildChapel` + 3 مرتّلين داخل المستنقع حول الكنيسة ((112,70)، (128,72)، (120,92)) → `AllEnemies` تصبح **11** + موقع راحة 4 عند `OnGround(95, 55)` (+1.1) + `MarshZone` للاعب + `ForbiddenZone` للحصان.
  - `BuildStory()`: الشخصيات الثلاث — مارا (4, −96) بلون دافئ، فاليون قرب بوابة الضباب (5, fy, 18) بلون فولاذي، سيلا (−40, −70) بلون شاحب مع شجرة ميتة؛ قطعة الجرس في غرفة كنز الكهف (`CaveRoomCenter + (−2, 0.6, −2)`)؛ المذبح؛ ربط `Boss.Defeated → Story.Set(BossDefeated)`؛ تمرير npcs/altar/panel للـ GameLoop.
  - تحول اللون: عند `AshPlaced` (عبر `FlagChanged`) يبدأ `ThemeShift` من السمة الحالية إلى نسخة أدفأ (سماء +دفء، عشب أكثر خضرة، ضباب أخف — نسخة مشتقة برمجياً)؛ `Update` في البناء يدفعه ويطبق `ApplyTheme(Blend(...))` حتى `Done`.
  - قائمة المحرر: `CreateAssetIfMissing<CharacterStats>("ChanterStats.asset", …)` + إسنادها في مشهد الساحة.

- [ ] **Step 1: اختبارات فاشلة**:
  - `Phase5_WorldBuilt`: 4 مواقع راحة، 11 عدواً، 3 شخصيات مبنية وغير مسجلة في lock-on، مذبح وجرس موجودان.
  - `Phase5_FullStoryScenario`: عبر الـ bootstrap: اكتشف الكهف (علم) → التقط الجرس → حاور مارا حتى النهاية (+500 وأخذ الجرس) → اقتل البوس (علم عبر الحدث) → حاور فاليون (رماد) → مذبح (3000 + علم + بدء التحول) → **موت وراحة بعدها: المراحل باقية والقطع لم تعد** (Review Focus 5).
  - `ThemeShift_RunsToCompletion_OnAshPlaced` (تحقق T يصل 1 عبر دفع يدوي).
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ البناء والربط.**
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: story, npcs and sunken chapel wired into the world`

### Task 9: التحقق النهائي والتوثيق

**Files:**
- Modify: `README.md` (سطر المراحل + ملاحظة الشخصيات)
- Modify: `docs/playtest-checklist.md` (قسم المرحلة 5)
- Test: الحزمة كاملة

- [ ] **Step 1:** حدّث README (المراحل المنجزة 1-5، لا أزرار جديدة — E نفسه للحوار) وقائمة الفحص (قسم: الشخصيات والحوار، الكنيسة والمستنقع، الخاتمة والتحول اللوني، سؤال المرحلة: «هل فهمت السر بلا سجل مهام؟»).
- [ ] **Step 2:** `./scripts/run-tests.sh` — الحزمة كاملة خضراء.
- [ ] **Step 3: Commit** — `docs: phase 5 checklist and readme`
