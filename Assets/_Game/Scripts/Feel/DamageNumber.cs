using UnityEngine;

namespace SoulsGame.Feel
{
    // رقم ضرر عائم: يصعد 1.2م ويتلاشى خلال 0.8ث ثم يفنى — مثل Elden Ring.
    public class DamageNumber : MonoBehaviour
    {
        public const float Lifetime = 0.8f;
        public const float RiseDistance = 1.2f;

        public TextMesh label;

        private float elapsed;
        private Vector3 origin;

        // منطق الحركة الصافي: (صعود، شفافية) بدلالة الزمن.
        public static (float rise, float alpha) Evaluate(float t)
        {
            float k = Mathf.Clamp01(t / Lifetime);
            return (RiseDistance * k, 1f - k);
        }

        // آخر موضع ولادة — للاختبارات (ارتفاع الرقم فوق رأس الهدف)
        public static Vector3 LastSpawnPosition { get; private set; }

        public static DamageNumber Spawn(Vector3 position, float amount)
        {
            LastSpawnPosition = position;
            var go = new GameObject("DamageNumber");
            go.transform.position = position;
            var text = go.AddComponent<TextMesh>();
            text.text = Mathf.Max(1f, amount).ToString("0");
            text.fontSize = 32;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.white;
            // TextMesh بلا خط ومادته لا يرسم شيئاً في Unity — الإغفال الكلاسيكي
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                text.font = font;
                // Unity يضيف MeshRenderer مع TextMesh تلقائياً؛ الشيم لا — نضمن وجوده
                var meshRenderer = go.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.material = font.material;
            }
            var number = go.AddComponent<DamageNumber>();
            number.label = text;
            number.origin = position;
            return number;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var (rise, alpha) = Evaluate(elapsed);
            transform.position = origin + Vector3.up * rise;
            if (label != null)
                label.color = new Color(1f, 1f, 1f, alpha);
            // الرقم يواجه الكاميرا دائماً
            var camera = Camera.main;
            if (camera != null)
                transform.rotation = camera.transform.rotation;
            if (elapsed >= Lifetime)
                Destroy(gameObject);
        }
    }
}
