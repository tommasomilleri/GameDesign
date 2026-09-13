using UnityEngine;
using System.Collections; 

public class LevelSetup : MonoBehaviour
{
    [Tooltip("Trascina qui l'immagine di sfondo specifica per questo livello")]
    public Sprite backgroundForThisLevel;
    [Tooltip("Inserisci il numero di questo livello (1, 2, 3, 4, 5)")]
    public int levelIndex = 1;
    
    void OnEnable()
    {
        
        StartCoroutine(UpdateBackgroundRoutine());
    }

    IEnumerator UpdateBackgroundRoutine()
    {
        
        yield return null;

        if (BackgroundManager.instance != null && backgroundForThisLevel != null)
        {
            BackgroundManager.instance.ChangeBackground(backgroundForThisLevel);
        }
        else if (BackgroundManager.instance == null)
        {
            Debug.LogWarning("LevelSetup: BackgroundManager non trovato! Assicurati che lo script sia attaccato a background_0.");
        }
        
        if (GameManager.instance != null)
        {
            GameManager.instance.currentLevel = levelIndex;
        }
    }
}