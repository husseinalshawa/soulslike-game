using SoulsGame.Enemies;
using SoulsGame.Player;
using SoulsGame.Progression;
using SoulsGame.Story;
using SoulsGame.UI;
using SoulsGame.World;
using UnityEngine;

namespace SoulsGame.Core
{
    // حلقة الموت والإعادة + اقتصاد الأرواح: الموت يسقط الأرواح كبقعة دم،
    // والإحياء عند آخر موقع راحة، والمرور على البقعة يستعيدها.
    public class GameLoop : MonoBehaviour
    {
        private const float RecoverRadius = 1.5f;

        public PlayerAvatar player;
        public EnemyAvatar[] enemies;
        public DeathScreen deathScreen;
        public Toast toast;
        public LevelUpMenu levelUpMenu;
        public GraceSiteMarker[] graceMarkers;
        public BossAvatar boss;
        public FogGate fogGate;
        public SoulsCache cache;

        // المرحلة 5: الشخصيات والمذبح ولوحة الحوار (يسندها البناء)
        public NpcAvatar[] npcs;
        public Vector3? altarPosition;
        public DialoguePanel dialoguePanel;
        public Toast banner;
        public const float AltarRadius = 2.5f;
        public const int AltarReward = 3000;

        public BloodstainLedger Ledger { get; } = new BloodstainLedger();
        public GraceSystem Graces { get; } = new GraceSystem();
        public StoryState Story { get; } = new StoryState();
        public DialogueRunner Dialogue { get; } = new DialogueRunner();

        private GameObject stainMarker;

        public void Initialize(PlayerAvatar playerAvatar, EnemyAvatar[] allEnemies, DeathScreen screen)
        {
            player = playerAvatar;
            enemies = allEnemies;
            deathScreen = screen;
            EnemyAvatar.AggroSuppressed = false; // حالة static — تصفير عند كل بدء
            Graces.AddSite(0, playerAvatar.transform.position, discovered: true);

            player.Damageable.Died += OnPlayerDied;
            player.InteractPressed += HandleInteract;
            deathScreen.OnRespawnRequested = Respawn;
            Dialogue.SessionChanged += OnDialogueSessionChanged;
        }

        // زر التفاعل بالأولوية: حوار جارٍ → تقدّم؛ قائمة مفتوحة → إغلاق؛
        // شخصية قريبة → حديث؛ المذبح قريب → تفاعل؛ موقع راحة → جلوس.
        // ولا شيء من هذا أثناء الموت.
        public void HandleInteract()
        {
            if (player.Damageable.Health.IsDead)
                return;
            if (Dialogue.IsActive)
            {
                Dialogue.Advance();
                return;
            }
            if (levelUpMenu != null && levelUpMenu.IsOpen)
            {
                levelUpMenu.Close();
                return;
            }
            var npc = NearestNpcInRange();
            if (npc != null)
            {
                StartDialogue(npc);
                return;
            }
            if (altarPosition.HasValue &&
                Vector3.Distance(player.transform.position, altarPosition.Value) <= AltarRadius)
            {
                InteractAltar();
                return;
            }
            if (Graces.CanRest(player.transform.position, out int siteId))
                RestAtGrace(siteId);
        }

        private NpcAvatar NearestNpcInRange()
        {
            if (npcs == null)
                return null;
            NpcAvatar nearest = null;
            float best = float.MaxValue;
            foreach (var npc in npcs)
            {
                if (npc == null || !npc.InRange(player.transform.position))
                    continue;
                float d = Vector3.Distance(player.transform.position, npc.transform.position);
                if (d < best) { best = d; nearest = npc; }
            }
            return nearest;
        }

        public const float HostileTalkRange = 10f;

        // لا حوار والسيوف مشهورة: قفل الإدخال وسط مطاردة = موت بلا دفاع.
        private bool AnyHostileEnemyNearby()
        {
            if (enemies == null)
                return false;
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                    continue;
                var state = enemy.Brain.CurrentState;
                if (state == EnemyStateId.Patrol || state == EnemyStateId.Returning ||
                    state == EnemyStateId.Dead)
                    continue;
                if (Vector3.Distance(player.transform.position, enemy.transform.position)
                    <= HostileTalkRange)
                    return true;
            }
            return false;
        }

        public void StartDialogue(NpcAvatar npc)
        {
            var stage = npc.Questline.ActiveStage(Story, player.KeyItems);
            if (stage == null)
                return;
            if (AnyHostileEnemyNearby())
            {
                toast?.Show("They will not speak while blades are drawn.");
                return;
            }
            player.InputLocked = true;
            EnemyAvatar.AggroSuppressed = true;
            Dialogue.Begin(npc.Questline.NpcName, stage.Lines, () =>
            {
                bool hadAshes = player.KeyItems.Has(StoryLibrary.WardensAshes);
                npc.Questline.CompleteStage(stage, Story, player.KeyItems, player.Wallet);
                // قطعة قصة مُنحت في الحوار: الاسم والوصف المبهم يظهران كالالتقاط
                if (!hadAshes && player.KeyItems.Has(StoryLibrary.WardensAshes))
                    toast?.Show($"{StoryLibrary.WardensAshes} — {StoryLibrary.WardensAshesDescription}");
            });
        }

        private void OnDialogueSessionChanged()
        {
            if (Dialogue.IsActive)
            {
                dialoguePanel?.Show(Dialogue.SpeakerName, Dialogue.CurrentLine);
                return;
            }
            dialoguePanel?.Hide();
            // لا فك قفل فوق قائمة مفتوحة (لا تتقاطعان عملياً — حزام أمان)
            if (levelUpMenu == null || !levelUpMenu.IsOpen)
            {
                player.InputLocked = false;
                EnemyAvatar.AggroSuppressed = false;
            }
        }

        private void InteractAltar()
        {
            switch (AltarLogic.TryPlace(Story, player.KeyItems))
            {
                case AltarLogic.Result.Placed:
                    player.Wallet.Add(AltarReward);
                    banner?.Show("THE MEADOW REMEMBERS");
                    break;
                case AltarLogic.Result.AlreadyPlaced:
                    toast?.Show("Only a faint, warm hum remains.");
                    break;
                default:
                    toast?.Show("The altar is silent.");
                    break;
            }
        }

        public void RestAtGrace(int siteId)
        {
            Graces.RestAt(siteId);
            player.Damageable.ResetToFull();
            player.Stamina.ResetToFull();
            player.Focus.ResetToFull();

            if (enemies != null)
                foreach (var enemy in enemies)
                    if (enemy != null)
                        enemy.ResetToSpawn();

            toast?.Show("Rested at grace");
            levelUpMenu?.Open();
        }

        private void OnPlayerDied()
        {
            if (levelUpMenu != null && levelUpMenu.IsOpen)
                levelUpMenu.Close();
            Dialogue.Abort(); // موت منتصف حوار: جلسة تنتهي بلا فعل إكمال
            player.Buff.Clear(); // التعزيز يسقط بالموت
            Ledger.Drop(player.transform.position, player.Wallet.TakeAll());
            RefreshStainMarker();
            deathScreen.Show();
        }

        private void Update()
        {
            if (player == null || player.Damageable.Health.IsDead)
                return;

            int recovered = Ledger.TryRecover(player.transform.position, RecoverRadius);
            if (recovered > 0)
            {
                player.Wallet.Add(recovered);
                toast?.Show($"Recovered {recovered} souls");
                RefreshStainMarker();
            }

            if (cache != null)
            {
                int cacheSouls = cache.TryCollect(player.transform.position);
                if (cacheSouls > 0)
                {
                    player.Wallet.Add(cacheSouls);
                    toast?.Show($"Found {cacheSouls} souls");
                }
            }

            ProcessDiscovery();
        }

        // اكتشاف مواقع الراحة + أعلام القصة التابعة (الكهف 2، الكنيسة 3) —
        // مفصولة عن Update لتكون قابلة للاختبار في بيئة الشيم.
        public void ProcessDiscovery()
        {
            int discovered = Graces.TryDiscover(player.transform.position);
            if (discovered < 0)
                return;
            if (discovered == 2) Story.Set(StoryFlag.CaveDiscovered);
            if (discovered == 3) Story.Set(StoryFlag.ChapelDiscovered);
            toast?.Show("Grace discovered");
            if (graceMarkers != null)
                foreach (var marker in graceMarkers)
                    if (marker != null && marker.siteId == discovered)
                        marker.SetLit(true);
        }

        private void RefreshStainMarker()
        {
            if (Ledger.HasStain && stainMarker == null)
            {
                stainMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stainMarker.name = "Bloodstain";
                stainMarker.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
                var collider = stainMarker.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                GameBootstrap.Tint(stainMarker, new Color(0.5f, 0.9f, 0.6f));
            }
            if (stainMarker != null)
            {
                stainMarker.SetActive(Ledger.HasStain);
                if (Ledger.HasStain)
                    stainMarker.transform.position = Ledger.Position + Vector3.up * 0.75f;
            }
        }

        public void Respawn()
        {
            if (levelUpMenu != null && levelUpMenu.IsOpen)
                levelUpMenu.Close();

            Vector3 spawn = Graces.RespawnPosition;
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = spawn;
            if (controller != null) controller.enabled = true;

            player.Damageable.ResetToFull();
            player.Stamina.ResetToFull();
            player.Focus.ResetToFull();
            player.Brain.Revive();

            if (enemies != null)
                foreach (var enemy in enemies)
                    if (enemy != null)
                        enemy.ResetToSpawn();

            // موت أثناء معركة البوس: البوس يعود كاملاً والبوابة تفتح من جديد
            if (boss != null && !boss.DefeatedForever && boss.FightActive)
            {
                boss.ResetFight();
                fogGate?.Reopen();
            }
        }
    }
}
