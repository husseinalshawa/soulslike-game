using SoulsGame.Data;
using SoulsGame.Player;
using UnityEngine;
using SoulsGame.UI;

namespace SoulsGame.World
{
    // قطعة عتاد ملقاة في العالم (سلاح أو درع): لمسها يضيفها ويجهزها —
    // مرة واحدة للأبد، لا تعود بالراحة.
    public class GearPickup : MonoBehaviour
    {
        public const float CollectRadius = 1.5f;

        public WeaponData weapon;
        public ArmorData armor;
        public PlayerAvatar player;
        public Toast toast;

        private bool collected;

        public string DisplayName => weapon != null ? weapon.weaponName
            : armor != null ? armor.armorName : "Gear";

        // يعيد 1 عند أول التقاط ضمن النطاق، وإلا 0.
        public int TryCollect(Vector3 playerPosition)
        {
            if (collected || player == null ||
                Vector3.Distance(playerPosition, transform.position) > CollectRadius)
                return 0;

            collected = true;
            if (weapon != null)
                player.AcquireWeapon(weapon);
            else if (armor != null)
                player.AcquireArmor(armor);

            toast?.Show($"Obtained: {DisplayName}");
            var renderer = GetComponentInChildren<Renderer>() ?? GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            return 1;
        }

        private void Update()
        {
            if (!collected && player != null)
                TryCollect(player.transform.position);
        }
    }
}
