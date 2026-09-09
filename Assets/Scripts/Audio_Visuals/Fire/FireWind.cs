using UnityEngine;

namespace RealisticFire
{
    /// <summary>
    /// Vento globale condiviso. Si auto-crea al primo accesso.
    /// Tutti i fuochi leggono da qui: le raffiche sono coerenti in scena.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Effects/Fire Wind")]
    public class FireWind : MonoBehaviour
    {
        // ---- singleton ------------------------------------------------------
        static FireWind _i;
        static bool _quitting;

        // FIX 8: con "Enter Play Mode Options" e domain reload disattivato i
        // campi statici sopravvivono tra una sessione di Play e l'altra.
        // Senza questo reset al secondo Play _quitting resta true e il vento
        // non si crea piu'.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _i = null;
            _quitting = false;
        }

        public static FireWind I
        {
            get
            {
                if (_quitting) return null;
                if (!Application.isPlaying) return _i;   // FIX: mai creare GO in editor

                if (_i == null)
                {
#if UNITY_2022_2_OR_NEWER
                    _i = Object.FindFirstObjectByType<FireWind>();
#else
                    _i = Object.FindObjectOfType<FireWind>();
#endif
                    if (_i == null)
                    {
                        GameObject go = new GameObject("~FireWind");
                        _i = go.AddComponent<FireWind>();
                    }
                }
                return _i;
            }
        }

        /// <summary>Vento sicuro anche se il singleton non esiste.</summary>
        public static Vector3 SafeCurrent
        {
            get
            {
                FireWind w = I;
                return (w == null) ? Vector3.zero : w.Current;
            }
        }

        /// <summary>Intensita' sicura 0..1.</summary>
        public static float SafeStrength
        {
            get
            {
                FireWind w = I;
                return (w == null) ? 0f : w.Strength;
            }
        }

        // ---- parametri ------------------------------------------------------
        [Header("Vento base")]
        [Tooltip("Direzione in world space. Viene normalizzata.")]
        public Vector3 direction = new Vector3(1f, 0f, 0.3f);

        [Range(0f, 1f)]
        [Tooltip("Vento costante di fondo. 0.02 interno chiuso, 0.45 esterno esposto.")]
        public float baseStrength = 0.12f;

        [Header("Raffiche")]
        [Tooltip("Raffiche al secondo in media. 0.15 = una ogni 7 secondi. 0 = off.")]
        [Min(0f)] public float gustFrequency = 0.15f;
        [Min(0f)] public float gustStrength = 0.75f;
        [Tooltip("Durata minima e massima di una raffica, in secondi.")]
        public Vector2 gustDuration = new Vector2(0.8f, 3.0f);

        [Header("Turbolenza")]
        [Range(0f, 1f)]
        [Tooltip("Quanto la direzione ruota nel tempo. 0.35 = piu' o meno 31 gradi.")]
        public float turbulence = 0.35f;
        [Min(0f)] public float turbulenceSpeed = 0.5f;

        // ---- output ---------------------------------------------------------
        /// <summary>Vettore vento corrente in world space. Magnitudine 0..1.</summary>
        public Vector3 Current { get; private set; }
        /// <summary>Intensita' 0..1, comoda per modulare audio o particelle.</summary>
        public float Strength { get; private set; }

        // ---- stato interno --------------------------------------------------
        float gustTimer, gustLen, gustAmp, gustCur, gustEnd;
        float seed;
        Vector3 dirNorm = Vector3.right;

        void Awake()
        {
            if (_i != null && _i != this)
            {
                Debug.LogWarning("[FireWind] Trovato un secondo FireWind. Ne basta uno: " +
                                 "disattivo '" + name + "'.", this);
                enabled = false;
                return;
            }

            _i = this;
            _quitting = false;
            seed = Random.Range(0f, 1000f);
            NormalizeDirection();

            // stato iniziale coerente gia' al primo frame
            Strength = Mathf.Clamp01(baseStrength);
            Current = dirNorm * Strength;
        }

        void OnDestroy()
        {
            if (_i == this) _i = null;
        }

        void OnApplicationQuit()
        {
            _quitting = true;
        }

        void NormalizeDirection()
        {
            dirNorm = (direction.sqrMagnitude < 0.0001f)
                ? Vector3.right
                : direction.normalized;
        }

        void OnValidate()
        {
            if (gustDuration.x < 0.05f) gustDuration.x = 0.05f;
            if (gustDuration.y < gustDuration.x) gustDuration.y = gustDuration.x;
            NormalizeDirection();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float t = Time.time;

            // ---- raffiche ---------------------------------------------------
            // Intervalli con distribuzione esponenziale: mai ritmici.
            if (gustFrequency > 0f)
            {
                gustTimer -= dt;

                if (gustTimer <= 0f)
                {
                    // FIX: Random.value puo' valere esattamente 0, mai 1, quindi
                    // 1 - value non e' mai 0 e il log non diverge. Comunque
                    // clampiamo l'intervallo per non avere attese assurde.
                    float u = Mathf.Clamp(Random.value, 0.0001f, 0.9999f);
                    gustTimer = Mathf.Min(-Mathf.Log(1f - u) /
                                          Mathf.Max(0.001f, gustFrequency), 120f);
                    gustLen = Random.Range(gustDuration.x, gustDuration.y);
                    gustLen = Mathf.Min(gustLen, gustTimer * 0.8f);
                    gustAmp = gustStrength * Random.Range(0.35f, 1f);
                    gustEnd = gustTimer - gustLen;
                }

                bool inGust = gustTimer > gustEnd;
                float goal = inGust ? gustAmp : 0f;
                // attacco rapido, coda lenta: profilo di una folata reale
                float k = inGust ? 4.0f : 1.8f;
                gustCur = Mathf.Lerp(gustCur, goal, 1f - Mathf.Exp(-k * dt));
            }
            else
            {
                gustCur = Mathf.Lerp(gustCur, 0f, 1f - Mathf.Exp(-2f * dt));
            }

            // ---- turbolenza: la direzione ruota lentamente -------------------
            float yaw = (Mathf.PerlinNoise(t * turbulenceSpeed, seed) - 0.5f)
                        * 2f * turbulence * 90f;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * dirNorm;

            Strength = Mathf.Clamp01(baseStrength + gustCur);
            Current = dir * Strength;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Vector3 d = (direction.sqrMagnitude < 0.0001f)
                ? Vector3.right : direction.normalized;
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            Gizmos.DrawRay(transform.position, d * 2f);
            Gizmos.DrawWireSphere(transform.position + d * 2f, 0.15f);
        }
#endif
    }
}
