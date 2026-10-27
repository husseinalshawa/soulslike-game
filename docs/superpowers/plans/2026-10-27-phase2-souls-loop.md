# خطة تنفيذ المرحلة 2 — حلقة السولز

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** حلقة سولز كاملة فوق المرحلة 1: أرواح تُكسب وتسقط وتُستعاد، موقعا راحة يعيدان الأعداء ويرفعان المستوى، وبوس «حارس الساحة» بغرفة وبوابة ضباب.

**Architecture:** نفس عمارة المرحلة 1: منطق خالص مُختبر (SoulsWallet, BloodstainLedger, PlayerProgression, GraceSystem, BossBrain) تغلفه طبقة MonoBehaviour رقيقة، والمشهد يُبنى برمجياً في GameBootstrap بنمط «معطّل → اربط → فعّل». البوس يعيد استخدام Damageable/Hitbox/Hurtbox كما هي.

**Tech Stack:** كما المرحلة 1 (Unity 6 + LocalTests dotnet/NUnit/shim).

**Spec:** `docs/superpowers/specs/2026-10-27-phase2-souls-loop-design.md`

## Global Constraints

- كل قيود المرحلة 1 سارية (asmdefs، namespace `SoulsGame`، منطق خالص قابل للاختبار، الأرقام في ScriptableObjects، بناء المشهد برمجياً، `./scripts/run-tests.sh` أخضر بعد كل مهمة).
- أعراف مثبتة من المواصفة: بقعة دم واحدة كحد أقصى والاستبدال يضيّع القديم؛ نصف قطر الاستعادة 1.5م؛ نصف قطر الاكتشاف/الجلوس 2م؛ الإحصائيات تبدأ 10؛ التكلفة `100 + 25×(Level-1)`؛ أرقام البوس وهجماته الثلاثة من جدول المواصفة؛ الراحة لا تعيد البوس المهزوم.
- نصوص الواجهة الجديدة إنجليزية أو أرقام فقط (قيد uGUI/العربية من المرحلة 1)؛ الرسائل ثنائية حيث تظهر نصاً كبيراً («سقط العدو العظيم / GREAT ENEMY FELLED» تُعرض "GREAT ENEMY FELLED" مع سطر عربي بخط النظام — خط النظام في Unity يعرض العربية غير موصولة، مقبول مؤقتاً).

## Review Focus

- موت اللاعب أثناء معركة البوس: البوابة تفتح، البوس يعود كامل الصحة لموقعه، والبقعة تبقى داخل الغرفة — منطق إعادة البوس يُختبر في المهمة 5 (`ResetFight`)، والربط في المهمة 8.
- رفع مستوى يرفع الحد الأقصى دون إنقاص الحالي: بعد الرفع عند الراحة اللاعب ممتلئ بالقيم الجديدة — يُختبر في المهمة 3 (القيم المشتقة) والربط في المهمة 7.
- الراحة أثناء بقعة دم قائمة: البقعة لا تُمس (الراحة لا تلغي الاستعادة) — اختبار في المهمة 4.
- قتل عدو أثناء وجود قائمة الرفع مفتوحة مستحيل (الأعداء أعيدوا) لكن أرواح البوس بعد النصر تصل والقائمة مغلقة — تسلسل النصر في المهمة 8 خطوة تحقق يدوية مذكورة في قائمة الفحص.
- ترنّح البوس المتتالي: بعد كل ترنح يصبح حد الـ poise ×1.5 لعشر ثوانٍ — يُختبر في المهمة 2 (PoiseMeter multiplier) والمهمة 5 (نافذة البوس الزمنية).

---

### Task 1: محفظة الأرواح — SoulsWallet + soulsReward

**Files:**
- Create: `Assets/_Game/Scripts/Progression/SoulsWallet.cs`
- Modify: `Assets/_Game/Scripts/Data/CharacterStats.cs` (حقل جديد)
- Test: `Assets/_Game/Tests/EditMode/SoulsWalletTests.cs`

**Interfaces:**
- Produces: `class SoulsWallet` (namespace `SoulsGame.Progression`): `int Current`, `void Add(int amount)`, `bool TrySpend(int amount)` (يرفض الناقص دون تغيير)، `int TakeAll()` (يعيد الرصيد ويصفّره). و`CharacterStats.soulsReward : int = 0`.

- [ ] **Step 1:** اختبارات فاشلة: `Add_Accumulates`، `TrySpend_ExactAmount_Succeeds`، `TrySpend_Insufficient_FailsAndKeeps`، `TakeAll_ReturnsBalanceAndZeroes`.
- [ ] **Step 2:** شغّل `./scripts/run-tests.sh` — المتوقع: فشل ترجمة (SoulsWallet غير موجود).
- [ ] **Step 3:** نفّذ `SoulsWallet` وأضف `public int soulsReward = 0;` إلى CharacterStats.
- [ ] **Step 4:** شغّل — PASS. **Step 5:** Commit: `feat: souls wallet and kill rewards field`

### Task 2: مضاعف عتبة التوازن — PoiseMeter threshold multiplier

**Files:**
- Modify: `Assets/_Game/Scripts/Combat/PoiseMeter.cs`
- Test: `Assets/_Game/Tests/EditMode/HealthPoiseTests.cs` (إضافة)

**Interfaces:**
- Produces: `PoiseMeter.SetThresholdMultiplier(float m)` — العتبة الفعلية = `max × m` (الافتراضي 1). تستهلكه المهمة 8 لمضاد سبام ترنح البوس.

- [ ] **Step 1:** اختبار فاشل `Poise_ThresholdMultiplier_RaisesBreakPoint`: عداد بـ max 40، مضاعف 1.5 → ضرر 45 لا يكسر، 60+ يكسر؛ إعادة المضاعف 1 تعيد السلوك.
- [ ] **Step 2:** شغّل — FAIL. **Step 3:** نفّذ. **Step 4:** شغّل — PASS. **Step 5:** Commit: `feat: poise threshold multiplier`

### Task 3: التقدم — ProgressionData + PlayerProgression

**Files:**
- Create: `Assets/_Game/Scripts/Data/ProgressionData.cs`, `Assets/_Game/Scripts/Progression/PlayerProgression.cs` (+ `StatId.cs`)
- Test: `Assets/_Game/Tests/EditMode/PlayerProgressionTests.cs`

**Interfaces:**
- Consumes: `SoulsWallet`, `CharacterStats`.
- Produces:
  - `enum StatId { Vigor, Endurance, Strength }` (namespace `SoulsGame.Progression`).
  - `ProgressionData : ScriptableObject` بحقول: `baseStatValue=10, vigorHpPerPoint=12, enduranceStaminaPerPoint=6, strengthDamagePerPoint=2, baseCost=100, costIncrement=25`.
  - `class PlayerProgression`: ctor `(ProgressionData data, CharacterStats baseStats)`؛ `int Level` (يبدأ 1)؛ `int GetStat(StatId)` (تبدأ 10)؛ `int NextLevelCost => baseCost + costIncrement*(Level-1)`؛ `bool TryLevelUp(StatId stat, SoulsWallet wallet)` (يصرف التكلفة، يرفع الإحصائية والمستوى)؛ مشتقات: `float MaxHealth` (= baseStats.maxHealth + نقاط الصلابة المشتراة×12)، `float MaxStamina` (جَلَد×6)، `float BonusDamage` (قوة×2).

- [ ] **Step 1:** اختبارات فاشلة: `Costs_FollowCurve` (100 ثم 125 ثم 150)، `LevelUp_SpendsAndRaisesDerived` (صلابة → MaxHealth 112 وLevel 2 والمحفظة خُصمت)، `LevelUp_InsufficientSouls_RejectedAndUnchanged`، `BonusDamage_FromStrength`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: player progression with soul-cost curve`

### Task 4: بقعة الدم ومواقع الراحة — BloodstainLedger + GraceSystem

**Files:**
- Create: `Assets/_Game/Scripts/Progression/BloodstainLedger.cs`, `Assets/_Game/Scripts/Progression/GraceSystem.cs`
- Test: `Assets/_Game/Tests/EditMode/BloodstainGraceTests.cs`

**Interfaces:**
- Produces:
  - `class BloodstainLedger`: `bool HasStain`, `Vector3 Position`, `int Amount`, `void Drop(Vector3 position, int amount)` (amount ≤ 0 → يمسح أي بقعة ولا ينشئ)، `int TryRecover(Vector3 playerPosition, float radius)` (ضمن النطاق: يعيد المبلغ ويمسح؛ وإلا 0).
  - `class GraceSystem`: `void AddSite(int id, Vector3 position, bool discovered)`؛ `int TryDiscover(Vector3 playerPos)` (ضمن 2م من موقع غير مكتشف → يكتشفه ويعيد id، وإلا -1)؛ `bool CanRest(Vector3 playerPos, out int siteId)` (ضمن 2م من مكتشف)؛ `void RestAt(int siteId)` يحدّث `Vector3 RespawnPosition` (ابتداءً: موضع الموقع 0).

- [ ] **Step 1:** اختبارات فاشلة: `Drop_ZeroAmount_NoStain`، `Drop_ReplacesOldStain_OldAmountLost`، `TryRecover_WithinRadius_ReturnsAndClears`، `TryRecover_TooFar_ReturnsZeroKeepsStain`، `Rest_DoesNotTouchStain` (سيناريو: بقعة قائمة + RestAt → HasStain لا يتغير — توثيق للعقد)، `Discover_WithinRange_Once`، `CanRest_RequiresDiscovered`، `RestAt_SetsRespawn`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ الكلاسين. **Step 4:** PASS. **Step 5:** Commit: `feat: bloodstain ledger and grace system`

### Task 5: عقل البوس — BossBrain

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/BossBrain.cs`
- Test: `Assets/_Game/Tests/EditMode/BossBrainTests.cs`

**Interfaces:**
- Produces (namespace `SoulsGame.Enemies`):
  - `enum BossAttack { Smash, Sweep, Slam }`, `enum BossStateId { Dormant, Approach, Windup, Active, Recovery, Stagger, Dead }`
  - جدول الهجمات داخلي مثبت من المواصفة: Smash (مدى ≤3، إعلان 0.9، نافذة ضرر واحدة 0.25 ث، استراحة 1.6)؛ Sweep (مدى ≤4، إعلان 0.7، نافذتان [0–0.2] و[0.5–0.7] ضمن طور Active بطول 0.9، استراحة 1.2)؛ Slam (يُفرض عندما `normalAttacksSinceSlam ≥ 3` والمسافة ≤2.5، إعلان 1.1، نافذة 0.2، استراحة 1.4، `IsRadial=true`).
  - `class BossBrain`: `void Activate()` (من Dormant إلى Approach — يبدأ المعركة)؛ `void Tick(float dt, float distanceToPlayer)`؛ `BossStateId CurrentState`؛ `BossAttack CurrentAttack`؛ أحداث `StateChanged`, `AttackChosen(BossAttack)`, `HitboxOpen(BossAttack)` (مرتان للكنس), `HitboxClose`؛ `void NotifyPoiseBroken()` (ترنح 0.8 + يغلق النافذة + يُعلم الغلاف ببدء نافذة المضاعف عبر حدث `StaggerEnded`)؛ `void NotifyDied()`؛ `void ResetFight()` (كل شيء إلى Dormant بعد موت اللاعب).
  - الاختيار في Approach عند بلوغ مدى هجوم: الأولوية Slam المفروضة ثم Smash ثم Sweep؛ خارج 4م يبقى Approach (الغلاف يمشي 3.5 م/ث).

- [ ] **Step 1:** اختبارات فاشلة: `Dormant_IgnoresTicks_UntilActivate`، `Approach_PicksSmashInCloseRange`، `Sweep_FiresTwoHitWindows`، `Slam_ForcedAfterThreeNormalAttacks_OnlyWhenClose` (3 هجمات ثم مسافة 3.5 → لا Slam؛ مسافة 2 → Slam)، `PoiseBreak_DuringActive_ClosesWindowAndStaggers`، `ResetFight_ReturnsToDormant`، `Dead_IsTerminal`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ (البنية الداخلية قرار المنفّذ؛ السلوك والتواقيت أعلاه ملزمة). **Step 4:** PASS. **Step 5:** Commit: `feat: boss brain with telegraphed attack table and forced slam`

### Task 6: ربط الاقتصاد — المكافآت والعداد والبقعة والإحياء عند الراحة

**Files:**
- Create: `Assets/_Game/Scripts/Progression/BloodstainMarker.cs` (التمثيل البصري)
- Modify: `PlayerAvatar.cs` (محفظة)، `GameLoop.cs`، `GameBootstrap.cs`، `HudBuilder.cs` (عداد الأرواح)
- Test: توسيع `BootstrapLifecycleTests.cs`

**Interfaces:**
- Consumes: `SoulsWallet`, `BloodstainLedger`, `GraceSystem`, `CharacterStats.soulsReward`.
- Produces: `PlayerAvatar.Wallet : SoulsWallet`؛ `GameLoop` يحمل `BloodstainLedger Ledger` و`GraceSystem Graces` ويُحيي عند `Graces.RespawnPosition`؛ قتل أي `EnemyAvatar`/بوس يستدعي `Wallet.Add(stats.soulsReward)` (اشتراك على حدث Died في البناء)؛ عداد HUD أسفل يمين بـ `tabular-nums`؛ `BloodstainMarker` عمود ضوء يُنشأ/يُنقل عند الإسقاط ويختفي عند الاستعادة (فحص مسافة كل إطار في GameLoop: `Ledger.TryRecover(playerPos, 1.5f)`).

- [ ] **Step 1:** وسّع اختبار دورة الحياة: بعد بناء Bootstrap — `Player.Wallet` ليس null، وعدو القتل يضيف 100 (استدعاء `Enemy.Damageable.Health.TakeDamage(1000)` ثم Assert على `Wallet.Current == 100`). شغّل — FAIL.
- [ ] **Step 2:** نفّذ الربط (المحفظة في PlayerAvatar.Awake؛ الاشتراكات في GameBootstrap؛ الموت في GameLoop: `Ledger.Drop(موضع اللاعب, Wallet.TakeAll())` قبل شاشة الموت؛ الإحياء من `Graces.RespawnPosition`).
- [ ] **Step 3:** شغّل `./scripts/run-tests.sh` — PASS. **Step 4:** Commit: `feat: souls economy wiring with bloodstain drop/recover`

### Task 7: ربط الراحة — مواقع في المشهد + قائمة رفع المستوى

**Files:**
- Create: `Assets/_Game/Scripts/Progression/GraceSiteMarker.cs`, `Assets/_Game/Scripts/UI/LevelUpMenu.cs`
- Modify: `PlayerControls.cs` (أكشن `Interact`: `<Keyboard>/e` + `<Gamepad>/buttonSouth`)، `PlayerAvatar.cs` (`bool InputLocked` يوقف أوامر القتال والحركة)، `GameBootstrap.cs`، `GameLoop.cs`, `HudBuilder.cs`
- Test: توسيع `BootstrapLifecycleTests.cs`

**Interfaces:**
- Consumes: `GraceSystem`, `PlayerProgression`, `SoulsWallet`, `EnemyAvatar.ResetToSpawn()`.
- Produces: موقعا راحة في البناء (id 0 عند البداية مكتشف، id 1 أمام غرفة البوس)؛ `GraceSiteMarker` (أسطوانة ذهبية خافتة تسطع عند الاكتشاف، مع نص إشعار "Grace discovered" يظهر ثانيتين عبر عنصر Toast بسيط يضاف إلى HudBuilder)؛ ضغط Interact ضمن موقع مكتشف → `GameLoop.RestAtGrace(siteId)`: ملء كامل + `ResetToSpawn` لكل الأعداء العاديين + تحديث نقطة الإحياء + فتح `LevelUpMenu`؛ القائمة (uGUI برمجية): الأرواح، المستوى، 3 صفوف إحصائية مع زر +، التكلفة، زر "Rise" للإغلاق؛ الرفع يستدعي `TryLevelUp` ثم يعيد بناء Health/Stamina pools بالقيم المشتقة ويملؤها؛ `BonusDamage` يضاف لحزم ضرر اللاعب في PlayerAvatar.
- ملاحظة بنية: إعادة بناء الـ pools عبر دالة جديدة `PlayerAvatar.ApplyProgression(PlayerProgression p)`.

- [ ] **Step 1:** وسّع اختبار دورة الحياة: الموقعان مبنيان (`bootstrap.GameLoop.Graces` فيه موقعان)، و`ApplyProgression` بعد رفع صلابة يجعل `Player.Damageable.Health.Max == 112`. شغّل — FAIL.
- [ ] **Step 2:** نفّذ (الترتيب: أكشن الإدخال، ApplyProgression، RestAtGrace، الماركر، القائمة، البناء).
- [ ] **Step 3:** شغّل — PASS. **Step 4:** Commit: `feat: grace sites with rest, enemy respawn and level-up menu`

### Task 8: البوس في المشهد — الغرفة والبوابة وBossAvatar وبار البوس

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/BossAvatar.cs`, `Assets/_Game/Scripts/UI/BossBar.cs`
- Modify: `GameBootstrap.cs` (الغرفة 20×20 شمال الساحة + فتحة في الجدار الشمالي + جدار ضباب trigger + **جندي عادي ثانٍ** قرب الفتحة بدورية خاصة — بند المواصفة 6)، `GameLoop.cs` (موت اللاعب أثناء المعركة → `BossAvatar.ResetFight()` وفتح البوابة)، `HudBuilder.cs`
- Test: توسيع `BootstrapLifecycleTests.cs`

**Interfaces:**
- Consumes: `BossBrain`, `Damageable`, `Hitbox`, `PoiseMeter.SetThresholdMultiplier`, `SoulsWallet`.
- Produces: `BossAvatar : MonoBehaviour, ITargetable` — stats أصل CharacterStats للبوس `(1000hp, poise 120, runSpeed 3.5, soulsReward 2000)`؛ يمشي نحو اللاعب في Approach؛ ينفذ نوافذ الهجمات عبر Hitbox السلاح، وSlam بفحص نصف قطر 4م حول البوس (تجاهل i-frames ممنوع — نفس `Damageable.ApplyDamage`)؛ بعد `StaggerEnded` يضبط `SetThresholdMultiplier(1.5f)` لعشر ثوانٍ ثم يعيد 1؛ دخول جدار الضباب: `Brain.Activate()` + قفل البوابة (حاجز collider) + إظهار `BossBar` (شريط عريض أسفل الوسط باسم "Arena Warden")؛ النصر (Died): +2000 للمحفظة، رسالة "GREAT ENEMY FELLED / سقط العدو العظيم" ثانيتين، فتح البوابة، `DefeatedForever = true` (الراحة تتخطاه، تُفحص في RestAtGrace).
- ضرر هجمات البوس: Smash `(40,40)`، Sweep `(25,25)` لكل نافذة، Slam `(35,30)`.

- [ ] **Step 1:** وسّع اختبار دورة الحياة: `bootstrap.Boss` ليس null وحالته `Dormant`، وقتل البوس (TakeDamage 2000 بعد Activate) يضيف 2000 للمحفظة. شغّل — FAIL.
- [ ] **Step 2:** نفّذ (BossAvatar، الغرفة والبوابة في البناء، BossBar، تكامل GameLoop).
- [ ] **Step 3:** شغّل `./scripts/run-tests.sh` — PASS (كل الاختبارات).
- [ ] **Step 4:** Commit: `feat: boss room, fog gate, boss avatar and victory flow`

### Task 9: التحقق النهائي

**Files:**
- Modify: `docs/playtest-checklist.md`, `README.md`

- [ ] **Step 1:** شغّل `./scripts/run-tests.sh` — كل الاختبارات PASS.
- [ ] **Step 2:** حدّث قائمة الفحص بقسم المرحلة 2 (الحلقة الكاملة، عدالة البوس، إحساس فقدان/استعادة الأرواح، تسلسل النصر والقائمة) وREADME (الأنظمة الجديدة والتحكم الجديد E/زر A).
- [ ] **Step 3:** راجع المواصفة بنداً بنداً وأصلح أي نقص. **Step 4:** Commit: `docs: phase 2 playtest checklist and readme`
