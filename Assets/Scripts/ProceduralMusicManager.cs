using UnityEngine;
using System.Collections; // Necessario per le Coroutine

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))] // <-- AGGIUNTO: Unity metterà il filtro da solo!
public class ProceduralMusicManager : MonoBehaviour
{
    public static ProceduralMusicManager instance;

    [Header("Music Settings")]
    [Tooltip("Trascina qui il tuo file .wav o .mp3")]
    public AudioClip backgroundTrack;

    [Tooltip("Quanti secondi ci mette la musica ad arrivare al massimo volume? (Fade-In)")]
    public float fadeInDuration = 4f;

    [Tooltip("Il volume massimo desiderato (da 0 a 1)")]
    [Range(0f, 1f)]
    public float targetVolume = 0.4f;

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter; // <-- AGGIUNTO: Il nostro filtro per la muffa

    void Awake()
    {
        // Singleton pattern per non interrompere la musica al cambio livello
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();

        // Imposta la frequenza aperta (suono limpido normale) all'avvio
        lowPassFilter.cutoffFrequency = 22000f;

        if (backgroundTrack != null)
        {
            audioSource.clip = backgroundTrack;
            audioSource.loop = true;
            audioSource.playOnAwake = false; // Lo gestiamo noi via codice!

            // Impostiamo il volume a 0 per preparare il Fade-In
            audioSource.volume = 0f;

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                // Facciamo partire la magia dell'ingresso graduale
                StartCoroutine(FadeInMusic());
            }
        }
        else
        {
            Debug.LogWarning("MUSIC ERROR: Nessuna traccia audio inserita!");
        }
    }

    void Start()
    {
        // Avvia il controllo perenne della muffa (Fase F2)
        StartCoroutine(MusicQualityRoutine());
    }

    // Coroutine che alza dolcemente il volume nel tempo
    IEnumerator FadeInMusic()
    {
        float currentTime = 0;

        while (currentTime < fadeInDuration)
        {
            currentTime += Time.deltaTime;
            // Calcola la sfumatura morbida da 0 al volume massimo
            audioSource.volume = Mathf.Lerp(0f, targetVolume, currentTime / fadeInDuration);
            yield return null;
        }

        // Assicuriamoci che arrivi esattamente al volume target alla fine
        audioSource.volume = targetVolume;
    }

    // --- NUOVA COROUTINE: REAZIONE ALLA MUFFA ---
    private IEnumerator MusicQualityRoutine()
    {
        while (true)
        {
            if (GameManager.instance != null)
            {
                int quality = GameManager.instance.currentQuality;

                // La magia: sotto 80% inizia a chiudersi, sotto 40% è ovattata, a 0 è un incubo sordo
                float targetCutoff = quality < 40 ? 800f : (quality < 80 ? 4000f : 22000f);

                // Applica un Lerp per fare in modo che la transizione audio sia super fluida e mai a scatti
                lowPassFilter.cutoffFrequency = Mathf.Lerp(
                    lowPassFilter.cutoffFrequency,
                    targetCutoff,
                    Time.unscaledDeltaTime * 1.5f
                );
            }
            yield return new WaitForSecondsRealtime(0.5f); // Controlla ogni mezzo secondo
        }
    }
    // --- FUNZIONI DI PAUSA ---
    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause(); // Mette in pausa senza azzerare il tempo!
        }
    }

    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause(); // Riprende esattamente da dove era rimasta
        }
    }
}