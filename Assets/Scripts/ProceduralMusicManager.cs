using UnityEngine;
using System.Collections; 

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))] 
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
    private AudioLowPassFilter lowPassFilter; 

    void Awake()
    {
        
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

        
        lowPassFilter.cutoffFrequency = 22000f;

        if (backgroundTrack != null)
        {
            audioSource.clip = backgroundTrack;
            audioSource.loop = true;
            audioSource.playOnAwake = false; 

            
            audioSource.volume = 0f;

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                
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
        
        StartCoroutine(MusicQualityRoutine());
    }

    
    IEnumerator FadeInMusic()
    {
        float currentTime = 0;

        while (currentTime < fadeInDuration)
        {
            currentTime += Time.deltaTime;
            
            audioSource.volume = Mathf.Lerp(0f, targetVolume, currentTime / fadeInDuration);
            yield return null;
        }

        
        audioSource.volume = targetVolume;
    }

    
    private IEnumerator MusicQualityRoutine()
    {
        while (true)
        {
            if (GameManager.instance != null)
            {
                int quality = GameManager.instance.currentQuality;

                
                float targetCutoff = quality < 40 ? 800f : (quality < 80 ? 4000f : 22000f);

                
                lowPassFilter.cutoffFrequency = Mathf.Lerp(
                    lowPassFilter.cutoffFrequency,
                    targetCutoff,
                    Time.unscaledDeltaTime * 1.5f
                );
            }
            yield return new WaitForSecondsRealtime(0.5f); 
        }
    }
    
    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause(); 
        }
    }

    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause(); 
        }
    }
}