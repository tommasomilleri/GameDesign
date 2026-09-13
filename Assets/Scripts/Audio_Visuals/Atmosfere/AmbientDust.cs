
using UnityEngine;

namespace Atmosphere
{
    [AddComponentMenu("Atmosphere/Ambient Dust")]
    public class AmbientDust : AtmosphereVolume
    {
        [Header("Aspetto")]
        [ColorUsage(false, true)]
        [Tooltip("Colore del granello illuminato. Deve essere TIEPIDO, mai " +
                 "bianco puro: la polvere riflette il colore della luce che la " +
                 "colpisce, e quasi nessuna luce e' bianca.")]
        public Color dustColor = new Color(1f, 0.88f, 0.70f);

        [Range(0f, 3f)]
        public float brightness = 1f;

        [Tooltip("Dimensione in metri. Sono granelli: DEVONO essere minuscoli. " +
                 "Sopra i 2 centimetri diventano fiocchi di neve.")]
        public Vector2 grainSize = new Vector2(0.005f, 0.017f);

        [Tooltip("Diffrazione a quattro punte. Piu' bella ma piu' vistosa: " +
                 "usala solo con pochi granelli grandi e luce forte.")]
        public bool starShaped = false;

        [Header("Scintillio")]
        [Range(0f, 1f)]
        [Tooltip("I granelli lampeggiano ruotando e cambiando faccia alla luce. " +
                 "Senza questo sembrano puntini morti incollati allo schermo.")]
        public float twinkle = 0.7f;

        [Range(0.1f, 6f)]
        public float twinkleSpeed = 1.2f;

        [Header("Sedimentazione")]
        [Range(0f, 1f)]
        [Tooltip("Quanto il pulviscolo tende a scendere. La polvere reale cade, " +
                 "ma lentissimamente: valori alti sembrano cenere.")]
        public float settling = 0.15f;

        [Header("Correnti d'aria")]
        [Tooltip("Ampiezza della deriva pulsante, oltre a 'drift'. Simula le " +
                 "correnti convettive di un ambiente riscaldato.")]
        [Range(0f, 1f)]
        public float convection = 0.25f;

        [Min(0.01f)]
        [Tooltip("Periodo delle correnti in secondi. Alto = respiro lento.")]
        public float convectionPeriod = 12f;

        protected override void Configure()
        {
            main.startSize = new ParticleSystem.MinMaxCurve(grainSize.x, grainSize.y);
            main.startColor = dustColor;

            main.gravityModifier = settling * 0.004f;

            shape.randomDirectionAmount = 1f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient(0.15f, 0.85f);

            if (twinkle > 0f)
            {
                ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(
                    1f,
                    TwinkleCurve(5, 1f - twinkle * 0.85f, 13),
                    TwinkleCurve(8, 1f - twinkle * 0.70f, 71));
            }
        
            psr.sortMode = ParticleSystemSortMode.None;
            psr.minParticleSize = 0.0007f;
            psr.maxParticleSize = 0.02f;
        }

        protected override Material CreateMaterial()
        {
            return AtmosphereTextures.MoteMaterial(starShaped);
        }
        protected override bool UsesSharedMaterial { get { return true; } }

        protected override void ApplyForces()
        {
            Vector3 f = drift;

            if (convection > 0f)
            {
                float t = Time.time / Mathf.Max(0.01f, convectionPeriod);
                float ux = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f;
                float uy = (Mathf.PerlinNoise(t * 1.31f, seed + 45f) - 0.5f) * 2f;
                float uz = (Mathf.PerlinNoise(t * 0.77f, seed + 91f) - 0.5f) * 2f;

                float k = convection * 0.05f;
                f += new Vector3(ux * k, uy * k * 1.4f, uz * k);
            }

            force.x = new ParticleSystem.MinMaxCurve(f.x);
            force.y = new ParticleSystem.MinMaxCurve(f.y);
            force.z = new ParticleSystem.MinMaxCurve(f.z);
        }

        protected override void Tick(float dt)
        {
            float b = brightness;
            if (twinkle > 0f)
            {
                float g = Mathf.PerlinNoise(Time.time * twinkleSpeed * 0.35f, seed + 200f);
                b *= Mathf.Lerp(1f - twinkle * 0.22f, 1f + twinkle * 0.22f, g);
            }

            main.startColor = new Color(dustColor.r * b, dustColor.g * b,
                                        dustColor.b * b, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(grainSize.x, grainSize.y);
        }
        
        static AnimationCurve TwinkleCurve(int peaks, float low, int seed)
        {
            low = Mathf.Clamp01(low);

            Random.State saved = Random.state;
            Random.InitState(seed);

            int n = Mathf.Max(2, peaks * 2 + 1);
            Keyframe[] keys = new Keyframe[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                float v = (i % 2 == 0)
                    ? Random.Range(low, low + (1f - low) * 0.45f)
                    : Random.Range(low + (1f - low) * 0.7f, 1f);
                keys[i] = new Keyframe(t, v);
            }

            Random.state = saved;

            AnimationCurve c = new AnimationCurve(keys);
            
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 1f);
            return c;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (grainSize.x < 0.001f) grainSize.x = 0.001f;
            if (grainSize.y < grainSize.x) grainSize.y = grainSize.x;
        }
#endif
    }
}

