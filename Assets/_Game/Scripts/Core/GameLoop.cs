using SoulsGame.Enemies;
using SoulsGame.Player;
using SoulsGame.Progression;
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

        public BloodstainLedger Ledger { get; } = new BloodstainLedger();
        public GraceSystem Graces { get; } = new GraceSystem();

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
        }

        // زر التفاعل: يغلق القائمة إن كانت مفتوحة (مخرج كيبورد مضمون)،
        // وإلا يجلس عند موقع راحة قريب — ولا شيء من هذا أثناء الموت.
        public void HandleInteract()
        {
            if (player.Damageable.Health.IsDead)
                return;
            if (levelUpMenu != null && levelUpMenu.IsOpen)
            {
                levelUpMenu.Close();
                return;
            }
            if (Graces.CanRest(player.transform.position, out int siteId))
                RestAtGrace(siteId);
        }

        public void RestAtGrace(int siteId)
        {
            Graces.RestAt(siteId);
            player.Damageable.ResetToFull();
            player.Stamina.ResetToFull();

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

            int discovered = Graces.TryDiscover(player.transform.position);
            if (discovered >= 0)
            {
                toast?.Show("Grace discovered");
                if (graceMarkers != null)
                    foreach (var marker in graceMarkers)
                        if (marker != null && marker.siteId == discovered)
                            marker.SetLit(true);
            }
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
