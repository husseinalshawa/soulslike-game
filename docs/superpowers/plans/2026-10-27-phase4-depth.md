# خطة تنفيذ المرحلة 4 — العمق (أسلحة، سحر، مهارات، دروع)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** خمسة أسلحة بشخصيات مختلفة تُلتقط من العالم، مورد تركيز (FP) بإحصائية رُوحانية، تعويذتان ومهارة لكل سلاح، وثلاث فئات دروع تبادل الحماية بخفة التفادي.

**Architecture:** كما السوابق: منطق خالص مُختبر (FocusPool, GearInventory, ProjectileLogic, WeaponBuff, توسعة PlayerBrain بحالتي Cast/Skill) + بيانات في ScriptableObjects (توسعة WeaponData، ArmorData، SpellData) + طبقة ربط رقيقة. مصنع `GearLibrary` ينتج أصول الأسلحة/الدروع/التعاويذ الخمسة بأرقام المواصفة (يستعمله الـ bootstrap والمحرر معاً — مصدر أرقام واحد مُختبر).

**Tech Stack:** كما السوابق.

**Spec:** `docs/superpowers/specs/2026-10-27-phase4-depth-design.md`

## Global Constraints

- كل قيود المراحل 1-3 سارية. أرقام الأسلحة/المهارات/الدروع/التعاويذ من جداول المواصفة حرفياً.
- FP: يبدأ 60، تجدد 3/ث بعد مهلة 2ث، الراحة تملؤه. رُوحانية: +5 FP لكل نقطة.
- أزرار: Tab/`<Gamepad>/dpad/right` سلاح؛ G/`dpad/down` درع؛ V/`dpad/left` تعويذة؛ C/`buttonWest` إلقاء؛ F/`leftTrigger` مهارة.
- الركوب يمنع: الهجوم والإلقاء والمهارة وكل تبديل. كسر التوازن يقطع Cast/Skill ويغلق نوافذها.
- الالتقاط ضمن 1.5م، للأبد (لا يعود بالراحة).

## Review Focus

- إلقاء/مهارة بـ FP ناقص: رفض + وميض بار FP، لا حالة عالقة — المهمة 4.
- كسر توازن أثناء وابل الخناجر (3 نوافذ): النوافذ المتبقية تُلغى — المهمة 4.
- التقاط سلاح مملوك مسبقاً: لا تكرار في الحوزة — المهمة 2.
- تبديل الدرع يغير مدة التفادي فوراً والنافذة المطلقة للحصانة [0.1,0.4] تبقى — المهمتان 4 و7.
- القذيفة تختار أقرب Hurtbox في مسارها لا أول مُسجَّل — المهمة 5.

---

### Task 1: FocusPool + رُوحانية

**Files:**
- Create: `Assets/_Game/Scripts/Combat/FocusPool.cs`
- Modify: `ProgressionData.cs` (`mindFocusPerPoint=5`, تبقى base 10)، `StatId.cs` (+Mind)، `PlayerProgression.cs` (`MaxFocus`)
- Test: `FocusPoolTests.cs` + إضافات `PlayerProgressionTests.cs`

**Interfaces:**
- Produces: `class FocusPool`: ctor `(float max)`؛ `Current/Max`؛ `bool TrySpend(float)`؛ `void Tick(float dt)` (تجدد 3/ث بعد مهلة 2ث)؛ `ResetToFull()`؛ `SetMax(float)` (ملء). و`PlayerProgression.MaxFocus = 60 + (mind-10)*5`.

- [ ] **Step 1:** اختبارات فاشلة: `TrySpend_Exact_Succeeds`، `Regen_WaitsTwoSeconds_Then3PerSec`، `SetMax_Refills`؛ وفي التقدم: `Mind_RaisesMaxFocus` (نقطة → 65). 
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: focus pool and mind stat`

### Task 2: حوزة العتاد — GearInventory

**Files:**
- Create: `Assets/_Game/Scripts/Progression/GearInventory.cs`
- Test: `GearInventoryTests.cs`

**Interfaces:**
- Produces: `class GearInventory<T> where T : class`: `bool Add(T item)` (false للمكرر)؛ `T Current`؛ `T Cycle()` (يدور ويعيد الجديد)؛ `int Count`. أول إضافة تصير Current.

- [ ] **Step 1:** اختبارات فاشلة: `Add_First_BecomesCurrent`، `Add_Duplicate_Rejected`، `Cycle_WrapsAround`، `Cycle_SingleItem_Stays`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: generic gear inventory`

### Task 3: البيانات — توسعة WeaponData + ArmorData + SpellData + GearLibrary

**Files:**
- Modify: `WeaponData.cs` (+`weaponName, reach=1.6f, hitboxWidth=0.25f, SkillData skill`؛ `struct SkillData { skillName, fpCost, AttackTimeline timeline, damage, poiseDamage, dashDistance, radius, hitCount }`)
- Create: `Assets/_Game/Scripts/Data/ArmorData.cs`, `SpellData.cs`, `Assets/_Game/Scripts/Data/GearLibrary.cs`
- Modify: `SoulsGameEditorMenu.cs` (أصول عبر GearLibrary)
- Test: `GearLibraryTests.cs`

**Interfaces:**
- Produces:
  - `ArmorData : ScriptableObject { armorName, damageReduction, dodgeDurationMultiplier, dodgeDistanceMultiplier, Color tint }`.
  - `SpellData : ScriptableObject { spellName, fpCost, damage, poiseDamage, projectileSpeed, maxRange, buffBonusDamage, buffDuration, bool isBuff }` (القذيفة: 12/35/10/20/30؛ التعزيز: 20, +10, 30ث).
  - `static class GearLibrary`: `WeaponData Longsword()/Greatsword()/TwinDaggers()/Spear()/Warhammer()`, `ArmorData Cloth()/Leather()/Plate()`, `SpellData GlintBolt()/WeaponBuffSpell()` — كل الأرقام من جداول المواصفة §2 و§5 و§6 (بما فيها الـ timelines والمهارات).
- `CharacterStats` يبقى كما هو.

- [ ] **Step 1:** اختبارات فاشلة تثبّت عيّنة أرقام من كل جدول: `Greatsword_Numbers` (خفيفة 45/35/22، timeline (0.45,0.30,0.70,0.85)، كومبو 2، reach 2.0، مهارة «زلزال» fp 25 radius 3)، `TwinDaggers_FiveHitCombo`، `Plate_Armor_Numbers` (0.30/1.30/0.85)، `GlintBolt_Numbers`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: weapon/armor/spell data and gear library`

### Task 4: توسعة PlayerBrain — حالتا Cast وSkill وملف التفادي

**Files:**
- Modify: `PlayerStateId.cs` (+Cast, +Skill)، `PlayerBrain.cs`
- Test: إضافات `PlayerBrainTests.cs`

**Interfaces:**
- Consumes: `FocusPool`, `SpellData`, `WeaponData.skill`.
- Produces:
  - ctor الجديد: `PlayerBrain(StaminaPool, WeaponData, CharacterStats, FocusPool focus = null)` (null = مراحل قديمة، الاختبارات القائمة تبقى خضراء).
  - `void SetWeapon(WeaponData)` — التبديل (مسموح في Locomotion/Block فقط، يعيد الكومبو).
  - `void SetDodgeProfile(float durationMultiplier)` — مدة التفادي تُضرب؛ نافذة الحصانة تبقى [0.1, 0.4] مطلقة.
  - `bool RequestCast(SpellData spell)`: من Locomotion/Block، يرفض بـ FP ناقص (حدث `FocusRejected`)؛ حالة Cast بـ timeline (0.35, 0.15, 0.4)؛ عند بدء الطور الفعال حدث `SpellReleased(SpellData)` (الغلاف يطلق قذيفة أو بافّاً).
  - `bool RequestSkill()`: مهارة السلاح الحالي، FP من skill.fpCost؛ حالة Skill بـ timeline المهارة؛ أحداث `HitboxOpen/Close` تتكرر `hitCount` مرة موزعة في الطور الفعال، و`SkillMotion(float dashDistance)` عند الدخول (الغلاف يحرك)؛ `CurrentSkill` مكشوفة للغلاف (radius).
  - كسر التوازن أثناء Cast/Skill → Stagger ويغلق أي نافذة ويُسقط ما تبقى.

- [ ] **Step 1:** اختبارات فاشلة: `Cast_ConsumesFp_AndReleasesAtActive`، `Cast_InsufficientFp_RejectedWithEvent`، `Skill_MultiHit_OpensWindowsHitCountTimes` (وابل = 3 فتحات)، `Skill_StaggerMidWay_CancelsRemainingWindows`، `SetWeapon_OnlyFromNeutral`، `DodgeProfile_StretchesDuration_KeepsIFrameWindow` (مضاعف 1.3: المناعة تنتهي عند 0.4 والحالة عند 0.78).
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS (مع كل القديم). **Step 5:** Commit: `feat: cast and skill states with dodge profiles`

### Task 5: ProjectileLogic + WeaponBuff

**Files:**
- Create: `Assets/_Game/Scripts/Combat/ProjectileLogic.cs`, `Assets/_Game/Scripts/Combat/WeaponBuff.cs`
- Test: `ProjectileBuffTests.cs`

**Interfaces:**
- Produces:
  - `class ProjectileLogic`: ctor `(Vector3 origin, Vector3 direction, float speed, float maxRange)`؛ `Vector3 Position`؛ `bool Expired`؛ `ITargetHit Step(float dt, IReadOnlyList<(object target, Vector3 position, float radius)> targets)` — يتقدم ويعيد الهدف **الأقرب للمسار** المُصاب هذا الإطار أو null؛ بعد maxRange ينتهي.
  - `class WeaponBuff`: `void Activate(float bonus, float duration)`؛ `float CurrentBonus` (0 بعد الانتهاء)؛ `void Tick(float dt)`؛ إعادة التفعيل تجدد المدة؛ `Clear()` (الموت).

- [ ] **Step 1:** اختبارات فاشلة: `Projectile_HitsNearestInPath` (هدفان على الخط → الأقرب)، `Projectile_ExpiresAtRange`، `Projectile_MissesOffPath`، `Buff_ExpiresAndRefreshes`، `Buff_ClearOnDeath`.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: projectile logic and weapon buff`

### Task 6: ربط العتاد — الالتقاط والتبديل والواجهة

**Files:**
- Create: `Assets/_Game/Scripts/World/GearPickup.cs`, `Assets/_Game/Scripts/UI/LoadoutLabel.cs`
- Modify: `PlayerControls.cs` (أكشنات SwitchWeapon/SwitchArmor/SwitchSpell/CastSpell/WeaponSkill)، `PlayerAvatar.cs` (حوزات + تجهيز + FP pool + أحداث)، `HudBuilder.cs` (بار FP أزرق + سطر العتاد)، `GameBootstrap.cs` (توزيع 4 أسلحة و2 درع في المواقع المحددة بالمواصفة عبر GearLibrary)، `GameLoop.cs` (التقاط بالمسافة كالكنز)، `LevelUpMenu.cs` (صف رُوحانية + ApplyProgression يضبط MaxFocus)
- Test: توسيع `BootstrapLifecycleTests.cs`

**Interfaces:**
- Produces: `PlayerAvatar.Weapons : GearInventory<WeaponData>` (يبدأ بالسيف الطويل)، `Armors : GearInventory<ArmorData>` (يبدأ بالقماش)، `Spells : GearInventory<SpellData>` (التعويذتان منذ البداية)، `Focus : FocusPool`؛ `void EquipCurrentWeapon()` (hitbox reach/width + شكل السلاح + Brain.SetWeapon)؛ `void EquipCurrentArmor()` (tint + DamageTransform + Brain.SetDodgeProfile + motor dodge distance)؛ `GearPickup : MonoBehaviour { WeaponData weapon; ArmorData armor; int TryCollect(Vector3) → 0/1 }` يلتقطه GameLoop.Update؛ مواضع: عظيم+صفائح في غرفة البوس، خنجران في غرفة الكهف، رمح معسكر 1، مطرقة معسكر 2، جلد عند طلل قرب موقع المنتصف. كل أزرار التبديل والإلقاء والمهارة محروسة بـ `!IsRiding && !InputLocked`؛ `RestAtGrace` يملأ `Focus` أيضاً.
- Consumes: GearLibrary (fallback الـ bootstrap).

- [ ] **Step 1:** وسّع دورة الحياة: `Player.Weapons.Count == 1` بدايةً؛ `bootstrap.Pickups.Length == 6`؛ التقاط العظيم (TryCollect عند موضعه) → `Weapons.Count == 2` و`Current.weaponName == "Greatsword"`؛ `Player.Focus.Max == 60`. شغّل — FAIL.
- [ ] **Step 2:** نفّذ كل الربط. **Step 3:** شغّل — PASS. **Step 4:** Commit: `feat: gear pickups, switching, FP bar and loadout HUD`

### Task 7: ربط السحر والمهارات والدروع في المشهد

**Files:**
- Create: `Assets/_Game/Scripts/Combat/ProjectileAvatar.cs`
- Modify: `PlayerAvatar.cs` (إطلاق قذيفة/بافّ عند SpellReleased، مهارة: SkillMotion يحرك عبر motor dash، radius يضرب دائرياً عبر ApplyDamage، ضرر الحزم += `Buff.CurrentBonus`)، `Damageable.cs` (+`Func<DamagePacket, DamagePacket> DamageTransform` بعد المعترض)، `PlayerMotor.cs` (dash مسافة مهارة + مضاعف مسافة تفادي الدرع)
- Test: توسيع `BootstrapLifecycleTests.cs` + اختبار Damageable

**Interfaces:**
- Produces: `ProjectileAvatar : MonoBehaviour` يغلف ProjectileLogic (كرة متوهجة، يجمع أهداف Hurtbox الأحياء كل إطار، يطبق DamagePacket عند الإصابة ويفنى)؛ `Damageable.DamageTransform` (الدرع: `healthDamage × (1 − reduction)`)؛ `GameLoop.OnPlayerDied` يصفّر البافّ (`player.Buff.Clear()`).
- [ ] **Step 1:** اختبار فاشل في HealthPoiseTests-نمط: `DamageTransform_ReducesHealthDamage` (تحويل 30% → ضربة 20 تخصم 14)؛ ودورة حياة: التقاط الصفائح + ApplyDamage → الضرر مخفض 30%؛ وتفعيل بافّ ثم HitboxOpen → حزمة السلاح أعلى بـ 10.
- [ ] **Step 2:** FAIL. **Step 3:** نفّذ. **Step 4:** PASS. **Step 5:** Commit: `feat: spells, weapon skills and armor wired into the world`

### Task 8: التحقق النهائي

- [ ] **Step 1:** suite كامل PASS. **Step 2:** قسم المرحلة 4 في قائمة الفحص (تمايز الأسلحة، قيمة FP، عدالة المهارات، مقايضة الدروع، موازنة البوس مع المطرقة) + README (العتاد والأزرار الجديدة). **Step 3:** مطابقة المواصفة بنداً بنداً. **Step 4:** Commit: `docs: phase 4 checklist and readme`
