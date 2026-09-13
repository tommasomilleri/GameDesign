
using UnityEngine;

namespace Atmosphere
{
    [AddComponentMenu("Atmosphere/Ambient Mist")]
    public class AmbientMist : AtmosphereVolume
    {
        public enum MistStyle
        {
            GroundFog,
            Haze,
            LitHaze
        }

        [Header("Stile")]
        public MistStyle style = MistStyle.Haze;

        [Header("Aspetto")]
        [ColorUsage(false, true)]
        public Color mistColor = new Color(0.60f, 0.64f, 0.72f);

        [Range(0f, 0.30f)]
        [Tooltip("Opacita' del SINGOLO banco. E' l'errore piu' comune di tutto " +
                 "il sistema: alzarla. Sopra 0.10 sembra fumogeno da discoteca. " +
                 "Se vuoi piu' densita' alza 'count', mai questo valore.")]
        public float opacity = 0.055f;

        [Tooltip("Diametro dei banchi in metri. Devono essere GRANDI: " +
                 "circa un terzo della dimensione della stanza.")]
        public Vector2 bankSize = new Vector2(4f, 9f);

        [Header("Movimento")]
        [Range(0f, 0.3f)]
        [Tooltip("Rotazione dei billboard in radianti al secondo. " +
                 "Tenerla bassissima: la nebbia non gira, si arrotola.")]
        public float swirl = 0.05f;

        [Range(0.5f, 2.5f)]
        [Tooltip("Quanto i banchi si espandono durante la vita. " +
                 "E' la dissipazione: senza, sembrano solidi.")]
        public float expansion = 1.30f;

        [Tooltip("Solo GroundFog: impedisce che la nebbia si alzi dal suolo, " +
                 "limitando la velocita' verticale.")]
        public bool clampToGround = true;

        [Header("Occlusione")]
        [Min(0.01f)]
        [Tooltip("Metri di dissolvenza contro la geometria. " +
                 "RICHIEDE Depth Texture attiva. Alto = piu' morbido, ma sopra " +
                 "i 5 metri la nebbia inizia a sparire anche dove serve.")]
        public float softDistance = 3f;

        [Header("Variazione")]
        [Range(0f, 1f)]
        [Tooltip("Respiro lento della densita' complessiva. La nebbia reale non " +
                 "e' mai stabile: si addensa e si dirada in modo impercettibile.")]
        public float breathing = 0.25f;

        [Min(1f)]
        public float breathingPeriod = 25f;

        protected override void Configure()
        {
            bool ground = style == MistStyle.GroundFog;
            bool additive = style == MistStyle.LitHaze;

            main.startSize = new ParticleSystem.MinMaxCurve(bankSize.x, bankSize.y);
            main.startColor = WithAlpha(mistColor, opacity);
            main.gravityModifier = 0f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient(0.28f, 0.74f);

            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f / Mathf.Max(1f, expansion), 1f, 1f));

            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = swirl > 0f;
            rot.z = new ParticleSystem.MinMaxCurve(-swirl, swirl);

            if (ground && clampToGround)
            {
                ParticleSystem.LimitVelocityOverLifetimeModule lim =
                    ps.limitVelocityOverLifetime;
                lim.enabled = true;
                lim.separateAxes = true;
                lim.limitX = new ParticleSystem.MinMaxCurve(3f);
                lim.limitY = new ParticleSystem.MinMaxCurve(0.05f);
                lim.limitZ = new ParticleSystem.MinMaxCurve(3f);
                lim.dampen = 0.3f;
            }
            psr.sortMode = additive
                ? ParticleSystemSortMode.None
                : ParticleSystemSortMode.Distance;
            psr.sortingFudge = ground ? 45f : 25f;

            psr.maxParticleSize = 4f;
        }

        protected override Material CreateMaterial()
        {
            bool ground = style == MistStyle.GroundFog;
            bool additive = style == MistStyle.LitHaze;
            float soft = AtmosphereQuality.SoftParticles ? softDistance : 0f;
            return AtmosphereTextures.CreateMistMaterial(ground, additive, soft);
        }

        protected override void Tick(float dt)
        {
            float a = opacity;

            if (breathing > 0f)
            {
                float t = Time.time / Mathf.Max(1f, breathingPeriod);
                float n = Mathf.PerlinNoise(t, seed + 310f);
                a *= Mathf.Lerp(1f - breathing * 0.5f, 1f + breathing * 0.5f, n);
            }

            main.startColor = WithAlpha(mistColor, a);
            main.startSize = new ParticleSystem.MinMaxCurve(bankSize.x, bankSize.y);
        }

        static Color WithAlpha(Color c, float a)
        {
            c.a = Mathf.Clamp01(a);
            return c;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (bankSize.x < 0.3f) bankSize.x = 0.3f;
            if (bankSize.y < bankSize.x) bankSize.y = bankSize.x;
            if (softDistance < 0.01f) softDistance = 0.01f;
        }
#endif
    }
}
