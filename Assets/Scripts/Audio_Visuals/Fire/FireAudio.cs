
using UnityEngine;

namespace RealisticFire
{
    /// <summary>
    /// Loop del fuoco con pitch e volume modulati dall'intensita', piu'
    /// scoppiettii one-shot sincronizzati con i burst di scintille.
    /// Viene creato automaticamente da FireSource.
    /// </summary>
    [DisallowMultipleComponent]
    public class FireAudio : MonoBehaviour
    {
        [Tooltip("Loop continuo del fuoco. Opzionale. Importare Force To Mono.")]
        public AudioClip loopClip;

        [Tooltip("Scoppiettii casuali. Opzionale. Metti almeno 4 clip diverse " +
                 "altrimenti l'orecchio riconosce la ripetizione.")]
        public AudioClip[] crackleClips;

        FireProfile p;
        AudioSource loop, oneShot;
        bool built;
        Coroutine fadeRoutine;

        // evita raffiche di scoppiettii sovrapposti nello stesso istante
        float lastPopTime = -99f;
        const float MIN_POP_INTERVAL = 0.08f;

        public void Build(FireProfile profile)
        {
            if (built) return;
            built = true;

            p = profile;
            float rng = Mathf.Max(0.5f, p.audioRange);

            loop = gameObject.AddComponent<AudioSource>();
            loop.loop = true;
            loop.playOnAwake = false;
            loop.spatialBlend = 1f;                      // 3D puro
            // FIX 6: Custom senza SetCustomCurve usa una curva lineare di
            // default, non quella attesa. Logarithmic e' il comportamento
            // fisicamente corretto e non richiede curve aggiuntive.
            loop.rolloffMode = AudioRolloffMode.Logarithmic;
            loop.minDistance = rng * 0.15f;
            loop.maxDistance = rng;
            loop.volume = p.loopVolume;
            loop.pitch = p.loopPitch;
            loop.dopplerLevel = 0f;                      // il fuoco non si muove

            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            oneShot.spatialBlend = 1f;
            oneShot.rolloffMode = AudioRolloffMode.Logarithmic;
            oneShot.minDistance = rng * 0.20f;
            oneShot.maxDistance = rng * 1.20f;
            oneShot.dopplerLevel = 0f;

            if (loopClip != null)
            {
                loop.clip = loopClip;
                // parte da un punto casuale: due fuochi vicini non vanno in fase
                loop.time = Random.Range(0f, Mathf.Max(0.01f, loopClip.length - 0.01f));
                loop.Play();
            }
        }

        public void Tick(float heat)
        {
            if (!built || loop == null || loop.clip == null) return;
            if (fadeRoutine != null) return;   // FIX: non contrastare il fade

            heat = Mathf.Clamp01(heat);
            loop.volume = p.loopVolume * Mathf.Lerp(0.6f, 1.2f, heat);
            loop.pitch = p.loopPitch * Mathf.Lerp(0.94f, 1.06f, heat);
        }

        public void Pop(float power)
        {
            if (!built || oneShot == null) return;
            if (p.crackleVolume <= 0f) return;           // la candela non scoppietta
            if (crackleClips == null || crackleClips.Length == 0) return;
            if (Time.time - lastPopTime < MIN_POP_INTERVAL) return;

            AudioClip c = crackleClips[Random.Range(0, crackleClips.Length)];
            if (c == null) return;

            lastPopTime = Time.time;
            oneShot.pitch = Random.Range(0.82f, 1.25f);

            float vol = p.crackleVolume * Mathf.Clamp01(power) * Random.Range(0.7f, 1f);
            oneShot.PlayOneShot(c, Mathf.Clamp01(vol));
        }

        public void FadeOutAndStop(float seconds)
        {
            if (!built || loop == null) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(seconds));
        }

        /// <summary>
        /// FIX 1: Build() ritornava subito se built era gia' true, quindi
        /// Relight() non riavviava mai l'audio dopo un FadeOutAndStop.
        /// Questo metodo riporta il loop in riproduzione.
        /// </summary>
        public void Restart()
        {
            if (!built || loop == null) return;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            loop.volume = p.loopVolume;
            loop.pitch = p.loopPitch;

            if (loop.clip != null && !loop.isPlaying)
            {
                loop.time = Random.Range(0f, Mathf.Max(0.01f, loop.clip.length - 0.01f));
                loop.Play();
            }
        }

        System.Collections.IEnumerator FadeRoutine(float seconds)
        {
            float v0 = loop.volume;
            float t = 0f;
            seconds = Mathf.Max(0.01f, seconds);

            while (t < seconds)
            {
                t += Time.deltaTime;
                loop.volume = Mathf.Lerp(v0, 0f, t / seconds);
                yield return null;
            }

            loop.volume = 0f;
            loop.Stop();
            fadeRoutine = null;
        }
    }
}

