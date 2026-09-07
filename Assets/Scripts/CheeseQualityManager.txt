using UnityEngine;

public class CheeseQualityManager : MonoBehaviour
{
    // Singleton instance so any script can access this without Inspector references
    public static CheeseQualityManager Instance { get; private set; }

    [Header("Cheese Quality Settings")]
    public QualityBar qualityBar;
    public int maxQuality = 100;

    // Kept public so other scripts can read it, but it's no longer static
    public int currentQuality;

    void Awake()
    {
        // Set up the Singleton
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment this if you start using multiple Unity Scenes later!
        }
        else
        {
            Destroy(gameObject);
            return; // Interrompe l'esecuzione se questo è un clone da distruggere
        }
    }

    void Start()
    {
        // Initialize the game
        ResetGame();

        // --- LA CORREZIONE FONDAMENTALE ---
        // Spegne forzatamente l'oggetto (bordo + riempimento) appena avvii il gioco.
        // Sarà il MenuManager a riaccenderlo con SetActive(true) quando clicchi su Player 2!
        if (qualityBar != null)
        {
            qualityBar.gameObject.SetActive(false);
        }
    }

    public void DecreaseQuality(int damage)
    {
        currentQuality -= damage;

        if (currentQuality <= 0) // Meglio usare <= 0 per evitare bug se il danno supera la vita residua
        {
            currentQuality = 0;
        }

        if (qualityBar != null)
        {
            qualityBar.SetQuality(currentQuality);
        }

        if (currentQuality == 0)
        {
            CheeseSpoiled();
        }
    }

    void CheeseSpoiled()
    {
        Debug.Log("The cheese went bad! A Mold Monster is born!");
        // Add your Game Over logic here

        // OPZIONALE: Se vuoi che la barra sparisca quando perdi, togli il commento alla riga sotto
        // if (qualityBar != null) qualityBar.gameObject.SetActive(false);
    }

    public void ResetGame()
    {
        currentQuality = maxQuality;

        if (qualityBar != null)
        {
            qualityBar.SetMaxQuality(maxQuality);
            qualityBar.SetQuality(currentQuality);
        }
    }
}