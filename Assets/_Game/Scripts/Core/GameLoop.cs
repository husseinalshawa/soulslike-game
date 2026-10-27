using SoulsGame.Enemies;
using SoulsGame.Player;
using SoulsGame.UI;
using UnityEngine;

namespace SoulsGame.Core
{
    // حلقة الموت والإعادة: موت اللاعب → شاشة الموت → العودة لنقطة البداية
    // بصحة وستامينا كاملتين، وإعادة ضبط كل الأعداء.
    public class GameLoop : MonoBehaviour
    {
        public PlayerAvatar player;
        public EnemyAvatar[] enemies;
        public DeathScreen deathScreen;
        public Vector3 spawnPosition;

        public void Initialize(PlayerAvatar playerAvatar, EnemyAvatar[] allEnemies, DeathScreen screen)
        {
            player = playerAvatar;
            enemies = allEnemies;
            deathScreen = screen;
            spawnPosition = playerAvatar.transform.position;

            player.Damageable.Died += () => deathScreen.Show();
            deathScreen.OnRespawnRequested = Respawn;
        }

        public void Respawn()
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = spawnPosition;
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
