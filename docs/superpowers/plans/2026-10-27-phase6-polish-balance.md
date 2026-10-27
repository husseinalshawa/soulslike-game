# خطة تنفيذ المرحلة 6 — الصقل والموازنة

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** تغذية راجعة قتالية (hit-stop/وميض/اهتزاز/أرقام/تلاشٍ)، محاكي موازنة
بحواجس حارسة وتقرير، تصفية 12 ثانوية مؤجلة، وشاشة عنوان بإعداد حساسية.

**Architecture:** منطق الإحساس صافٍ (HitStopLogic/ShakeLogic/FadeLogic/
TitleLogic) بأغلفة رفيعة توصله بالأحداث القائمة (Damaged/HitboxOpen)؛
المحاكي كود اختبارات فقط يدير العقول القائمة تكّة-تكّة؛ الثانويات تعديلات
موضعية على ملفات قائمة.

**Tech Stack:** Unity 6 (بناء برمجي) + LocalTests (dotnet 8 + NUnit + UnityShim).

**Spec:** `docs/superpowers/specs/2026-10-27-phase6-polish-balance-design.md`

## Global Constraints

- لا مشاهد يدوية؛ `./scripts/run-tests.sh` أخضر بعد كل مهمة (152 قائماً + الجديد).
- أرقام الإحساس حرفياً من جدول المواصفة §2 (60/90/50ms بمقياس 0.05؛ وميض 0.1ث؛
  اهتزازات 0.15/0.25 و0.35/0.4 و0.25/0.3؛ أرقام تصعد 1.2م في 0.8ث؛ تلاشٍ 0.6/0.5؛
  عنوان يتلاشى 0.8ث).
- حواجز الموازنة §3 الخمسة كاختبارات دائمة؛ أي تعديل رقم = أقل تدخل + تحديث
  اختباره المثبت + سطر في التقرير.
- اسم اللعبة: **THE WITHERED MEADOW**. الحساسية 0.5-2.0 خطوة 0.25 افتراضي 1.
- `ShakeLogic` حتمي (لا Random)؛ hit-stop يُدار بزمن غير مقيس ولا يجمد نفسه.

## Review Focus

1. **تكدس hit-stop** — ضربتان في إطار/طلب أثناء تجميد: الأطول يسود و`Time.timeScale`
   يعود 1 تماماً (لا slow-mo دائم). (اختباره في المهمة 1.)
2. **الوميض لا يعلق أبيض** — موت العدو منتصف الوميض: لون الموت يغلب ولا يرتد
   أبيض/baseTint فوقه. (المهمة 2.)
3. **تعويذة مخزنة بلا FP كافٍ** عند لحظة التنفيذ → FocusRejected بلا حالة عالقة.
   (المهمة 6.)
4. **E مزدوجة على شاشة العنوان** لا تبدأ مرتين ولا تكسر التلاشي؛ الحساسية
   مقصوصة [0.5, 2.0] دائماً. (المهمة 8 عبر TitleLogic في 1.)
5. **موت هدف القفل أو فكّه** يخفي شريط الهدف فوراً — لا شريط يتيم. (المهمة 7.)

---

### Task 1: منطق الإحساس الصافي + TitleLogic

**Files:**
- Create: `Assets/_Game/Scripts/Feel/HitStopLogic.cs`, `ShakeLogic.cs`, `FadeLogic.cs`
- Create: `Assets/_Game/Scripts/UI/TitleLogic.cs`
- Test: `Assets/_Game/Tests/EditMode/FeelLogicTests.cs`

**Interfaces (Produces):**
- `class HitStopLogic { const float StopScale = 0.05f; void Request(float seconds); void Tick(float unscaledDt); float CurrentScale { get; } bool Active { get; } }` — `Request` أثناء تجميد يمدد للأطول فقط.
- `class ShakeLogic { void Shake(float amplitude, float duration); Vector3 Tick(float dt); bool Active { get; } }` — إزاحة حتمية (جيبيات بترددين مختلفين × سعة متضائلة خطياً حتى صفر تماماً).
- `class FadeLogic { void FadeTo(float targetAlpha, float duration); void Tick(float dt); float Alpha { get; } bool Done { get; } }` — خطي.
- `class TitleLogic { const float Min = 0.5f, Max = 2.0f, Step = 0.25f; float Sensitivity { get; } bool Started { get; } void AdjustSensitivity(int direction); bool RequestStart(); }` — `RequestStart` يعيد true أول مرة فقط؛ `AdjustSensitivity` مقصوص.

- [ ] **Step 1: اختبارات فاشلة** — `FeelLogicTests`:
  - `HitStop_LongestWins_RestoresExactlyOne`: Request(0.06) ثم Request(0.09) → Active وScale=0.05؛ Tick(0.09) → Scale=1 تماماً وActive=false (Review Focus 1).
  - `Shake_DecaysToExactZero`: Shake(0.15, 0.25)؛ منتصف المدة إزاحة ≠ صفر وطولها ≤ السعة المتبقية؛ بعد المدة Tick يعيد Vector3.zero وActive=false.
  - `Fade_LinearAlpha`: FadeTo(1, 0.6) من 0؛ Tick(0.3) → Alpha≈0.5؛ Tick(0.3) → 1 وDone.
  - `Title_SensitivityClampAndSingleStart`: Adjust(+1)×8 → 2.0 لا أكثر؛ Adjust(−1)×12 → 0.5؛ RequestStart true ثم false (Review Focus 4).
- [ ] **Step 2: شغّل — فشل ترجمة.**
- [ ] **Step 3: نفّذ الأصناف الأربعة.**
- [ ] **Step 4: شغّل — أخضر (156+).**
- [ ] **Step 5: Commit** — `feat: pure feel logic (hit-stop, shake, fade, title)`

### Task 2: ربط الإحساس 1 — hit-stop ووميض الإصابة واهتزاز الكاميرا

**Files:**
- Create: `Assets/_Game/Scripts/Feel/HitFlash.cs` (مكوّن: `Begin(Color baseColor)`, `Tick(dt)`, يملكه الغلاف)
- Modify: `Assets/_Game/Scripts/Enemies/EnemyAvatar.cs`, `BossAvatar.cs` (وميض عند Damaged، يحترم لون الموت)
- Modify: `Assets/_Game/Scripts/Player/ThirdPersonCamera.cs` (`ShakeLogic Shake` عام + تطبيق الإزاحة بعد حساب الموضع)
- Modify: `Assets/_Game/Scripts/Core/GameBootstrap.cs` (`HitStopLogic HitStop` عام، `Update` يطبق `Time.timeScale` بـ`Time.unscaledDeltaTime`؛ الربط: Damaged على كل عدو/بوس → Request حسب حالة عقل اللاعب (AttackHeavy/Skill ⇒ 0.09 وإلا 0.06) + اهتزاز 0.15/0.25 للثقيلة؛ Damaged على اللاعب → 0.05؛ أرضية البوس تصيب → اهتزاز 0.35/0.4؛ كسر حرس اللاعب (Brain stagger من الصد — حدث StaminaRejected? لا: كسر الحرس = NotifyBlockedHit false → Stagger؛ اربط عبر Damageable.PoiseBroken للاعب أو حدث StateChanged→Stagger أثناء الصد) → اهتزاز 0.25/0.3
- Test: `Assets/_Game/Tests/EditMode/FeelWiringTests.cs`

**Interfaces:**
- Consumes: HitStopLogic/ShakeLogic (مهمة 1)، baseTint (قائم)، Damaged/PoiseBroken (قائمة).
- Produces: `GameBootstrap.HitStop` (`HitStopLogic`)، `ThirdPersonCamera.Shake` (`ShakeLogic`)، `HitFlash` على كل عدو (`enemy.GetComponent<HitFlash>()`).

- [ ] **Step 1: اختبارات فاشلة** — `FeelWiringTests`:
  - `EnemyHit_TriggersHitStop_TierByPlayerState`: bootstrap؛ ضرر على Enemy → `bootstrap.HitStop.Active`؛ (بعد انتهاءه بالتكّات) لاعب في AttackHeavy ثم ضرر → مدة أطول (Tick(0.07) ما زال Active).
  - `HitFlash_RestoresBaseTint_DeathWins`: فلاش يدوي على عدو عبر مكوّنه ثم Tick(0.1) → اللون = baseTint؛ فلاش ثم قتل العدو ثم Tick → اللون يبقى لون نوعه بعد ResetToSpawn/لا يرتد أبيض (Review Focus 2: اختبر أن Tick بعد الموت لا يكتب فوق لون الجثة).
  - `PlayerHit_RequestsStop50ms`: ضرر على اللاعب → Active وTick(0.04) Active ثم Tick(0.02) انتهى.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ** (HitFlash يحفظ لوناً أساسياً ويُعطَّل عند موت المالك — افحص `Damageable.Health.IsDead`).
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: hit-stop, hit flash and camera shake wired`

### Task 3: ربط الإحساس 2 — أرقام الضرر وتلاشي الموت/الإحياء

**Files:**
- Create: `Assets/_Game/Scripts/Feel/DamageNumber.cs` (مكوّن TextMesh يصعد 1.2م ويتلاشى 0.8ث ثم يفنى؛ منطق الموضع/الشفافية في دالة ساكنة نقية `Evaluate(float t) -> (float rise, float alpha)`)
- Create: `Assets/_Game/Scripts/UI/FadeOverlay.cs` (Image أسود ملء الشاشة فوق كل شيء، يلفه `FadeLogic`)
- Modify: `Assets/_Game/Scripts/Core/GameBootstrap.cs` (ربط Damaged للأعداء/البوس → spawn رقم بالقيمة المخصومة عند موضع الإصابة)
- Modify: `Assets/_Game/Scripts/UI/HudBuilder.cs` (بناء FadeOverlay)
- Modify: `Assets/_Game/Scripts/Core/GameLoop.cs` (الموت → FadeTo(1, 0.6) قبل YOU DIED؛ Respawn → FadeTo(0, 0.5))
- Test: إضافات `FeelWiringTests`

**Interfaces:**
- Consumes: FadeLogic (مهمة 1)، Damaged(amount) القائم (تحقق توقيعه — إن كان بلا قيمة استعمل فرق الصحة).
- Produces: `GameLoop.fadeOverlay` (`FadeOverlay { FadeLogic Fade; }`)، `DamageNumber.Evaluate`.

- [ ] **Step 1: اختبارات فاشلة**:
  - `DamageNumber_RiseAndAlphaByTime`: Evaluate(0) → (0, 1)؛ Evaluate(0.4) → (0.6, 0.5±0.01)؛ Evaluate(0.8+) → (1.2, 0).
  - `Death_FadesIn_RespawnFadesOut`: bootstrap بـfadeOverlay مبني يدوياً إن لزم؛ قتل اللاعب → Fade هدفه 1؛ Respawn → هدفه 0.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ.**
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `feat: floating damage numbers and death/respawn fade`

### Task 4: CombatSim والحواجز الخمسة

**Files:**
- Create: `Assets/_Game/Tests/EditMode/Sim/CombatSim.cs`
- Test: `Assets/_Game/Tests/EditMode/Sim/BalanceGuardrailTests.cs`

**Interfaces (Produces):**
- `static class CombatSim`:
  - `struct DuelResult { float Seconds; bool PlayerWon; int PlayerHitsTaken; int StaminaStalls; }`
  - `DuelResult PlayerVsEnemy(WeaponData weapon, ArmorData armor, CharacterStats enemyStats, WeaponData enemyWeapon)` — 60Hz؛ سياسة اللاعب: تفادٍ عند دخول العدو AttackWindup (بتأخير رد فعل 0.25ث)، هجوم خفيف في AttackRecovery إن كفت الستامينا، وإلا انتظار؛ ضربات الطرفين تلحق عند HitboxOpen ما لم يكن الخصم invulnerable (i-frames اللاعب من حدث InvulnerabilityChanged)؛ سقف 600ث.
  - `DuelResult PlayerVsBoss(WeaponData weapon, ArmorData armor, CharacterStats bossStats)` — نفس السياسة ضد BossBrain (تفادي الإعلانات، ضرب في Recovery).
  - `int HitsToDie(float enemyDamage, ArmorData armor, float maxHealth = 100f)` — حساب مباشر.
- الأسلحة/الدروع/الإحصاءات من `GearLibrary` وأرقام البناء القائمة.

- [ ] **Step 1: اختبارات الحواجز** (فشلها الأولي = غياب المحاكي):
  - `Guardrail1_EveryWeaponKillsSoldier_3to25s`: لكل أسلحة GearLibrary الخمسة ضد (150hp/30poise جندي، سلاحه 20dmg timeline (0.6,0.3,1.2)): `3 ≤ Seconds ≤ 25` وPlayerWon.
  - `Guardrail2_WeaponSpreadAtMost2_5x`: أبطأ/أسرع ≤ 2.5.
  - `Guardrail3_BossTTK_Longsword_45to240s`: ضد (1000hp/120poise) PlayerWon و45-240ث.
  - `Guardrail4_PlayerHitsToDie_Classic`: HitsToDie(20, Cloth)∈{5,6}؛ HitsToDie(30, Cloth)==4؛ HitsToDie(40, Cloth)==3.
  - `Guardrail5_PlateAddsAtLeast2Hits`: HitsToDie(20, Plate) − HitsToDie(20, Cloth) ≥ 2.
- [ ] **Step 2: شغّل — فشل (CombatSim غير موجود).**
- [ ] **Step 3: نفّذ CombatSim** (منطق صافٍ، حتمي، بلا GameObject).
- [ ] **Step 4: شغّل الحواجز.** حارس يفشل على **الأرقام** (لا على المحاكي) ⇒ عدّل الرقم بأقل تدخل (مواصفة §3) + حدّث اختباره المثبت (GearLibraryTests/DataDefaults) وسجّل القرار في السجل.
- [ ] **Step 5: شغّل الحزمة كاملة — أخضر. Commit** — `feat: combat simulator and balance guardrails`

### Task 5: تقرير الموازنة

**Files:**
- Create: `Assets/_Game/Tests/EditMode/Sim/BalanceReportDump.cs` (`[Test, Explicit]` يطبع الجداول)
- Create: `docs/balance/2026-10-27-balance-report.md`

- [ ] **Step 1:** اكتب `BalanceReport_Dump` يطبع: TTK كل سلاح ضد جندي/مرتّل/بوس، ضربات الموت لكل درع ضد 20/30/40، StaminaStalls، وجدول الاقتصاد الثابت (أرواح متاحة: 8 جنود×100 + 3 مرتّلين×150 + كنز 800 + جرس 500 + بوس 2000 + مذبح 3000 = **8750 لمسار واحد** مقابل تكاليف الرفع 100+125+150+... التراكمية).
- [ ] **Step 2:** شغّله بالفلتر: `dotnet test LocalTests/SoulsGame.LocalTests.csproj --filter BalanceReport_Dump -v n` واقرأ الجداول.
- [ ] **Step 3:** اكتب التقرير من المخرجات: الجداول + الأهداف + أي تعديلات المهمة 4 وأسبابها + توصيات لعب حقيقي.
- [ ] **Step 4:** الحزمة خضراء (الـ Explicit لا يعد). **Commit** — `docs: balance report from combat simulation`

### Task 6: الثانويات 1-7 (قتال وأنظمة)

**Files:**
- Modify: `PlayerAvatar.cs` (GearChanged→`Action<string>` برسائل "Equipped: X"؛ حفظ لون السلاح الأساسي عند التجهيز)، `GameBootstrap.cs` (توصيل GearChanged بالـ Toast)
- Modify: `PlayerBrain.cs` (تخزين Cast/Skill في BufferedAction مع حفظ التعويذة المطلوبة؛ CanEverAct يشمل Cast/Skill؛ نقطة الإلغاء من التفادي `DodgeIFrameEnd × dodgeDurationMultiplier`)
- Modify: `GearLibrary.cs` (اندفاعة الرمح 2.0→2.4) + `GearLibraryTests` المثبت
- Modify: `FocusPool.cs` (`TrySpend(amount <= 0) => true` بلا تصفير المهلة)
- Modify: `ProjectileAvatar.cs` (قائمة مرشحين static تعاد تدويرها)، `HudBuilder.cs` (سطر العتاد يُبنى عند التغير فقط — كاش آخر نص)
- Test: `Assets/_Game/Tests/EditMode/PolishMinorsTests.cs`

**Interfaces:**
- Produces: `PlayerAvatar.GearChanged` يصير `event Action<string>`.

- [ ] **Step 1: اختبارات فاشلة** — `PolishMinorsTests`:
  - `GearSwitch_RaisesMessage`: اشترك، بدّل درعاً عبر Acquire ثم cycle مباشر (أو استدعِ معالج التبديل العام الجديد إن فُصل) → رسالة تحوي الاسم. (إن بقي التبديل في lambda الإدخال: افصل `TrySwitchWeapon/Armor/Spell()` عامة يستدعيها الإدخال، واختبرها.)
  - `CastBufferedDuringAttack_FiresAfterRecovery`: هجوم خفيف ثم RequestCast أثناءه → عند العودة للمحايد يدخل Cast تلقائياً ويخصم FP حينها؛ وبلا FP كافٍ لحظة التنفيذ → FocusRejected بلا حالة عالقة (Review Focus 3).
  - `SkillBufferedDuringCast_Fires`.
  - `PlateDodge_CancelPointScalesWithArmor`: بمضاعف 1.3، هجوم عند 0.45ث من التفادي يُرفض/يُخزن، وعند 0.53 ينفذ؛ بمضاعف 1.0 ينفذ عند 0.41.
  - `Spear_SkillDash2_4`: GearLibrary.Spear().skill.dashDistance == 2.4.
  - `FocusPool_TrySpendZero_KeepsRegenTimer`: استهلك، انتظر حتى قبيل التجدد، TrySpend(0)، تكة صغيرة → التجدد بدأ.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ السبعة.**
- [ ] **Step 4: شغّل — أخضر (مع تحديث مثبتات الرمح).**
- [ ] **Step 5: Commit** — `fix: deferred minors — gear toast, action buffering, armor commitment, spear reach, fp guard, colors, allocs`

### Task 7: الثانويات 8-12 (عالم وواجهة)

**Files:**
- Modify: `GameLoop.cs` (فرعا NPC/المذبح يتخطيان `player.IsRiding`)
- Modify: `GameBootstrap.cs` (كاش Light/Camera عند StartThemeShift؛ فصل `public void TickThemeShift(float dt)` يستدعيه Update)، `WorldBuilder.cs` (overload `ApplyTheme(theme, Light sun)` أو ما يكافئه للكاش؛ الماء `CreatePrimitive(Cylinder)` بقطر 70)
- Modify: `HudBuilder.cs`/`EnemyHealthBar.cs` (الشريط يقرأ `lockOn.CurrentTarget` الحالي — يظهر لأي عدو مقفول، يختفي عند الفك/الموت، ويتجاهل BossAvatar)
- Test: إضافات `PolishMinorsTests` + تحديث `ChapelWorldTests` إن لزم (شكل الماء)

**Interfaces:**
- Consumes: `LockOnSystem.CurrentTarget` القائم، `ThemeShift` القائم.
- Produces: `GameBootstrap.TickThemeShift(float dt)`.

- [ ] **Step 1: اختبارات فاشلة**:
  - `NoDialogue_NoAltar_WhileRiding`: استدعِ الحصان، شخصية بالمدى → HandleInteract لا يفتح حواراً (وراكباً فوق المذبح بالرماد لا يضع).
  - `ThemeShift_TicksToCompletion`: ارفع AshPlaced ثم `TickThemeShift(0.5)` × 17 → `ActiveThemeShift == null`.
  - `MarshWater_IsCylinder`: `WaterPlane.name`/شكله أسطوانة (اختبر عبر مكوّن/اسم بدائي كما يتاح في الشيم).
  - `LockOnBar_FollowsTarget_HidesOnRelease`: قفل على عدو → الشريط Visible بهدفه؛ فك → مخفي (Review Focus 5).
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ الخمسة.**
- [ ] **Step 4: شغّل — أخضر.**
- [ ] **Step 5: Commit** — `fix: deferred minors — riding gates, theme tick, circular water, lock-on health bar`

### Task 8: شاشة العنوان + التوثيق والتحقق النهائي

**Files:**
- Create: `Assets/_Game/Scripts/UI/TitleScreen.cs` (مكوّن: غطاء أسود + "THE WITHERED MEADOW" + "Press E to begin" + سطر الحساسية؛ يلف `TitleLogic` و`FadeLogic` للتلاشي 0.8ث؛ يقرأ أسهم الكيبورد/dpad في Update)
- Modify: `HudBuilder.cs` (`BuildTitleScreen()`)، `GameBootstrap.cs` (بناؤها آخر Awake: InputLocked+AggroSuppressed حتى البدء)، `GameLoop.cs` (HandleInteract: شاشة العنوان مفتوحة → `BeginGame()` قبل كل الأولويات)، `ThirdPersonCamera.cs` (`SensitivityMultiplier` يضرب الماوس والعصا)
- Modify: `README.md` (الاسم + المرحلة 6)، `docs/playtest-checklist.md` (قسم المرحلة 6: الإحساس والعنوان وأسئلة الموازنة من التقرير)
- Test: إضافات `PolishMinorsTests` (أو ملف `TitleScreenTests`)

**Interfaces:**
- Consumes: TitleLogic/FadeLogic (مهمة 1).
- Produces: `GameLoop.titleScreen` (`TitleScreen { TitleLogic Logic; bool IsOpen; void BeginGame(); }`)، `ThirdPersonCamera.SensitivityMultiplier`.

- [ ] **Step 1: اختبارات فاشلة**:
  - `GameStarts_Locked_TitleOpen`: بعد البناء `titleScreen.IsOpen` و`InputLocked` و`AggroSuppressed`.
  - `Interact_BeginsGame_Once`: HandleInteract → بدأت (IsOpen=false بعد التلاشي أو فوراً منطقياً) والإدخال حر؛ E ثانية لا تكسر شيئاً وتتصرف بالأولويات العادية.
  - `Sensitivity_AppliesToCamera`: Adjust(+1) → `cameraRig.SensitivityMultiplier == 1.25`.
- [ ] **Step 2: شغّل — فشل.**
- [ ] **Step 3: نفّذ + حدّث الوثائق.**
- [ ] **Step 4: `./scripts/run-tests.sh` — الحزمة كاملة خضراء.**
- [ ] **Step 5: Commit** — `feat: title screen with camera sensitivity; phase 6 docs`
