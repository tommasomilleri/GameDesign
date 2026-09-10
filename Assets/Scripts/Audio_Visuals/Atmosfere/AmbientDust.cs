
using UnityEngine;

namespace Atmosphere
{
    /// <summary>
    /// Pulviscolo sospeso nell'aria di tutto l'ambiente.
    ///
    /// E' l'effetto col miglior rapporto resa su costo che esista. Non simula
    /// niente: sfrutta il fatto che il cervello legge i puntini luminosi
    /// sospesi come prova che l'aria ha densita' e che la luce e' fisica.
    /// Sessanta granelli fanno sembrare volumetrica una stanza che non lo e'.
    ///
    /// USO: GameObject vuoto al centro della stanza, Add Component.
    /// </summary>
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

            // sedimentazione impercettibile: la polvere cade, ma pianissimo
            main.gravityModifier = settling * 0.004f;

            // la direzione iniziale casuale evita che i granelli appena nati
            // sembrino sparati tutti dallo stesso punto
            shape.randomDirectionAmount = 1f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient(0.15f, 0.85f);

            // ---- SCINTILLIO ---------------------------------------------------
            // Un granello reale e' una scaglia irregolare: ruotando cambia la
            // faccia esposta alla luce e la sua luminosita' oscilla in modo
            // irregolare. Lo simuliamo con una curva di dimensione che pulsa
            // piu' volte lungo la vita.
            //
            // Il MinMaxCurve a DUE curve e' la chiave: ogni particella ne pesca
            // una a caso tra le due e interpola, quindi non lampeggiano mai
            // tutte in sincrono. Con una sola curva l'effetto sarebbe un
            // battito collettivo, immediatamente artificiale.
            if (twinkle > 0f)
            {
                ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(
                    1f,
                    TwinkleCurve(5, 1f - twinkle * 0.85f, 13),
                    TwinkleCurve(8, 1f - twinkle * 0.70f, 71));
            }

            // additivo: i granelli AGGIUNGONO luce, non occludono mai nulla.
            // Per questo l'ordinamento non serve, e non ordinare fa risparmiare.
            psr.sortMode = ParticleSystemSortMode.None;

            // Un granello lontano finisce dentro meno di un pixel e sparisce a
            // intermittenza col movimento della camera: il pulviscolo
            // "sfarfalla". minParticleSize impone una dimensione minima a
            // schermo e risolve il problema.
            psr.minParticleSize = 0.0007f;
            psr.maxParticleSize = 0.02f;
        }

        protected override Material CreateMaterial()
        {
            return AtmosphereTextures.MoteMaterial(starShaped);
        }

        // il materiale del pulviscolo e' condiviso da tutti i volumi: mai
        // distruggerlo, altrimenti gli altri volumi restano senza
        protected override bool UsesSharedMaterial { get { return true; } }

        protected override void ApplyForces()
        {
            Vector3 f = drift;

            if (convection > 0f)
            {
                // due rumori sfasati: le correnti non sono mai una spinta
                // costante, sono bolle d'aria che si staccano e risalgono
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

            // Modulazione globale lentissima: ogni tanto una corrente attraversa
            // la stanza e per un istante tutto il pulviscolo brilla di piu'.
            // E' un dettaglio quasi subliminale ma toglie la sensazione di
            // effetto "in loop".
            if (twinkle > 0f)
            {
                float g = Mathf.PerlinNoise(Time.time * twinkleSpeed * 0.35f, seed + 200f);
                b *= Mathf.Lerp(1f - twinkle * 0.22f, 1f + twinkle * 0.22f, g);
            }

            main.startColor = new Color(dustColor.r * b, dustColor.g * b,
                                        dustColor.b * b, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(grainSize.x, grainSize.y);
        }

        /// <summary>
        /// Curva che oscilla piu' volte tra 'low' e 1 lungo la vita della
        /// particella, con ampiezze irregolari.
        ///
        /// Random.state viene salvato e ripristinato: generare numeri casuali
        /// qui altererebbe la sequenza globale e renderebbe non riproducibile
        /// qualsiasi altro sistema che usi Random con un seed fisso.
        /// </summary>
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
            // SmoothTangents con peso 1 da' tangenti automatiche morbide.
            // Con peso 0 le appiattirebbe tutte e la curva diventerebbe
            // una scaletta, con lampeggi a scatti invece che fluidi.
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

