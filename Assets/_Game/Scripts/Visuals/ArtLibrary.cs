using UnityEngine;

namespace SoulsGame.Visuals
{
    // كتالوج الفن الحقيقي (Poly Haven، CC0): نماذج ومواد تلبس العالم المبني
    // برمجياً. يُنشأ ويُملأ من قائمة Tools/SoulsGame/3 ويُحمّل من Resources.
    [CreateAssetMenu(menuName = "SoulsGame/Art Library", fileName = "ArtLibrary")]
    public class ArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "ArtLibrary";

        [Header("Nature")]
        public GameObject[] deadTrees;
        public GameObject[] forestTrees;
        public GameObject[] fallenLogs;  // جذوع ساقطة — تُقاس بطولها لا بارتفاعها
        public GameObject[] rocks;
        public GameObject[] undergrowth; // سرخس، جذور، جذوع مقطوعة

        [Header("Structures")]
        public GameObject firePit;
        public GameObject fortRuin;
        public GameObject castleDoor;
        public GameObject ironGate;

        [Header("Materials")]
        public Material ground;
        public Material forestFloor;
        public Material stone;
        public Material rockFace;
        public Material skybox;

        public static ArtLibrary Load() => Resources.Load<ArtLibrary>(ResourcePath);
    }
}
