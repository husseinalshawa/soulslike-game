# خطة تنفيذ المرحلتين 0 و1 — جوهر قتال السولز

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** مشروع Unity 6 + URP فيه ساحة اختبار رمادية بقتال سولز كامل: آلة حالات لاعب، ستامينا، hitbox/hurtbox، lock-on، عدو بذكاء اصطناعي، موت وإعادة.

**Architecture:** منطق اللعبة الخالص (ستامينا، صحة، poise، آلات الحالات، اختيار أهداف الـ lock-on) يُكتب ككلاسات C# عادية بدون MonoBehaviour وتُختبر بـ EditMode tests؛ طبقة MonoBehaviour رقيقة تربط هذا المنطق بالمشهد والفيزياء والأنيميشن. كل الأرقام في ScriptableObjects.

**Tech Stack:** Unity 6 LTS، URP، Input System (الجديد)، Cinemachine، Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-10-27-phase1-combat-core-design.md`

## Global Constraints

- Unity 6 (أحدث LTS متاح)، قالب URP. كل كودنا تحت `Assets/_Game/`، الأصول الخارجية تحت `Assets/ThirdParty/`.
- نظام الإدخال الجديد فقط (Input System package) — ممنوع `UnityEngine.Input` القديم. دعم كيبورد/ماوس ويد تحكم في نفس الـ Action Map.
- namespace جذري: `SoulsGame` (مثل `SoulsGame.Combat`). asmdef للـ runtime اسمه `SoulsGame`، وللاختبارات `SoulsGame.EditModeTests`.
- المنطق القابل للاختبار كلاسات C# خالصة؛ الـ MonoBehaviour يغلّفها فقط.
- كل الأرقام القابلة للموازنة في ScriptableObjects، لا ثوابت مدفونة في الكود.
- إعدادات المشروع: Version Control = Visible Meta Files، Asset Serialization = Force Text.
- **قرار موثَّق (انحراف صغير عن المواصفة):** نوافذ تفعيل الـ Hitbox تقودها بيانات `AttackTimeline` (مدد windup/active/recovery في WeaponData) بدل أحداث الأنيميشن مباشرة — لأن أصولنا الحالية placeholder بلا أنيميشنات مضمونة. أحداث الأنيميشن تستدعي نفس الدوال لاحقاً عندما تتوفر أنيميشنات حقيقية. النتيجة سلوكياً مطابقة للمواصفة.
- **قرار موثَّق 2:** الشخصيات كبسولات ملوّنة في هذه المرحلة (هذه البيئة السحابية بلا واجهة لتنزيل أصول Mixamo/Starter Assets). استبدالها بشخصية وأنيميشنات مجانية خطوة يدوية يقوم بها الشريك في Unity Editor على جهازه — كل المنطق جاهز لذلك (أحداث HitboxOpen/Close وStateChanged هي نقاط الربط بالأنيميشن).

## Review Focus

- استهلاك ستامينا يساوي الرصيد بالضبط (current == cost) يجب أن ينجح — اختباره في المهمة 3.
- ضرر على هدف ميت أو ضرر يُنزل الصحة تحت الصفر: الصحة تُثبَّت عند 0 وحدث الموت يُطلق مرة واحدة فقط — اختباره في المهمة 4.
- طلب lock-on بلا أي هدف صالح في المدى/الزاوية: لا شيء يحدث، بلا استثناء — اختباره في المهمة 8.
- تفادي مطلوب أثناء هجوم قبل نافذة الإلغاء: يُخزَّن 0.2 ثانية وينفَّذ عند النافذة، ويسقط إذا انتهت المهلة — اختباره في المهمة 6.
- عدو يُكسر توازنه أثناء هجومه: نافذة الـ hitbox تُلغى ولا يصيب أثناء الـ Stagger — اختباره في المهمة 10.

---

### Task 1: تأسيس مشروع Unity والبنية التحتية للاختبار

**Files:**
- Create: مشروع Unity في جذر المستودع (`Assets/`, `Packages/`, `ProjectSettings/`)
- Create: `Assets/_Game/{Scripts,Scenes,Prefabs,Data,Art}/`, `Assets/ThirdParty/`
- Create: `Assets/_Game/Scripts/SoulsGame.asmdef`, `Assets/_Game/Tests/EditMode/SoulsGame.EditModeTests.asmdef`
- Create: `scripts/run-tests.sh`, `.gitignore`

**Interfaces:**
- Produces: أمر اختبار موحد `./scripts/run-tests.sh` تعتمد عليه كل المهام التالية؛ asmdef `SoulsGame` الذي تضاف إليه كل السكربتات.

- [ ] **Step 1:** استدعِ مهارة `unity:unity-cli` لتثبيت Unity 6 LTS وإنشاء مشروع URP في `/home/user/soulslike-game` (المستودع موجود مسبقاً — المشروع يُنشأ في مكانه أو يُنقل إليه).
- [ ] **Step 2:** أضف حزمتي `com.unity.inputsystem` و`com.unity.cinemachine` إلى `Packages/manifest.json`، وفعّل Force Text + Visible Meta Files في `ProjectSettings/EditorSettings.asset`، وActive Input Handling = Input System Package.
- [ ] **Step 3:** أنشئ هيكل المجلدات والـ asmdef-ين (asmdef الاختبارات يشير إلى `SoulsGame` و`UnityEngine.TestRunner` و`UnityEditor.TestRunner`، Editor-only platform).
- [ ] **Step 4:** أنشئ `.gitignore` قياسي لـ Unity (Library/, Temp/, Logs/, obj/, UserSettings/, *.csproj, *.sln).
- [ ] **Step 5:** اكتب اختبار دخان `Assets/_Game/Tests/EditMode/SmokeTest.cs`:

```csharp
[Test] public void Sanity() => Assert.Pass();
```

- [ ] **Step 6:** اكتب `scripts/run-tests.sh` يشغّل Unity بوضع batch: `-batchmode -projectPath . -runTests -testPlatform EditMode -testResults /tmp/results.xml -logFile -` ثم يطبع ملخص النتائج من الـ XML ويعيد exit code غير صفري عند أي فشل (مسار Unity الفعلي حسب ما ثبّتته مهارة unity-cli).
- [ ] **Step 7:** شغّل `./scripts/run-tests.sh` — المتوقع: نجاح، اختبار واحد، صفر فشل.
- [ ] **Step 8:** Commit: `chore: bootstrap Unity 6 URP project with test harness`

### Task 2: بيانات الموازنة — WeaponData وCharacterStats

**Files:**
- Create: `Assets/_Game/Scripts/Data/WeaponData.cs`, `Assets/_Game/Scripts/Data/CharacterStats.cs`
- Create: `Assets/_Game/Data/Sword_Basic.asset`, `PlayerStats.asset`, `EnemyMelee Stats.asset` → اسمه `EnemyMeleeStats.asset`

**Interfaces:**
- Produces:
  - `WeaponData : ScriptableObject` بحقول: `lightDamage=25, lightPoiseDamage=15, lightStaminaCost=15, heavyDamage=50, heavyPoiseDamage=35, heavyStaminaCost=30, comboLength=3`، و`AttackTimeline light/heavy` حيث `AttackTimeline` struct `{float windup; float active; float recovery; float cancelWindowStart;}` — light: `(0.25, 0.20, 0.45, 0.55)`، heavy: `(0.60, 0.25, 0.80, 1.00)`.
  - `CharacterStats : ScriptableObject` بحقول: `maxHealth, maxStamina, maxPoise, poiseRegenPerSecond=10, staminaRegenPerSecond=25, staminaRegenDelay=1.0, blockingRegenMultiplier=0.33, walkSpeed=2, runSpeed=5, sprintSpeed=8, sprintStaminaPerSecond=10, dodgeStaminaCost=20`.
  - القيم: PlayerStats `(100, 100, 40)`، EnemyMeleeStats `(150, 100, 30)`.

- [ ] **Step 1:** اكتب الكلاسين مع `[CreateAssetMenu]` في namespace `SoulsGame.Data`.
- [ ] **Step 2:** أنشئ ملفات الـ .asset الثلاثة بالقيم المذكورة أعلاه (بسكربت editor مؤقت أو يدوياً بكتابة YAML — Force Text يسمح بذلك).
- [ ] **Step 3:** شغّل `./scripts/run-tests.sh` — المتوقع: المشروع يترجم بلا أخطاء والاختبارات تنجح.
- [ ] **Step 4:** Commit: `feat: add WeaponData and CharacterStats ScriptableObjects with initial balance`

### Task 3: منطق الستامينا — StaminaPool

**Files:**
- Create: `Assets/_Game/Scripts/Combat/StaminaPool.cs`
- Test: `Assets/_Game/Tests/EditMode/StaminaPoolTests.cs`

**Interfaces:**
- Produces: `class StaminaPool` (namespace `SoulsGame.Combat`, C# خالص):
  - `StaminaPool(float max, float regenPerSecond, float regenDelay, float blockingRegenMultiplier)`
  - `float Current { get; }`, `float Max { get; }`
  - `bool TryConsume(float amount)` — ينجح إذا `amount <= Current` (يشمل التساوي)، يخصم ويعيد ضبط مهلة التجدد.
  - `void Tick(float deltaTime, bool isBlocking, bool isSprinting)` — sprint يستهلك `sprintCost*dt` (يُمرر عبر `SetSprintCost(float)` أو ctor موسّع — قرار المنفّذ) ويعيد ضبط المهلة؛ بعد المهلة يتجدد بالمعدل، مضروباً بـ `blockingRegenMultiplier` أثناء الصد؛ يتثبّت عند Max.

- [ ] **Step 1:** اكتب الاختبارات الفاشلة:

```csharp
[Test] public void TryConsume_ExactAmount_Succeeds()      // 100/100 ثم TryConsume(100) == true و Current == 0
[Test] public void TryConsume_Insufficient_FailsAndKeeps() // Current لا يتغير
[Test] public void Regen_WaitsForDelay()                   // Tick(0.5) بعد استهلاك: لا تجدد؛ بعد 1.0 ثانية: يتجدد
[Test] public void Regen_SlowerWhileBlocking()             // معدل التجدد × 0.33
[Test] public void Regen_ClampsAtMax()
[Test] public void Sprint_DrainsAndResetsDelay()
```

- [ ] **Step 2:** شغّل `./scripts/run-tests.sh` — المتوقع: فشل الترجمة/الاختبارات (StaminaPool غير موجود).
- [ ] **Step 3:** نفّذ `StaminaPool` حتى تنجح الاختبارات.
- [ ] **Step 4:** شغّل `./scripts/run-tests.sh` — المتوقع: PASS.
- [ ] **Step 5:** Commit: `feat: stamina pool with delayed regen, blocking and sprint rules`

### Task 4: الصحة والتوازن وحزمة الضرر — HealthPool, PoiseMeter, DamagePacket

**Files:**
- Create: `Assets/_Game/Scripts/Combat/DamagePacket.cs`, `HealthPool.cs`, `PoiseMeter.cs`
- Test: `Assets/_Game/Tests/EditMode/HealthPoiseTests.cs`

**Interfaces:**
- Produces (namespace `SoulsGame.Combat`):
  - `readonly struct DamagePacket { float HealthDamage; float PoiseDamage; Vector3 SourcePosition; }` + ctor.
  - `class HealthPool`: `HealthPool(float max)`, `float Current`, `bool IsDead`, `event Action Died`, `void TakeDamage(float amount)` — يتثبّت عند 0، `Died` يُطلق مرة واحدة فقط، الضرر بعد الموت يُتجاهل.
  - `class PoiseMeter`: `PoiseMeter(float max, float regenPerSecond)`, `bool TakePoiseDamage(float)` — يعيد `true` عند الكسر (التراكم ≥ max) ويعيد الضبط، `void Tick(float dt)` يتعافى نحو الصفر.

- [ ] **Step 1:** اكتب الاختبارات الفاشلة:

```csharp
[Test] public void TakeDamage_ClampsAtZero_AndDiedFiresOnce() // ضرر 200 على 100: Current==0، Died مرة واحدة؛ ضرر إضافي لا يطلقها ثانية
[Test] public void Poise_BreaksAtThreshold_AndResets()        // 40 poise: ضربتان 15 لا تكسران، الثالثة تكسر وتعيد الضبط
[Test] public void Poise_RegensOverTime()
```

- [ ] **Step 2:** شغّل الاختبارات — المتوقع: FAIL.
- [ ] **Step 3:** نفّذ الكلاسات الثلاثة.
- [ ] **Step 4:** شغّل `./scripts/run-tests.sh` — المتوقع: PASS.
- [ ] **Step 5:** Commit: `feat: health, poise and damage packet primitives`

### Task 5: تسجيل الإصابات — HitRegistry وHitbox/Hurtbox وDamageable

**Files:**
- Create: `Assets/_Game/Scripts/Combat/HitRegistry.cs`, `Hitbox.cs`, `Hurtbox.cs`, `Damageable.cs`
- Test: `Assets/_Game/Tests/EditMode/HitRegistryTests.cs`

**Interfaces:**
- Consumes: `DamagePacket`, `HealthPool`, `PoiseMeter`, `CharacterStats`.
- Produces:
  - `class HitRegistry` (خالص): `void BeginSwing()`, `bool TryRegister(object target)` — `false` لهدف سبق تسجيله في نفس الـ swing.
  - `Damageable : MonoBehaviour` — يملك `HealthPool Health` و`PoiseMeter Poise` (يبنيهما من `CharacterStats stats` في Awake)، `bool IsInvulnerable { get; set; }` (للـ i-frames)، `void ApplyDamage(DamagePacket p)` يتجاهل الضرر أثناء المناعة، وأحداث `event Action<DamagePacket> Damaged`, `event Action PoiseBroken`, `event Action Died`.
  - `Hurtbox : MonoBehaviour` — Collider (isTrigger) يشير إلى `Damageable Owner`.
  - `Hitbox : MonoBehaviour` — Collider (isTrigger، معطّل افتراضياً) + `HitRegistry`؛ `void Activate(DamagePacket packet)` / `void Deactivate()`؛ عند `OnTriggerEnter` مع Hurtbox: `TryRegister(owner)` ثم `owner.ApplyDamage(packet)`؛ حقل `Damageable ignoreOwner` حتى لا يضرب الحامل نفسه.

- [ ] **Step 1:** اختبارات فاشلة لـ `HitRegistry`:

```csharp
[Test] public void SameTarget_OncePerSwing()   // TryRegister(a)==true ثم false؛ BeginSwing() ثم true
[Test] public void DifferentTargets_BothRegister()
```

- [ ] **Step 2:** شغّل — FAIL، ثم نفّذ `HitRegistry` — PASS.
- [ ] **Step 3:** نفّذ `Damageable` و`Hurtbox` و`Hitbox` (منطق المناعة والتجاهل كما في الواجهات أعلاه؛ طبقات الفيزياء: layer `Hitboxes` يتلامس مع `Hurtboxes` فقط في Physics settings).
- [ ] **Step 4:** شغّل `./scripts/run-tests.sh` — المتوقع: PASS وترجمة نظيفة.
- [ ] **Step 5:** Commit: `feat: hitbox/hurtbox pipeline with per-swing hit registry and i-frame gate`

### Task 6: آلة حالات اللاعب — PlayerBrain

**Files:**
- Create: `Assets/_Game/Scripts/Player/PlayerBrain.cs` (+ ملف `PlayerStateId.cs`)
- Test: `Assets/_Game/Tests/EditMode/PlayerBrainTests.cs`

**Interfaces:**
- Consumes: `StaminaPool`, `WeaponData` (الأرقام والـ timelines), `AttackTimeline`.
- Produces (namespace `SoulsGame.Player`) — كلاس خالص يقاد بالزمن عبر `Tick(float dt)`:
  - `enum PlayerStateId { Locomotion, Dodge, AttackLight, AttackHeavy, Block, Stagger, Death }`
  - `class PlayerBrain`: ctor `(StaminaPool stamina, WeaponData weapon)`.
  - أوامر إدخال تُعيد bool قبول فوري: `RequestDodge()`, `RequestLightAttack()`, `RequestHeavyAttack()`, `SetBlockHeld(bool)`.
  - أحداث يستهلكها الغلاف الـ MonoBehaviour: `event Action<PlayerStateId> StateChanged;`, `event Action<AttackType> HitboxOpen;`, `event Action HitboxClose;`, `event Action<bool> InvulnerabilityChanged;`, `event Action StaminaRejected;` (يُطلق عند رفض فعل لنقص الستامينا — يستهلكه الـ UI).
  - دوال إشعار خارجية: `NotifyDamaged(bool poiseBroken)` (poiseBroken → Stagger مدته 0.8 ثانية)، `NotifyDied()` → Death نهائية.
  - القواعد المثبتة: التفادي مدته 0.6 ثانية وi-frames من 0.1 إلى 0.4 ثانية (عبر `InvulnerabilityChanged`)؛ الهجوم يتبع `AttackTimeline` (hitbox يفتح بعد windup ويغلق بعد active؛ الإلغاء مسموح بعد `cancelWindowStart`)؛ كومبو الضربات الخفيفة حتى `comboLength`؛ **input buffer**: أمر مرفوض أثناء هجوم/تفادي يُخزَّن 0.2 ثانية وينفَّذ تلقائياً عند أول لحظة مسموحة، ويسقط بعدها؛ كل فعل يتحقق من الستامينا عبر `TryConsume` وإلا يُرفض؛ الصد أثناء `SetBlockHeld(true)` في Locomotion فقط؛ `NotifyBlockedHit(DamagePacket p)`: `TryConsume(p.HealthDamage)` نجح → لا ضرر، فشل → كسر حرس = Stagger + يُعيد `false` (الغلاف يمرر الضرر كاملاً).

- [ ] **Step 1:** اكتب الاختبارات الفاشلة:

```csharp
[Test] public void Dodge_ConsumesStamina_AndGrantsIFramesWindow()   // أحداث المناعة عند 0.1 و0.4 ث
[Test] public void Dodge_InsufficientStamina_Rejected()
[Test] public void Attack_LocksUntilCancelWindow()                   // RequestDodge أثناء windup يُرفض فوراً لكن يُخزَّن
[Test] public void BufferedDodge_FiresAtCancelWindow()
[Test] public void BufferedInput_ExpiresAfter200ms()
[Test] public void LightCombo_AdvancesToThreeThenEnds()
[Test] public void BlockedHit_InsufficientStamina_GuardBreaksToStagger()
[Test] public void Death_IsTerminal()                                // لا أمر يخرج من Death
```

- [ ] **Step 2:** شغّل — المتوقع: FAIL.
- [ ] **Step 3:** نفّذ `PlayerBrain` (تمثيل الحالات داخلياً قرار المنفّذ — switch أو كلاسات — المهم السلوك والأحداث أعلاه).
- [ ] **Step 4:** شغّل `./scripts/run-tests.sh` — المتوقع: PASS.
- [ ] **Step 5:** Commit: `feat: player state machine with commitment, buffering, i-frames and guard break`

### Task 7: الإدخال والحركة والمشهد — PlayerMotor وساحة الاختبار

**Files:**
- Create: `Assets/_Game/Input/PlayerControls.inputactions`
- Create: `Assets/_Game/Scripts/Player/PlayerMotor.cs`, `PlayerAvatar.cs`
- Create: `Assets/_Game/Scenes/Arena.unity`, `Assets/_Game/Prefabs/Player.prefab`

**Interfaces:**
- Consumes: `PlayerBrain`, `StaminaPool`, `CharacterStats`, `Damageable`.
- Produces:
  - `PlayerControls.inputactions` — Action Map واحد `Gameplay`: `Move (Vector2)`, `Look (Vector2)`, `Sprint (hold)`, `Dodge` (Space / زر B), `LightAttack` (زر فأرة أيسر / RB), `HeavyAttack` (Shift+زر أيسر / RT), `Block (hold)` (زر فأرة أيمن / LB), `LockOn` (Q / عصا يمنى ضغط).
  - `PlayerAvatar : MonoBehaviour` — الغلاف الجامع: يبني `StaminaPool` و`PlayerBrain` من الـ assets، يوصّل الإدخال بأوامر الـ Brain، ويستمع لأحداثه (HitboxOpen→Hitbox.Activate بحزمة من WeaponData، InvulnerabilityChanged→Damageable.IsInvulnerable، Damaged/PoiseBroken/Died→NotifyX). يعرّض `PlayerBrain Brain` و`Damageable Damageable` للمهام التالية.
  - `PlayerMotor : MonoBehaviour` — CharacterController: حركة نسبية للكاميرا، مشي/جري/عدو بسرعات `CharacterStats`، العدو فقط مع ستامينا متاحة، دوران نحو اتجاه الحركة، اندفاع التفادي 3.5m خلال مدته باتجاه الإدخال، الحركة مجمّدة في Attack/Stagger/Death ومقيدة في Block (walkSpeed).

- [ ] **Step 1:** أنشئ ملف الـ inputactions بالخريطة أعلاه (JSON مباشرة — الصيغة نصية).
- [ ] **Step 2:** نفّذ `PlayerMotor` و`PlayerAvatar`.
- [ ] **Step 3:** ابنِ مشهد `Arena.unity`: أرضية 40×40، 4 جدران، 3 صناديق، إضاءة Directional خافتة + ضباب، وPrefab اللاعب (كبسولة + CharacterController + المكونات + Hurtbox + Hitbox ابن على مستوى "السلاح").
- [ ] **Step 4:** تحقق: `./scripts/run-tests.sh` يترجم وينجح؛ افتح المشهد في وضع batch وتأكد من عدم وجود أخطاء تهيئة (`-batchmode -quit` مع log نظيف).
- [ ] **Step 5:** Commit: `feat: input actions, player motor and graybox arena scene`

### Task 8: الكاميرا والـ Lock-on

**Files:**
- Create: `Assets/_Game/Scripts/Player/LockOnSelector.cs`, `LockOnSystem.cs`, `ITargetable.cs`
- Modify: `Assets/_Game/Scenes/Arena.unity` (كاميرات Cinemachine)
- Test: `Assets/_Game/Tests/EditMode/LockOnSelectorTests.cs`

**Interfaces:**
- Consumes: `PlayerMotor` (وضع strafe), `Damageable` (حدث موت الهدف).
- Produces:
  - `interface ITargetable { Vector3 Position { get; } bool IsAlive { get; } }`
  - `static class LockOnSelector` (خالص): `ITargetable Select(Vector3 origin, Vector3 forward, IReadOnlyList<ITargetable> candidates, float maxDistance, float maxAngleDegrees)` — الأقرب داخل المخروط، `null` إن لا شيء؛ `ITargetable Switch(ITargetable current, int direction, Vector3 origin, Vector3 forward, IReadOnlyList<ITargetable> candidates, ...)` — الهدف التالي يميناً/يساراً.
  - `LockOnSystem : MonoBehaviour` — زر LockOn يستدعي Select (maxDistance=15، maxAngle=60)؛ أثناء القفل: `PlayerMotor.StrafeTarget = target`، تبديل بالعصا اليمنى، فك تلقائي عند موت الهدف أو مسافة >20 أو ضغط الزر.
  - كاميرتان Cinemachine: حرة (orbit) وقفل (تؤطر اللاعب والهدف)، التبديل بالأولوية.

- [ ] **Step 1:** اختبارات فاشلة لـ `LockOnSelector` بأهداف وهمية:

```csharp
[Test] public void Select_PicksNearestInCone()
[Test] public void Select_IgnoresOutOfAngleAndDeadAndFar()
[Test] public void Select_EmptyCandidates_ReturnsNull()   // بلا استثناء
[Test] public void Switch_PicksNextInDirection()
```

- [ ] **Step 2:** شغّل — FAIL، ثم نفّذ `LockOnSelector` — PASS.
- [ ] **Step 3:** نفّذ `LockOnSystem` وأضف كاميرات Cinemachine للمشهد، ووضع strafe في `PlayerMotor` (يواجه الهدف، الحركة الجانبية حقيقية).
- [ ] **Step 4:** شغّل `./scripts/run-tests.sh` — المتوقع: PASS.
- [ ] **Step 5:** Commit: `feat: third-person camera and lock-on system`

### Task 9: ذكاء العدو — EnemyBrain وPrefab العدو

**Files:**
- Create: `Assets/_Game/Scripts/Enemies/EnemyBrain.cs`, `EnemyAvatar.cs`
- Create: `Assets/_Game/Data/Enemy_Sword.asset` (WeaponData: light `(20, 20, 0)` damage/poise/stamina، timeline `(0.6, 0.3, 1.2, 1.2)`)
- Create: `Assets/_Game/Prefabs/EnemyMelee.prefab`
- Modify: `Assets/_Game/Scenes/Arena.unity`
- Test: `Assets/_Game/Tests/EditMode/EnemyBrainTests.cs`

**Interfaces:**
- Consumes: `Damageable`, `Hitbox`, `WeaponData`, `ITargetable`.
- Produces (namespace `SoulsGame.Enemies`) — كلاس خالص يقاد بإدراك مُمرَّر:
  - `enum EnemyStateId { Patrol, Chase, AttackWindup, AttackActive, AttackRecovery, Stagger, Dead }`
  - `class EnemyBrain`: ctor `(WeaponData weapon, float detectRange=10, float fovDegrees=90, float attackRange=2, float staggerDuration=0.8)`.
  - `void Tick(float dt, float distanceToPlayer, float angleToPlayerDegrees)` — Patrol→Chase عند (مسافة ≤ detectRange وزاوية ≤ fov/2)؛ Chase→AttackWindup عند مسافة ≤ attackRange؛ windup→active→recovery بمدد الـ timeline ثم عودة لـ Chase.
  - أحداث: `StateChanged`, `HitboxOpen` (عند دخول AttackActive), `HitboxClose`.
  - `void NotifyPoiseBroken()` → Stagger **ويطلق HitboxClose إن كان مفتوحاً**؛ `void NotifyDied()` → Dead نهائية.
  - `EnemyAvatar : MonoBehaviour` — يغلّف Brain + Damageable + Hitbox، حركة Chase مباشرة نحو اللاعب (5 m/s) ودورية بين نقطتي patrol، يطبّق ضرر هجومه عبر Hitbox، ويُنفّذ `ITargetable`. `void ResetToSpawn()` يعيد الموضع والصحة والحالة.

- [ ] **Step 1:** اختبارات فاشلة:

```csharp
[Test] public void Patrol_DetectsPlayerInRangeAndFov()
[Test] public void OutOfFov_StaysPatrolling()
[Test] public void Chase_AttacksInRange_FollowsTimeline()  // windup 0.6 ثم HitboxOpen، بعد 0.3 HitboxClose، بعد 1.2 عودة Chase
[Test] public void PoiseBreak_DuringActive_ClosesHitboxAndStaggers()
[Test] public void Dead_IsTerminal()
```

- [ ] **Step 2:** شغّل — FAIL، ثم نفّذ `EnemyBrain` — PASS.
- [ ] **Step 3:** نفّذ `EnemyAvatar` وأنشئ `Enemy_Sword.asset` وPrefab العدو (كبسولة حمراء + Damageable بـ EnemyMeleeStats + Hurtbox + Hitbox) وضعه في الساحة بنقطتي دورية.
- [ ] **Step 4:** شغّل `./scripts/run-tests.sh` — المتوقع: PASS.
- [ ] **Step 5:** Commit: `feat: souls-pattern enemy AI with telegraphed attacks and punish windows`

### Task 10: الموت والإعادة وواجهة المستخدم

**Files:**
- Create: `Assets/_Game/Scripts/UI/ResourceBar.cs`, `EnemyHealthBar.cs`, `DeathScreen.cs`
- Create: `Assets/_Game/Scripts/Core/GameLoop.cs`
- Modify: `Assets/_Game/Scenes/Arena.unity`

**Interfaces:**
- Consumes: `PlayerAvatar` (Damageable + StaminaPool), `EnemyAvatar.ResetToSpawn()`, `LockOnSystem` (الهدف الحالي).
- Produces:
  - `ResourceBar : MonoBehaviour` — Slider يتتبع (قيمة حالية/قصوى) عبر `Func<float>`؛ يُستخدم للصحة (أحمر) والستامينا (أخضر) أسفل يسار الشاشة؛ `void Flash()` يومض الشريط (يُربط بحدث `PlayerBrain.StaminaRejected` — المؤشر البصري لنقص الستامينا من المواصفة).
  - `EnemyHealthBar` — شريط فوق العدو يظهر فقط عند كونه هدف الـ lock-on.
  - `DeathScreen` — عند موت اللاعب: تعتيم + نص "لقد هلكت / YOU DIED" لثانيتين ثم استدعاء `GameLoop.Respawn()`.
  - `GameLoop : MonoBehaviour` — يملك نقطة البداية؛ `Respawn()`: يعيد اللاعب للنقطة بصحة وستامينا كاملتين ويستدعي `ResetToSpawn()` لكل عدو.

- [ ] **Step 1:** نفّذ المكونات الأربعة واربطها في المشهد (Canvas: شريطا اللاعب + شاشة الموت؛ شريط العدو World Space).
- [ ] **Step 2:** تحقق: `./scripts/run-tests.sh` PASS وترجمة نظيفة وفتح المشهد batch بلا أخطاء.
- [ ] **Step 3:** Commit: `feat: death/respawn loop and combat HUD`

### Task 11: التحقق النهائي وقائمة فحص الإحساس

**Files:**
- Create: `docs/playtest-checklist.md`

- [ ] **Step 1:** شغّل `./scripts/run-tests.sh` — المتوقع: كل الاختبارات PASS.
- [ ] **Step 2:** اكتب `docs/playtest-checklist.md`: قائمة فحص يدوية (إحساس التفادي ووضوح i-frames، وزن الضربات، قابلية قراءة هجمات العدو، عدالة نافذة العقاب، إحساس إدارة الستامينا، سلاسة الـ lock-on والتبديل، حلقة الموت/الإعادة) — لكل بند: كيف تختبره وما السلوك المقبول، وتذكير أن الموازنة تُعدَّل من ملفات `Assets/_Game/Data/*.asset`.
- [ ] **Step 3:** راجع المواصفة بنداً بنداً وتأكد أن كل متطلب داخل نطاق المرحلة 1 منفَّذ؛ أصلح أي نقص.
- [ ] **Step 4:** Commit: `docs: playtest checklist for combat feel` ثم أبلغ الشريك أن المرحلة 1 جاهزة للتجربة في Unity Editor على جهازه.
