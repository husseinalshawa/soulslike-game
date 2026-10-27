using SoulsGame.Enemies;
using SoulsGame.Player;
using SoulsGame.Progression;
using SoulsGame.UI;
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

        public BloodstainLedger Ledger { get; } = new BloodstainLedger();
        public GraceSystem Graces { get; } = new GraceSystem();

        private GameObject stainMarker;

        public void Initialize(PlayerAvatar playerAvatar, EnemyAvatar[] allEnemies, DeathScreen screen)
        {
            player = playerAvatar;
            enemies = allEnemies;
            deathScreen = screen;
            Graces.AddSite(0, playerAvatar.transform.position, discovered: true);

            player.Damageable.Died += OnPlayerDied;
            deathScreen.OnRespawnRequested = Respawn;
        }

        private void OnPlayerDied()
        {
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
                RefreshStainMarker();
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
        }
    }
}
