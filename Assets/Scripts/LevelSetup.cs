using UnityEngine;
using System.Collections; // Aggiunto per poter usare le Coroutine!

public class LevelSetup : MonoBehaviour
{
    [Tooltip("Trascina qui l'immagine di sfondo specifica per questo livello")]
    public Sprite backgroundForThisLevel;
    [Tooltip("Inserisci il numero di questo livello (1, 2, 3, 4, 5)")]
    public int levelIndex = 1;
    // OnEnable scatta automaticamente quando questo GameObject viene acceso
    void OnEnable()
    {
        // Facciamo partire una Coroutine per posticipare l'azione di un frame
        StartCoroutine(UpdateBackgroundRoutine());
    }

    IEnumerator UpdateBackgroundRoutine()
    {
        // ASPETTA 1 FRAME: Diamo il tempo al BackgroundManager di svegliarsi!
        yield return null;

        if (BackgroundManager.instance != null && backgroundForThisLevel != null)
        {
            BackgroundManager.instance.ChangeBackground(backgroundForThisLevel);
        }
        else if (BackgroundManager.instance == null)
        {
            Debug.LogWarning("LevelSetup: BackgroundManager non trovato! Assicurati che lo script sia attaccato a background_0.");
        }
        // Diciamo al GameManager in che livello ci troviamo per le statistiche!
        if (GameManager.instance != null)
        {
            GameManager.instance.currentLevel = levelIndex;
        }
    }
}