
using UnityEngine;

namespace RealisticFire
{
    /// <summary>
    /// Sorgente di fuoco completa: Point Light + particelle + audio + vento.
    ///
    /// USO: aggiungi questo componente a un GameObject VUOTO, con il pivot
    /// sulla BASE della fiamma. Costruisce Light, VFX e Audio da solo in Awake.
    /// NON creare i figli a mano.
    ///
    /// NON scalare il Transform: usa il campo "scale" qui sotto.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Effects/Fire Source")]
    public class FireSource : MonoBehaviour
    {
        // ---- ispettore ------------------------------------------------------
        [Header("Tipo di fuoco")]
        [Tooltip("Letto UNA SOLA VOLTA in Awake. Cambiarlo in Play non ha effetto.")]
        public FireKind kind = FireKind.Candle;

        [Header("Override (0 = usa il preset)")]
        [Min(0f)]
        [Tooltip("Sostituisce l'intensita' DOPO l'applicazione di 'scale', " +
                 "quindi ignora completamente 'scale'.")]
        public float intensityOverride = 0f;

        [Min(0f)]
        [Tooltip("Sostituisce il raggio DOPO l'applicazione di 'scale', " +
                 "quindi ignora completamente 'scale'.")]
        public float rangeOverride = 0f;

        [Range(0.2f, 4f)]
        [Tooltip("Scala fisica del fuoco. intensita' x scale^2, range x radice(scale). " +
                 "Usa QUESTO, non la scala del Transform.")]
        public float scale = 1f;

        [Header("Moduli")]
        public bool enableVFX = true;
        public bool enableAudio = true;
        public bool enableLight = true;

        [Tooltip("Muove la luce di pochi centimetri: fa DANZARE le ombre. " +
                 "Visibile solo se castShadows e' attivo.")]
        public bool shadowDance = true;

        [Tooltip("IL PARAMETRO PIU' COSTOSO DEL SISTEMA. Massimo 2-3 per scena.")]
        public bool castShadows = false;

        [Tooltip("Collisioni delle scintille col mondo. Costose: su mobile OFF.")]
        public bool sparkCollision = true;

        [Header("Materiali (consigliato assegnarli a mano)")]
        [Tooltip("Materiale ADDITIVO per fiamma, scintille e braci. " +
                 "Se lasciato vuoto viene generato a runtime, ma in BUILD lo " +
                 "shader puo' essere strippato e le particelle diventano magenta.")]
        public Material flameMaterial;

        [Tooltip("Materiale ALPHA BLENDED per il fumo. Stessa avvertenza.")]
        public Material smokeMaterial;

        [Header("Colore")]
        [Range(0f, 1f)]
        [Tooltip("Quanto le 3 zone di colore si rimescolano. 0 = colore fisso.")]
        public float colorMix = 1f;

        [Range(0f, 0.08f)]
        [Tooltip("Deriva lentissima di tinta. Evita che il colore torni uguale.")]
        public float hueDrift = 0.012f;

        [Range(0f, 1f)]
        [Tooltip("Variazione di tinta fissa per istanza. 0.45 con piu' fuochi vicini.")]
        public float perInstanceHueVariance = 0.35f;

        [Range(0f, 1f)]
        [Tooltip("Quanto il rosso brace affiora nei cali di intensita'.")]
        public float emberBleed = 0.6f;

        [Tooltip("Temperatura colore fisica in Kelvin. Solo Built-in e URP, " +
                 "Unity 2019.1 o superiore. METTI OFF su HDRP.")]
        public bool useKelvin = true;

        [Header("Audio (opzionale)")]
        public AudioClip loopClip;
        public AudioClip[] crackleClips;

        [Header("Performance")]
        [Range(0f, 120f)]
        [Tooltip("Aggiornamenti al secondo della luce. 0 = ogni frame. " +
                 "40 e' indistinguibile dal continuo.")]
        public float updateRate = 40f;

        [Min(0f)]
        [Tooltip("Oltre questa distanza il fuoco si semplifica. 0 = LOD off. " +
                 "Richiede che la camera abbia il tag MainCamera.")]
        public float lodDistance = 30f;

        // ---- stato interno --------------------------------------------------
        FireProfile p;
        Light li;
        FireVFX vfx;
        FireAudio audioFx;
        Transform lightT;

        Vector3 lightBasePos;
        float seed, localTime, accum, hueOffset;
        float gustTimer, gustDuration, gustTarget, gustCurrent;
        float curHeat = 0.5f;
        bool lodFar;
        bool initialized;
        bool extinguished;
        Coroutine dimRoutine;                    // FIX 2
        Camera cam;
        float camSearchTimer;

        /// <summary>Intensita' normalizzata 0..1 corrente.</summary>
        public float Heat { get { return curHeat; } }

        /// <summary>Il profilo effettivo usato, gia' scalato. Sola lettura.</summary>
        public FireProfile Profile { get { return p; } }

        /// <summary>True se il fuoco e' stato spento con Extinguish().</summary>
        public bool IsExtinguished { get { return extinguished; } }

        // ====================================================================
        //  INIZIALIZZAZIONE
        // ====================================================================
        void Awake()
        {
            if (initialized) return;
            initialized = true;

            p = FireProfile.Get(kind);
            ApplyScale();

            seed = Random.Range(0f, 10000f);
            localTime = Random.Range(0f, 1000f);   // desincronizza le istanze
            hueOffset = Random.Range(-1f, 1f) * perInstanceHueVariance * 0.030f;
            ShiftPalette(hueOffset);

            if (intensityOverride > 0f) p.intensity = intensityOverride;
            if (rangeOverride > 0f) p.range = rangeOverride;

            if (enableLight) BuildLight();
            if (enableVFX) BuildVFX();
            if (enableAudio) BuildAudio();

            // forza la creazione del vento globale se non esiste
            if (FireWind.I == null)
                Debug.LogWarning("[FireSource] FireWind non disponibile: " +
                                 "il fuoco sara' immobile.", this);

            cam = Camera.main;

            if (transform.lossyScale != Vector3.one)
                Debug.LogWarning("[FireSource] Il Transform di '" + name + "' e' scalato. " +
                                 "Usa il campo 'scale' del componente: la Light non " +
                                 "segue la scala del Transform e il risultato sara' " +
                                 "incoerente.", this);
        }

        void ApplyScale()
        {
            float s = Mathf.Clamp(scale, 0.05f, 10f);

            p.flameHeight *= s;
            p.flameWidth *= s;
            p.intensity *= s * s;               // la luce cresce col quadrato
            // FIX 12: il vecchio "* 1.2f" gonfiava il raggio del 20% anche a
            // scale = 1, quindi il preset non era mai davvero il preset.
            p.range *= Mathf.Sqrt(s);
            p.wobbleRadius *= s;
            p.smokeRise *= s;
            p.audioRange *= Mathf.Sqrt(s);
        }

        void BuildLight()
        {
            GameObject go = new GameObject("Light");
            go.transform.SetParent(transform, false);
            // la luce sta dentro la fiamma, poco sopra la base
            go.transform.localPosition = new Vector3(0f, p.flameHeight * 0.55f, 0f);

            lightT = go.transform;
            lightBasePos = lightT.localPosition;

            li = go.AddComponent<Light>();
            li.type = LightType.Point;
            li.intensity = p.intensity;
            li.range = p.range;
            li.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
            li.shadowStrength = 0.85f;
            li.shadowBias = 0.02f;
            li.shadowNormalBias = 0.05f;
            li.renderMode = LightRenderMode.ForcePixel;  // ignorato su URP

#if UNITY_2019_1_OR_NEWER
            if (useKelvin) li.useColorTemperature = true;
#endif
        }

        void BuildVFX()
        {
            GameObject go = new GameObject("VFX");
            go.transform.SetParent(transform, false);
            vfx = go.AddComponent<FireVFX>();
            vfx.Build(kind, p, flameMaterial, smokeMaterial, sparkCollision);
        }

        void BuildAudio()
        {
            GameObject go = new GameObject("Audio");
            go.transform.SetParent(transform, false);
            audioFx = go.AddComponent<FireAudio>();
            audioFx.loopClip = loopClip;
            audioFx.crackleClips = crackleClips;
            audioFx.Build(p);
        }

        // ====================================================================
        //  LOOP PRINCIPALE
        // ====================================================================
        void Update()
        {
            if (extinguished) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            localTime += dt;

            UpdateLOD(dt);

            float rate = lodFar ? 12f : updateRate;
            if (rate > 0f)
            {
                accum += dt;
                float step = 1f / rate;
                if (accum < step) return;

                // FIX 13: prima si passava sempre "step" come dt, quindi con
                // framerate basso il fuoco rallentava in tempo soggettivo.
                // Ora passiamo il tempo REALMENTE trascorso, clampato per non
                // far esplodere le interpolazioni dopo un hitch.
                float elapsed = Mathf.Min(accum, step * 4f);
                accum = 0f;
                Tick(elapsed);
            }
            else
            {
                Tick(dt);
            }
        }

        void UpdateLOD(float dt)
        {
            if (lodDistance <= 0f) return;

            // la camera puo' comparire dopo (scene additive, spawn del player)
            if (cam == null)
            {
                camSearchTimer -= dt;
                if (camSearchTimer <= 0f)
                {
                    cam = Camera.main;
                    camSearchTimer = 1f;
                }
                if (cam == null) return;
            }

            float d2 = (cam.transform.position - transform.position).sqrMagnitude;
            bool far = d2 > lodDistance * lodDistance;

            if (far != lodFar)
            {
                lodFar = far;
                ApplyLOD(far);
            }
        }

        void ApplyLOD(bool far)
        {
            if (li != null && castShadows)
                li.shadows = far ? LightShadows.None : LightShadows.Soft;

            // il camino resta visibile anche da lontano: si nota se sparisce
            if (vfx != null)
                vfx.gameObject.SetActive(!far || kind == FireKind.Fireplace);
        }

        void Tick(float dt)
        {
            float t = localTime;

            Vector3 wind = FireWind.SafeCurrent;
            float windMag = wind.magnitude * p.windSensitivity;

            // ---- rumore frattale 1/f, 4 ottave ------------------------------
            // E' la firma spettrale del fuoco reale: micro tremiti dentro il
            // respiro lento. Un solo Perlin darebbe un respiro troppo regolare.
            float n =
                P(t * p.speed * 0.35f, seed) * 0.50f +
                P(t * p.speed * 1.00f, seed + 31.7f) * 0.28f +
                P(t * p.speed * 2.70f, seed + 77.3f) * 0.15f +
                P(t * p.speed * 6.10f, seed + 133.9f) * 0.07f;

            UpdateGust(dt);

            // asimmetria: il fuoco cala piu' a fondo di quanto salga
            float signed = (n - 0.5f) * 2f;
            if (signed < 0f) signed *= 1.45f;
            signed = Mathf.Clamp(signed, -1f, 1f);

            // il vento amplifica il flicker e sottrae calore
            float windFlicker = 1f + windMag * 1.8f;
            float mod = 1f + signed * p.flickerAmount * windFlicker + gustCurrent;
            mod -= windMag * 0.35f;
            mod = Mathf.Max(0.06f, mod);           // non si spegne mai del tutto

            curHeat = Smooth01(Mathf.InverseLerp(0.55f, 1.35f, mod));

            if (li != null) UpdateLight(t, mod, signed, curHeat, wind);
            if (vfx != null) vfx.Tick(curHeat, wind);
            if (audioFx != null) audioFx.Tick(curHeat);
        }

        void UpdateLight(float t, float mod, float signed, float heat, Vector3 wind)
        {
            li.intensity = p.intensity * mod;

            float rangeN = P(t * p.speed * 0.5f, seed + 220f);
            li.range = p.range
                     * (1f + (rangeN - 0.5f) * 2f * p.rangeAmount)
                     * Mathf.Lerp(1f, Mathf.Sqrt(mod), 0.5f);

            // ---- COLORE: 3 zone con rumori INDIPENDENTI dal flicker ---------
            // E' questa decorrelazione che l'occhio legge come "vivo": a volte
            // la fiamma e' luminosa ma rossastra, a volte fioca ma gialla.
            float wCore = P(t * p.speed * 0.9f, seed + 410f);
            float wEmber = P(t * p.speed * 0.6f, seed + 620f);

            float coreW = Mathf.Clamp01(heat * 0.75f + (wCore - 0.5f) * colorMix);
            float emberW = Mathf.Clamp01((1f - heat) * emberBleed
                                         + (wEmber - 0.5f) * colorMix * 0.8f);

            Color c = p.cBody;                                     // base arancio
            c = Color.Lerp(c, p.cCore, coreW * colorMix);         // picchi: giallo
            c = Color.Lerp(c, p.cEmber, emberW * colorMix * 0.7f); // cali: brace

            // base blu o brace: affiora solo nei momenti calmi
            if (p.baseBlueAmount > 0f)
            {
                float calm = Mathf.Clamp01(1f - Mathf.Abs(signed) * 1.6f);
                c = Color.Lerp(c, p.cBase, p.baseBlueAmount * calm * colorMix);
            }

            // deriva di tinta lentissima e saturazione dinamica
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            h = Mathf.Repeat(h + (P(t * 0.13f, seed + 900f) - 0.5f) * 2f * hueDrift, 1f);
            s = Mathf.Clamp01(s * (1f + (0.5f - heat) * p.saturationSwing));
            c = Color.HSVToRGB(h, s, v);

#if UNITY_2019_1_OR_NEWER
            if (useKelvin && li.useColorTemperature)
            {
                li.colorTemperature = Mathf.Lerp(p.kelvinCold, p.kelvinHot, heat);
                // il Kelvin da' la temperatura, il colore la sporcizia
                li.color = Color.Lerp(Color.white, c, 0.45f);
            }
            else
            {
                li.color = c;
            }
#else
            li.color = c;
#endif

            // ---- danza delle ombre e inclinazione col vento ------------------
            if (shadowDance && lightT != null)
            {
                float wx = (P(t * p.speed * 0.8f, seed + 50f) - 0.5f) * 2f;
                float wz = (P(t * p.speed * 0.8f, seed + 150f) - 0.5f) * 2f;
                float wy = (P(t * p.speed * 1.6f, seed + 250f) - 0.5f) * 2f;

                Vector3 localWind = transform.InverseTransformDirection(wind)
                                    * p.windSensitivity;

                lightT.localPosition = lightBasePos + new Vector3(
                    wx * p.wobbleRadius + localWind.x * p.flameHeight * 0.5f,
                    wy * p.wobbleRadius * 0.6f + heat * p.wobbleRadius * 0.4f,
                    wz * p.wobbleRadius + localWind.z * p.flameHeight * 0.5f);
            }
        }

        void UpdateGust(float dt)
        {
            gustTimer -= dt;

            if (gustTimer <= 0f)
            {
                // intervallo esponenziale: eventi imprevedibili, mai ritmici
                float u = Mathf.Clamp(Random.value, 0.0001f, 0.9999f);
                gustTimer = Mathf.Min(-Mathf.Log(1f - u) /
                                      Mathf.Max(0.01f, p.gustChance), 60f);
                gustDuration = Random.Range(0.06f, 0.35f);

                bool isPop = Random.value >= 0.65f;   // 35% guizzo, 65% calo
                gustTarget = (isPop ? 1f : -1f) * p.gustPower * Random.Range(0.4f, 1f);

                // uno scoppiettio e' UN evento: luce, scintille e suono insieme
                if (isPop)
                {
                    float power = Mathf.Abs(gustTarget) / Mathf.Max(0.01f, p.gustPower);
                    if (vfx != null) vfx.Pop(power);
                    if (audioFx != null) audioFx.Pop(power);
                }
            }

            bool inEvent = gustTimer <= gustDuration;
            float k = inEvent ? 22f : 6f;          // attacco molto rapido
            float goal = inEvent ? gustTarget : 0f;
            gustCurrent = Mathf.Lerp(gustCurrent, goal, 1f - Mathf.Exp(-k * dt));
        }

        // ====================================================================
        //  API PUBBLICA
        // ====================================================================

        /// <summary>Spegne il fuoco in modo graduale.</summary>
        public void Extinguish(float seconds)
        {
            if (extinguished) return;
            extinguished = true;

            seconds = Mathf.Max(0.1f, seconds);

            if (vfx != null) vfx.StopEmitting();
            if (audioFx != null) audioFx.FadeOutAndStop(seconds);

            if (dimRoutine != null) StopCoroutine(dimRoutine);
            dimRoutine = StartCoroutine(DimRoutine(seconds));
        }

        /// <summary>Riaccende un fuoco spento.</summary>
        public void Relight()
        {
            if (!extinguished) return;
            extinguished = false;

            // FIX 2: la DimRoutine continuava a girare e riportava a zero
            // l'intensita' appena riassegnata.
            if (dimRoutine != null)
            {
                StopCoroutine(dimRoutine);
                dimRoutine = null;
            }

            if (vfx != null) vfx.Resume();

            if (li != null)
            {
                li.intensity = p.intensity;
                li.range = p.range;
                li.color = p.cBody;   // ripristina dal rosso brace del fade
            }

            // FIX 1: Build() era un no-op sul secondo giro. Ora c'e' Restart().
            if (audioFx != null && enableAudio) audioFx.Restart();
        }

        System.Collections.IEnumerator DimRoutine(float seconds)
        {
            if (li == null) { dimRoutine = null; yield break; }

            float i0 = li.intensity;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                li.intensity = Mathf.Lerp(i0, 0f, t / seconds);
                // mentre muore vira al rosso brace
                li.color = Color.Lerp(li.color, p.cEmber, Time.deltaTime * 2f);
                yield return null;
            }
            li.intensity = 0f;
            dimRoutine = null;
        }

        // ====================================================================
        //  UTILITY
        // ====================================================================
        void ShiftPalette(float d)
        {
            p.cEmber = HueShift(p.cEmber, d);
            p.cBody = HueShift(p.cBody, d);
            p.cCore = HueShift(p.cCore, d);
        }

        static Color HueShift(Color c, float d)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(Mathf.Repeat(h + d, 1f), s, v);
        }

        static float P(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }

        static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (scale < 0.2f) scale = 0.2f;
            if (updateRate < 0f) updateRate = 0f;
            if (lodDistance < 0f) lodDistance = 0f;
            if (intensityOverride < 0f) intensityOverride = 0f;
            if (rangeOverride < 0f) rangeOverride = 0f;
        }

        void OnDrawGizmosSelected()
        {
            FireProfile pr = FireProfile.Get(kind);
            float s = Mathf.Max(0.05f, scale);

            // raggio di illuminazione
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(
                transform.position + Vector3.up * pr.flameHeight * 0.55f * s,
                (rangeOverride > 0f ? rangeOverride : pr.range * Mathf.Sqrt(s)));

            // ingombro della fiamma
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(
                transform.position + Vector3.up * pr.flameHeight * 0.5f * s,
                new Vector3(pr.flameWidth, pr.flameHeight, pr.flameWidth) * s);

            // raggio audio
            if (enableAudio)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
                Gizmos.DrawWireSphere(transform.position, pr.audioRange * Mathf.Sqrt(s));
            }
        }
#endif
    }
}

