# خطة تنفيذ المرحلة 3 — العالم المفتوح «المرج الذابل»

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** منطقة 400×400م بتضاريس حتمية وستايل المرج الذابل، تحتوي الحصن (محتوى المرحلتين 1-2) وكهفاً و3 مواقع راحة و8 جنود وحصان تنقل.

**Architecture:** كما السابق: منطق خالص مُختبر (WorldHeightmap, WorldScatter, MountSystem, leash) تغلفه طبقة بناء برمجي. GameBootstrap يصير منسقاً لـ WorldBuilder (تضاريس/نثر/كهف) وبناء الحصن الحالي بإزاحة هضبة. كل المواضع تُسقط على ارتفاع التضاريس.

**Tech Stack:** كما المرحلة 1 (+ Mesh API في الـ shim للترجمة).

**Spec:** `docs/superpowers/specs/2026-10-27-phase3-open-world-design.md`

## Global Constraints

- كل قيود المرحلتين 1-2 سارية (منطق خالص، لا YAML يدوي، أرقام في ScriptableObjects، suite أخضر لكل مهمة).
- حتمية مطلقة: لا `UnityEngine.Random` ولا `System.Random` بلا بذرة ثابتة — كل العشوائية من دوال حتمية على البذرة `12345`.
- أرقام مثبتة من المواصفة: منطقة ±200؛ ارتفاع [0,12]؛ هضبة الحصن نصف قطر 35 حول (0,0) مسطحة؛ حواف آخر 15م ترتفع؛ قطع شبكة 50م بدقة 2م؛ نثر 120 شجرة/80 صخرة/30 طللاً بقواعد الإقصاء (40م حصن، 6م مواقع/معسكرات/كهف، تباعد 4م)؛ حصان 16 م/ث، زر R/`<Gamepad>/select`؛ كهف عند الغرب (فم ≈ (−140, …))؛ كنز 800 روح؛ leash 25م؛ مواقع الراحة: 0 بوابة الحصن (مكتشف)، 1 عند (0,−100)، 2 فم الكهف.

## Review Focus

- حتمية الارتفاع عبر النسخ والاستدعاءات (نفس المدخل = نفس الناتج دائماً) — المهمة 1.
- رفض الاستدعاء أثناء Attack/Dodge/Stagger/Death والقائمة وداخل المناطق المحظورة — المهمة 3.
- النثر لا يسقط أبداً داخل الحصن أو فوق مواقع الراحة/المعسكرات — المهمة 2.
- الـ leash لا يرتد (hysteresis): ترك المطاردة فوق 25م يتبعه عودة للدورية ولا re-aggro إلا باكتشاف جديد — المهمة 4.
- كل كيان مبني يقف على ارتفاع التضاريس (y ≥ GetHeight في موضعه) — اختبار دورة الحياة في المهمتين 5-6.

---

### Task 1: WorldTheme + WorldHeightmap

**Files:**
- Create: `Assets/_Game/Scripts/Data/WorldTheme.cs`, `Assets/_Game/Scripts/World/WorldHeightmap.cs`
- Test: `Assets/_Game/Tests/EditMode/WorldHeightmapTests.cs`

**Interfaces:**
- Produces:
  - `WorldTheme : ScriptableObject` بحقول الألوان/الضباب/الشمس من المواصفة §3 بقيم المرج الذابل الافتراضية.
  - `static class WorldHeightmap` (namespace `SoulsGame.World`): ثوابت `HalfSize=200f, MaxHeight=12f, FortRadius=35f, FortPlateauHeight` (قيمة ثابتة يختارها المنفذ ضمن [2,6])، `EdgeBandWidth=15f`؛ `float GetHeight(float x, float z)` — موجات جيبية من البذرة + تسطيح ناعم للهضبات (الحصن (0,0) ق35؛ (0,−100) ق8؛ فم الكهف ق8) + رفع الحواف؛ `bool IsInsideWorld(x,z)`.

- [ ] **Step 1:** اختبارات فاشلة: `Height_Deterministic` (نفس النقاط مرتين = نفس القيم)، `Height_WithinBounds` (عينة شبكة 20×20: كل القيم في [0, 25] — الحواف قد تتجاوز 12 عمداً، الداخل ≤12)، `FortPlateau_Flat` (نقاط ضمن ق30 من المركز كلها بنفس ارتفاع الهضبة ±0.01)، `Edges_Rise` (ارتفاع عند |x|=198 أكبر من متوسط الداخل).
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: deterministic world heightmap and theme`

### Task 2: WorldScatter

**Files:**
- Create: `Assets/_Game/Scripts/World/WorldScatter.cs`
- Test: `Assets/_Game/Tests/EditMode/WorldScatterTests.cs`

**Interfaces:**
- Produces: `enum ScatterKind { DeadTree, Rock, Ruin }`؛ `struct ScatterItem { ScatterKind Kind; Vector3 Position; float Scale; float Rotation; }`؛ `static List<ScatterItem> Generate(IReadOnlyList<Vector3> exclusionCenters, IReadOnlyList<float> exclusionRadii)` — 120/80/30 عنصراً بمواضع حتمية (بذرة 12345)، y من `WorldHeightmap.GetHeight`، يحترم الإقصاءات والتباعد ≥4م وميولاً معقولة (يرفض موضعاً فرق ارتفاع جيرانه كبير — تقدير بسيط).

- [ ] **Step 1:** اختبارات فاشلة: `Generate_Deterministic` (استدعاءان = قوائم متطابقة)، `Generate_RespectsExclusions` (لا عنصر ضمن أي دائرة إقصاء)، `Generate_MinSpacing` (لا زوج أقرب من 4م)، `Generate_Counts` (120/80/30 أو أقل بقليل إن ضاقت المواضع — اقبل ≥90%).
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: deterministic world scatter`

### Task 3: MountSystem

**Files:**
- Create: `Assets/_Game/Scripts/World/MountSystem.cs`
- Test: `Assets/_Game/Tests/EditMode/MountSystemTests.cs`

**Interfaces:**
- Consumes: `PlayerStateId`.
- Produces: `struct ForbiddenZone { Vector3 Center; float Radius; }`؛ `class MountSystem`: `bool IsMounted`؛ `bool TrySummon(Vector3 playerPos, IReadOnlyList<ForbiddenZone> zones, PlayerStateId state, bool menuOpen)` — يرفض في المناطق المحظورة وأثناء Attack*/Dodge/Stagger/Death وأثناء القائمة، وينجح من Locomotion/Block → `IsMounted=true`؛ `void Dismount()`؛ `bool NotifyRiderHit()` — راكباً: ترجّل ويعيد true (الغلاف يطبق الضرر كاملاً).

- [ ] **Step 1:** اختبارات فاشلة: `Summon_FromLocomotion_Mounts`، `Summon_InForbiddenZone_Rejected`، `Summon_DuringCommitment_Rejected` (AttackLight وDodge وStagger)، `Summon_WhileMenuOpen_Rejected`، `Toggle_Dismounts`، `RiderHit_ForcesDismount`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: mount system logic`

### Task 4: Leash للأعداء

**Files:**
- Modify: `Assets/_Game/Scripts/Enemies/EnemyBrain.cs` (توقيع Tick يُحمَّل بوسيط جديد اختياري), `EnemyAvatar.cs`
- Test: `Assets/_Game/Tests/EditMode/EnemyBrainTests.cs` (إضافات)

**Interfaces:**
- Produces: `EnemyBrain.Tick(float dt, float distanceToPlayer, float angleToPlayerDegrees, float distanceFromHome = 0f)` — في Chase إذا `distanceFromHome > LeashRange (25f)` → حالة جديدة `Returning` (يُضاف للـ enum): يتجاهل اللاعب حتى `distanceFromHome < 2f` ثم Patrol (hysteresis — لا re-aggro أثناء العودة). `EnemyAvatar` يمرر المسافة من `spawnPosition` ويتحرك نحوها في Returning بسرعة الدورية.

- [ ] **Step 1:** اختبارات فاشلة: `Chase_BeyondLeash_Returns`، `Returning_IgnoresPlayer_UntilHome` (مسافة لاعب 1م أثناء العودة لا تعيد المطاردة؛ عند الوصول ثم اقتراب اللاعب → يطارد من جديد).
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS (كل القديم أخضر — الوسيط الافتراضي 0 لا يغير سلوك البوس/الاختبارات القائمة). **Step 5:** Commit: `feat: enemy leash with return hysteresis`

### Task 5: WorldBuilder — التضاريس والنثر والكهف والثيم

**Files:**
- Create: `Assets/_Game/Scripts/World/WorldBuilder.cs`
- Modify: `GameBootstrap.cs` (استدعاء WorldBuilder وإزالة أرضية الساحة القديمة)، `SoulsGameEditorMenu.cs` (أصل WorldTheme)
- Test: توسيع `BootstrapLifecycleTests.cs`
- Shim: Mesh/MeshFilter/MeshCollider stubs

**Interfaces:**
- Consumes: `WorldHeightmap`, `WorldScatter`, `WorldTheme`.
- Produces: `static class WorldBuilder`: `void Build(WorldTheme theme, IReadOnlyList<Vector3> exclusionCenters, IReadOnlyList<float> exclusionRadii)` — قطع التضاريس (Mesh + MeshCollider، 50م/دقة 2م)، تطبيق الثيم (شمس، ضباب، ambient، لون خلفية الكاميرا)، النثر (كائنات بدائية حسب ScatterKind)، الكهف (نفق صندوقي عند فم الكهف غرباً + غرفة بإضاءة معتمة)؛ `static Vector3 OnGround(float x, float z, float yOffset)` أداة إسقاط يستخدمها كل البناء.
- `GameBootstrap.Awake`: يبني العالم أولاً، ثم كل الكيانات عبر `OnGround`.

- [ ] **Step 1:** وسّع دورة الحياة: بعد البناء — `GameObject` باسم `Terrain_0_0` موجود (شبكة القطع بُنيت)، وموضع اللاعب `y ≥ WorldHeightmap.GetHeight(x,z)`. شغّل — FAIL.
- [ ] **Step 2:** نفّذ WorldBuilder + الـ shim (Mesh: vertices/triangles/colors/RecalculateNormals/RecalculateBounds؛ MeshFilter.mesh؛ MeshCollider.sharedMesh) + تعديل Bootstrap (الأرضية القديمة 40×40 تُستبدل بأرضيات الحصن فوق الهضبة).
- [ ] **Step 3:** شغّل — PASS. **Step 4:** Commit: `feat: procedural terrain, scatter, cave and withered-meadow theme`

### Task 6: إعادة تموضع الحصن + المحتوى (مواقع، معسكرات، كهف، كنز)

**Files:**
- Create: `Assets/_Game/Scripts/World/SoulsCache.cs`
- Modify: `GameBootstrap.cs`, `GameLoop.cs` (كنز لا يعود بالراحة)
- Test: توسيع `BootstrapLifecycleTests.cs`

**Interfaces:**
- Consumes: كل بناء المرحلتين 1-2، `GraceSystem`, `SoulsWallet`.
- Produces: الحصن على الهضبة (ارتفاع الهضبة يضاف لكل مواضع BuildArena/BuildBossArea/البوابة الجنوبية فتحة 6م في الجدار الجنوبي)؛ مواقع الراحة 3 (0 بوابة الحصن مكتشف/نقطة البداية؛ 1 عند (0,−100)؛ 2 فم الكهف)؛ معسكران (3 جنود + نار مخيم متوهجة لكل منهما) عند مواضع مثبتة في المرج؛ عدوان في غرفة الكهف؛ `SoulsCache : MonoBehaviour` (بلورة، لمس ضمن 1.5م يمنح 800 روح مرة واحدة — فحص في GameLoop.Update مثل البقعة)؛ مصفوفة الأعداء في GameLoop تشمل الثمانية.

- [ ] **Step 1:** وسّع دورة الحياة: `Loop.Graces.SiteCount == 3`؛ عدد الأعداء 8؛ الكنز يمنح 800 مرة واحدة (استدعاء منطق الالتقاط مرتين → 800 فقط). شغّل — FAIL.
- [ ] **Step 2:** نفّذ. **Step 3:** شغّل — PASS. **Step 4:** Commit: `feat: fort on plateau, camps, cave enemies and souls cache`

### Task 7: الحصان في المشهد — HorseAvatar والركوب

**Files:**
- Create: `Assets/_Game/Scripts/World/HorseAvatar.cs`
- Modify: `PlayerControls.cs` (أكشن Mount: `<Keyboard>/r` + `<Gamepad>/select`)، `PlayerAvatar.cs` (IsRiding يكبح القتال، تمرير الحركة للحصان)، `PlayerMotor.cs` (تجميد أثناء الركوب)، `LockOnSystem.cs` (فك القفل عند الركوب)، `GameBootstrap.cs`, `GameLoop.cs`
- Test: توسيع `BootstrapLifecycleTests.cs`

**Interfaces:**
- Consumes: `MountSystem`, `WorldBuilder.OnGround`, `Damageable` (ضربة الراكب).
- Produces: `HorseAvatar : MonoBehaviour` — كبسولة بنية ممدودة + CharacterController؛ `Mount(Transform rider)` يثبت اللاعب على السرج (parent)؛ `Dismount()` يعيده أرضاً بجانبه؛ حركة راكباً: إدخال Move يقود الحصان (16 م/ث، تسارع، دوران)، زر التفادي = قفزة؛ `PlayerAvatar.IsRiding` يرفض الضرب/الصد/التفادي-الأرضي ويفك القفل؛ `Damageable.Damaged` أثناء الركوب → `MountSystem.NotifyRiderHit()` → ترجّل؛ موت اللاعب → صرف الحصان (GameLoop.OnPlayerDied)؛ المناطق المحظورة: الحصن (مركز, 38) والكهف. يُعرَّض `PlayerAvatar.Mount : MountSystem` و`GameBootstrap.Horse : HorseAvatar`.

- [ ] **Step 1:** وسّع دورة الحياة: `bootstrap.Horse` ليس null ومخفي (غير مستدعى)؛ `Player.Mount.TrySummon(...)` من Locomotion خارج الحصن ينجح و`IsRiding == true`. شغّل — FAIL.
- [ ] **Step 2:** نفّذ. **Step 3:** شغّل — PASS. **Step 4:** Commit: `feat: summonable horse with riding traversal`

### Task 8: التحقق النهائي

**Files:**
- Modify: `docs/playtest-checklist.md`, `README.md`

- [ ] **Step 1:** suite كامل PASS. **Step 2:** قسم المرحلة 3 في قائمة الفحص (موحشة وجميلة؟ الحصان يستاهل؟ المسافات مناسبة؟ الأداء ثابت؟) + README (العالم والتحكم الجديد R). **Step 3:** مطابقة المواصفة بنداً بنداً. **Step 4:** Commit: `docs: phase 3 checklist and readme`
