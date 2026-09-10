
using UnityEngine;

namespace Nature
{
    /// <summary>
    /// Vento globale per la vegetazione.
    ///
    /// Non muove nulla da solo: pubblica tre vettori in variabili globali di
    /// shader che tutti i materiali "Nature/Wind Vegetation" leggono. Il
    /// movimento avviene interamente sulla GPU, nel vertex shader.
    ///
    /// USO: un solo componente in scena, su un GameObject qualsiasi.
    /// Se non c'e', vengono comunque installati valori di riserva reali
    /// (vedi InstallDefaults) e la vegetazione ondeggia lo stesso.
    ///
    /// [ExecuteAlways] serve perche' la Scene View mostri il vento anche
    /// fuori dal Play Mode.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("Nature/Nature Wind")]
    [DisallowMultipleComponent]
    public class NatureWind : MonoBehaviour
    {
        // ---- ID delle proprieta' globali, risolti una volta sola ------------
        static readonly int ID_WindDir = Shader.PropertyToID("_GlobalWindDirection");
        static readonly int ID_WindParams = Shader.PropertyToID("_GlobalWindParams");
        static readonly int ID_GustParams = Shader.PropertyToID("_GlobalGustParams");

        // Il wrap del tempo e' un multiplo intero di 2*PI cosi' che TUTTE le
        // sinusoidi usate nello shader (che hanno frequenze intere o quasi)
        // ritornino alla stessa fase: il salto e' invisibile.
        // Senza wrap, dopo ~2 ore un float perde risoluzione sotto il
        // millisecondo e il flutter ad alta frequenza diventa a scatti.
        const float TIME_WRAP = 6.2831853f * 1024f;   // ~6434 s

        static NatureWind _instance;

        /// <summary>Istanza attiva, se presente. Puo' essere null.</summary>
        public static NatureWind Instance { get { return _instance; } }

        [Header("Direzione")]
        [Tooltip("Direzione del vento in world space. La componente Y viene " +
                 "ignorata: il vento orizzontale e' l'unico che conta per la " +
                 "vegetazione.")]
        public Vector3 direction = new Vector3(1f, 0f, 0.35f);

        [Header("Intensita'")]
        [Range(0f, 3f)]
        [Tooltip("Forza costante di fondo. 0.15 brezza leggera, " +
                 "0.6 vento sostenuto, 1.5 tempesta.")]
        public float strength = 0.35f;

        [Range(0f, 4f)]
        [Tooltip("Velocita' di ondeggiamento. Piu' alto = oscillazione piu' " +
                 "rapida. Sopra 2 sembra innaturale: l'inerzia delle piante " +
                 "reali e' alta.")]
        public float speed = 1f;

        [Header("Onde")]
        [Min(0.001f)]
        [Tooltip("Scala spaziale delle onde di vento, in unita' inverse. " +
                 "Valori BASSI = onde ampie che attraversano tutto il prato " +
                 "come un'increspatura sull'acqua. Valori alti = agitazione " +
                 "disordinata pianta per pianta.")]
        public float waveScale = 0.05f;

        [Range(0f, 1f)]
        [Tooltip("Quanto le onde ampie dominano rispetto al tremolio locale. " +
                 "E' il parametro che rende il prato 'vivo' invece che agitato.")]
        public float coherence = 0.65f;

        [Header("Raffiche")]
        [Min(0f)]
        [Tooltip("Raffiche al secondo in media. 0.12 = una ogni 8 secondi. " +
                 "0 disattiva.")]
        public float gustFrequency = 0.14f;

        [Min(0f)]
        [Tooltip("Ampiezza massima della raffica, sommata alla forza di base.")]
        public float gustStrength = 0.8f;

        [Tooltip("Durata minima e massima di una raffica, in secondi.")]
        public Vector2 gustDuration = new Vector2(1.2f, 3.5f);

        [Header("Avanzate")]
        [Tooltip("Usa Time.unscaledDeltaTime: il vento continua anche a gioco " +
                 "in pausa o in slow motion. Di solito e' cio' che vuoi, " +
                 "perche' un prato immobile durante un menu e' innaturale.")]
        public bool useUnscaledTime = true;

        [Tooltip("Seme delle raffiche. Con lo stesso seme la sequenza di " +
                 "folate e' identica, utile per i replay deterministici.")]
        public int gustSeed = 0;

        [Header("Debug")]
        [Tooltip("Congela il vento per ispezionare la geometria a riposo.")]
        public bool freeze = false;

        [Tooltip("Ridisegna la Scene View a ogni frame in Edit Mode. " +
                 "Spegnilo se l'editor consuma troppa CPU su scene enormi.")]
        public bool animateInEditMode = true;

        // ---- stato interno --------------------------------------------------
        float gustTimer, gustEnd, gustAmp, gustCurrent;
        float windTime;                  // tempo accumulato, non Time.time
        double lastEditorTime;           // per il delta in Edit Mode

        // Generatore PRIVATO: usare UnityEngine.Random qui sporcherebbe lo
        // stato globale a ogni raffica, rendendo non riproducibile qualunque
        // altro sistema seedato (compreso NatureScatter).
        System.Random rng;

        /// <summary>Intensita' totale corrente, base piu' raffica.</summary>
        public float CurrentStrength { get { return strength + gustCurrent; } }

        /// <summary>Direzione normalizzata e appiattita sul piano XZ.</summary>
        public Vector3 FlatDirection
        {
            get
            {
                Vector3 d = direction;
                d.y = 0f;
                if (d.sqrMagnitude < 1e-6f) return Vector3.right;
                return d.normalized;
            }
        }

        // ====================================================================
        //  VALORI DI RISERVA
        //
        //  Le globali di shader NON impostate valgono (0,0,0,0). Senza questo
        //  bootstrap, in una scena priva di NatureWind la forza sarebbe 0 e la
        //  vegetazione risulterebbe completamente immobile, non "con valori
        //  sensati" come si potrebbe supporre.
        // ====================================================================
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorBootstrap()
        {
            _instance = null;
            InstallDefaults();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeBootstrap()
        {
            // Obbligatorio con "Enter Play Mode Options / Domain Reload"
            // disattivato: le static sopravvivono all'uscita dal Play Mode.
            _instance = null;
            InstallDefaults();
        }

        static void InstallDefaults()
        {
            Vector3 d = new Vector3(1f, 0f, 0.35f).normalized;
            Shader.SetGlobalVector(ID_WindDir, new Vector4(d.x, 0f, d.z, 0f));
            Shader.SetGlobalVector(ID_WindParams, new Vector4(0.30f, 0f, 0.05f, 0.65f));
            Shader.SetGlobalVector(ID_GustParams, Vector4.zero);
        }

        // ====================================================================
        void OnEnable()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[NatureWind] Esiste gia' un NatureWind attivo (" +
                                 _instance.name + "). Ne basta uno: disattivo '" +
                                 name + "'.", this);
                enabled = false;
                return;
            }

            _instance = this;
            rng = new System.Random(gustSeed != 0
                ? gustSeed
                : unchecked(System.Environment.TickCount * 397));
            lastEditorTime = GetEditorTime();
            Push();
        }

        void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
                // Ripristina i default: senza, l'ultimo stato resta congelato
                // nelle globali e sembra un bug dello shader.
                InstallDefaults();
            }
        }

        void Update()
        {
            float dt = ComputeDeltaTime();

            if (!freeze && dt > 0f)
            {
                // Il tempo viene accumulato invece di usare Time.time perche'
                // cambiare 'speed' a runtime con Time.time provocherebbe un
                // salto istantaneo di fase: tutta la vegetazione scatterebbe.
                // Accumulando dt * speed la fase resta continua.
                windTime += dt * Mathf.Max(0.001f, speed);
                if (windTime > TIME_WRAP) windTime -= TIME_WRAP;

                UpdateGusts(dt);
            }

            Push();

#if UNITY_EDITOR
            if (!Application.isPlaying && animateInEditMode && !freeze)
            {
                // In Edit Mode Update() gira solo quando qualcosa richiede un
                // ridisegno. Senza questo, la vegetazione si muove a scatti
                // (o non si muove affatto) nella Scene View.
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        float ComputeDeltaTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                double now = GetEditorTime();
                float dt = (float)(now - lastEditorTime);
                lastEditorTime = now;
                // clamp: dopo una ricompilazione il delta puo' valere secondi
                return Mathf.Clamp(dt, 0f, 0.1f);
            }
#endif
            float d = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            return Mathf.Clamp(d, 0f, 0.1f);
        }

        static double GetEditorTime()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.timeSinceStartup;
#else
            return 0.0;
#endif
        }

        // ---- raffiche -------------------------------------------------------

        float Rand01()
        {
            if (rng == null) rng = new System.Random(12345);
            return (float)rng.NextDouble();
        }

        float RandRange(float a, float b) { return a + (b - a) * Rand01(); }

        void UpdateGusts(float dt)
        {
            if (gustFrequency <= 0f || gustStrength <= 0f)
            {
                gustCurrent = Mathf.Lerp(gustCurrent, 0f, 1f - Mathf.Exp(-2f * dt));
                if (gustCurrent < 1e-4f) gustCurrent = 0f;
                return;
            }

            gustTimer -= dt;

            if (gustTimer <= 0f)
            {
                // Distribuzione esponenziale: gli intervalli sono imprevedibili
                // e non cadono mai su un ritmo riconoscibile. Con un intervallo
                // fisso l'occhio lo individua in pochi cicli.
                float u = Mathf.Clamp(Rand01(), 0.0001f, 0.9999f);
                gustTimer = Mathf.Min(-Mathf.Log(1f - u) / gustFrequency, 120f);

                float lo = Mathf.Max(0.1f, gustDuration.x);
                float hi = Mathf.Max(lo, gustDuration.y);
                float len = Mathf.Min(RandRange(lo, hi), gustTimer * 0.75f);

                gustEnd = gustTimer - len;
                gustAmp = gustStrength * RandRange(0.4f, 1f);
            }

            bool inGust = gustTimer > gustEnd;
            float goal = inGust ? gustAmp : 0f;
            // attacco rapido, rilascio lento: e' il profilo di una folata reale
            float k = inGust ? 3.2f : 1.4f;
            gustCurrent = Mathf.Lerp(gustCurrent, goal, 1f - Mathf.Exp(-k * dt));
        }

        // ---- pubblicazione --------------------------------------------------

        void Push()
        {
            Vector3 d = FlatDirection;

            Shader.SetGlobalVector(ID_WindDir, new Vector4(d.x, 0f, d.z, 0f));

            Shader.SetGlobalVector(ID_WindParams, new Vector4(
                Mathf.Max(0f, strength),
                windTime,
                Mathf.Max(0.001f, waveScale),
                Mathf.Clamp01(coherence)));

            Shader.SetGlobalVector(ID_GustParams, new Vector4(
                Mathf.Max(0f, gustCurrent), 0f, 0f, 0f));
        }

        /// <summary>
        /// Forza una raffica immediata. Utile per agganciare il vento a un
        /// evento di gioco (un'esplosione, un elicottero che atterra).
        /// </summary>
        public void TriggerGust(float amplitude, float duration)
        {
            gustAmp = Mathf.Max(0f, amplitude);
            float len = Mathf.Max(0.1f, duration);
            gustTimer = len + 0.001f;
            gustEnd = 0.001f;
        }

        void OnValidate()
        {
            if (gustDuration.x < 0.1f) gustDuration.x = 0.1f;
            if (gustDuration.y < gustDuration.x) gustDuration.y = gustDuration.x;
            if (direction.sqrMagnitude < 1e-6f) direction = new Vector3(1f, 0f, 0.35f);

            // OnValidate puo' essere chiamato durante la deserializzazione,
            // quando toccare le API di rendering e' illegale: rimandiamo.
            if (isActiveAndEnabled && _instance == this) Push();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Vector3 d = FlatDirection;
            Vector3 p = transform.position;

            float len = 2f + CurrentStrength * 3f;

            Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.9f);
            Gizmos.DrawRay(p, d * len);
            Gizmos.DrawWireSphere(p + d * len, 0.25f);

            // punte della freccia
            Vector3 side = Vector3.Cross(d, Vector3.up) * 0.4f;
            Gizmos.DrawLine(p + d * len, p + d * (len - 0.6f) + side);
            Gizmos.DrawLine(p + d * len, p + d * (len - 0.6f) - side);

            // visualizzazione della lunghezza d'onda coerente
            Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.25f);
            float wavelength = 1f / Mathf.Max(0.001f, waveScale);
            Vector3 perp = Vector3.Cross(d, Vector3.up).normalized * 6f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = p + d * (wavelength * i);
                Gizmos.DrawLine(c - perp, c + perp);
            }
        }
#endif
    }
}