using SoulsGame.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoulsGame.Visuals
{
    // طبقة الأجواء المظلمة فوق العالم المبني برمجياً: معالجة لاحقة (URP Volume)
    // ونيران مخيم مضيئة. تعيش في تجميعة منفصلة لأنها تحتاج URP — منطق اللعبة
    // نفسه لا يعرف عنها شيئاً ويبقى قابلاً للاختبار بلا خط رسم.
    public class DarkAtmosphere : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<GameBootstrap>() == null) return; // مشاهد أخرى لا تُلمس
            if (FindAnyObjectByType<DarkAtmosphere>() != null) return;
            new GameObject("DarkAtmosphere").AddComponent<DarkAtmosphere>();
        }

        private VolumeProfile profile;

        private void Start()
        {
            WorldDresser.Dress(ArtLibrary.Load()); // فن حقيقي فوق الأشكال البدائية (إن وُجد الكتالوج)
            BuildVolume();
            EnablePostOnCamera();
            LightCampfires();
        }

        private void BuildVolume()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.ACES;

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.value = 0.35f;     // ACES يعتّم — نعوّض قليلاً كي يبقى القتال مقروءاً
            color.contrast.value = 18f;
            color.saturation.value = -35f;        // ألوان مستنزفة، عالم يحتضر
            color.colorFilter.value = new Color(0.86f, 0.9f, 1f);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0.9f;         // النيران وأضواء الراحة تتوهج في العتمة
            bloom.threshold.value = 0.9f;
            bloom.scatter.value = 0.7f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.38f;
            vignette.smoothness.value = 0.45f;

            var grain = profile.Add<FilmGrain>(true);
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.value = 0.25f;

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static void EnablePostOnCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            // الضباب يبتلع كل شيء بعد ~150م — لا داعي لرسم ما لا يُرى
            camera.farClipPlane = 220f;
        }

        // كل نار مخيم تصير مصدر ضوء برتقالي يرتعش — منارات في الظلام
        private static void LightCampfires()
        {
            foreach (var t in FindObjectsByType<Transform>())
            {
                if (t.name != "Campfire") continue;
                var lightGo = new GameObject("CampfireLight");
                lightGo.transform.SetParent(t, false);
                lightGo.transform.localPosition = Vector3.up * 1.2f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.55f, 0.2f);
                light.range = 14f;
                light.intensity = 3f;
                light.shadows = LightShadows.Soft;
                lightGo.AddComponent<Flicker>();
            }
        }

        private void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }
    }

    public class Flicker : MonoBehaviour
    {
        private Light target;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            target = GetComponent<Light>();
            baseIntensity = target.intensity;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * 6f);
            target.intensity = baseIntensity * (0.75f + 0.5f * n);
        }
    }
}
