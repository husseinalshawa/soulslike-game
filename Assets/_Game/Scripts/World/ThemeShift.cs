using SoulsGame.Data;
using UnityEngine;

namespace SoulsGame.World
{
    // تحول لوني تدريجي بين سمتين (خاتمة المذبح: المرج يستعيد مسحة لون) —
    // منطق صافٍ، البناء يدفعه كل إطار ويطبق الناتج عبر ApplyTheme.
    public class ThemeShift
    {
        private readonly WorldTheme from;
        private readonly WorldTheme to;
        private readonly float duration;
        private float elapsed;

        public ThemeShift(WorldTheme fromTheme, WorldTheme toTheme, float shiftDuration)
        {
            from = fromTheme;
            to = toTheme;
            duration = Mathf.Max(0.0001f, shiftDuration);
        }

        public float T => Mathf.Clamp01(elapsed / duration);
        public bool Done => elapsed >= duration;

        public void Tick(float dt) => elapsed += dt;

        // يكتب مزيج كل حقول السمة عند T في نسخة العمل ويعيدها.
        public WorldTheme Blend(WorldTheme into)
        {
            float t = T;
            into.sunColor = LerpColor(from.sunColor, to.sunColor, t);
            into.sunIntensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t);
            into.sunPitchDegrees = Mathf.Lerp(from.sunPitchDegrees, to.sunPitchDegrees, t);
            into.sunYawDegrees = Mathf.Lerp(from.sunYawDegrees, to.sunYawDegrees, t);
            into.fogColor = LerpColor(from.fogColor, to.fogColor, t);
            into.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
            into.ambient = LerpColor(from.ambient, to.ambient, t);
            into.skyColor = LerpColor(from.skyColor, to.skyColor, t);
            into.grassColor = LerpColor(from.grassColor, to.grassColor, t);
            into.highGroundColor = LerpColor(from.highGroundColor, to.highGroundColor, t);
            into.rockColor = LerpColor(from.rockColor, to.rockColor, t);
            into.deadTreeColor = LerpColor(from.deadTreeColor, to.deadTreeColor, t);
            into.ruinColor = LerpColor(from.ruinColor, to.ruinColor, t);
            into.caveColor = LerpColor(from.caveColor, to.caveColor, t);
            return into;
        }

        private static Color LerpColor(Color a, Color b, float t) =>
            new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t),
                Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
    }
}
