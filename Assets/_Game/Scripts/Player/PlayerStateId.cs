namespace SoulsGame.Player
{
    public enum PlayerStateId
    {
        Locomotion,
        Dodge,
        AttackLight,
        AttackHeavy,
        Block,
        Cast,   // إلقاء تعويذة — التزام كامل
        Skill,  // مهارة السلاح الخاصة — التزام كامل
        Stagger,
        Death
    }
}
