
using UnityEngine;

namespace Atmosphere
{
    /// <summary>
    /// Controllo globale della qualita' atmosferica.
    ///
    /// Tutti i volumi moltiplicano la propria densita' per DensityScale, cosi'
    /// un unico slider nelle opzioni grafiche regola l'intero costo
    /// atmosferico della scena senza toccare i singoli componenti.
    ///
    /// E' una classe statica: non serve metterla in scena. Chiamala da un
    /// menu opzioni, per esempio:
    ///     AtmosphereQuality.SetPreset(AtmosphereQuality.Preset.Low);
    /// </summary>
    public static class AtmosphereQuality
    {
        public enum Preset { Off, Low, Medium, High, Ultra }

        static float _densityScale = 1f;
        static bool _softParticles = true;

        /// <summary>Evento emesso quando la qualita' cambia. I volumi si iscrivono.</summary>
        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _densityScale = 1f;
            _softParticles = true;
            Changed = null;      // altrimenti restano iscritti oggetti distrutti
        }

        /// <summary>Moltiplicatore globale di densita'. 0 spegne tutto.</summary>
        public static float DensityScale
        {
            get { return _densityScale; }
            set
            {
                float v = Mathf.Clamp(value, 0f, 4f);
                if (Mathf.Approximately(v, _densityScale)) return;
                _densityScale = v;
                if (Changed != null) Changed();
            }
        }

        /// <summary>
        /// Le soft particles richiedono la Depth Texture. Su piattaforme dove
        /// non e' disponibile o costa troppo, disattivale qui.
        /// </summary>
        public static bool SoftParticles
        {
            get { return _softParticles; }
            set
            {
                if (v_eq(value, _softParticles)) return;
                _softParticles = value;
                if (Changed != null) Changed();
            }
        }

        static bool v_eq(bool a, bool b) { return a == b; }

        public static void SetPreset(Preset p)
        {
            switch (p)
            {
                case Preset.Off: _densityScale = 0f; _softParticles = false; break;
                case Preset.Low: _densityScale = 0.35f; _softParticles = false; break;
                case Preset.Medium: _densityScale = 0.65f; _softParticles = true; break;
                case Preset.High: _densityScale = 1f; _softParticles = true; break;
                case Preset.Ultra: _densityScale = 1.6f; _softParticles = true; break;
            }
            if (Changed != null) Changed();
        }
    }
}

