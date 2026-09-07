using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements!

public class EndingManager : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Drag the TextMeshPro UI element here")]
    public TextMeshProUGUI storyText;
    [Tooltip("Testo che mostrerà il riepilogo della partita")]
    public TextMeshProUGUI statsText;

    [Header("Story Texts")]
    [TextArea(4, 8)]
    public string highEnding = "At long last, the Grand Fromagerie has yielded its ultimate masterpiece. Through perfect alchemy and unbreakable fellowship, you have forged a cheese so divine it has awakened the dormant strength of our kin. We are no longer mere shadows scurrying in the dark! Armed with the legendary recipe, the rats march back to the sunlit surface to overthrow their oppressors. The greedy humans, who for generations hoarded the dairy treasures of the world, can only watch in awe and despair as you save the cheese empire from human greed. A new golden age has begun!";

    [TextArea(4, 8)]
    public string mediumEnding = "The vats have cooled, and the final wheel rests upon the cellar shelves, yet a vital spark is missing. You have managed to salvage fragments of the ancient recipe, but it is not quite finished. It is a noble effort that will sustain our kin, but it lacks the magic required to break our chains. Because the cheese is imperfect, the humans remain in power above, their iron grip on the surface unbroken. We must survive in the shadows, sharing our hard-won secrets in whispers, waiting for a future generation to finally complete the great work.";

    [TextArea(4, 8)]
    public string lowEnding = "A foul stench rises from the vat, a putrid testament to your absolute failure. The milk has soured, the sacred temperatures were forsaken, and human greed takes over. From the ruined curds, a vile and ancient evil awakens: the mold monster is born! The Queen Rat, gazing upon the corrupted remains of our once-proud empire, has pronounced her final, devastating judgment. You are stripped of your apron and exiled into the cold, unforgiving wilds. The cheese empire crumbles into toxic rot, lost to the shadows forever.";

    // L'uso di OnEnable garantisce che la pagella si compili nel momento esatto in cui appare la UI
    void OnEnable()
    {
        // Blocca immediatamente il menu di pausa durante la scena finale
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.canPause = false;
        }

        // Safety check to ensure the global GameManager exists
        if (GameManager.instance != null)
        {
            int quality = GameManager.instance.currentQuality;

            // High Quality (80 to 100)
            if (quality >= 80)
            {
                // IL TOCCO FINALE: La parola d'ordine "CASEUS" per il Player 2!
                storyText.text = highEnding + "\n\nSpeak the maker's word to your partner: «CASEUS»";
            }
            // Medium Quality (40 to 79)
            else if (quality >= 40)
            {
                storyText.text = mediumEnding;
            }
            // Low Quality (below 40)
            else
            {
                storyText.text = lowEnding;
            }

            // Appena questa scena si attiva, dice al GameManager globale di nascondere la barra del formaggio
            if (GameManager.instance.qualityBarContainer != null)
            {
                GameManager.instance.qualityBarContainer.SetActive(false);
            }

            // --- STAMPA DELLA PAGELLA ---
            if (statsText != null)
            {
                int totalErrors = 0;
                string errorDetails = "";

                // Calcola gli errori totali
                for (int i = 1; i <= 5; i++)
                {
                    int errs = GameManager.instance.errorsPerLevel[i];
                    totalErrors += errs;
                    errorDetails += $"L{i}:{errs}  ";
                }

                // Calcola il tempo formattato (Minuti:Secondi)
                float totalTime = Time.time - GameManager.instance.runStartTime;
                int minutes = Mathf.FloorToInt(totalTime / 60F);
                int seconds = Mathf.FloorToInt(totalTime - minutes * 60);
                string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

                // Determina il Grado
                string grade = quality >= 80 ? "DIVINO" : (quality >= 40 ? "STAGIONATO" : "FRESCO");

                // Scrive il testo a schermo!
                statsText.text = $"Quality: {quality}/100 — Grade: {grade}\n" +
                                 $"Time: {timeString}\n" +
                                 $"Errors — {errorDetails} (Total: {totalErrors})\n\n" +
                                 $"Errors caused by miscommunication: {totalErrors}"; // Cattiveria co-op ;)
            }
        }
        else
        {
            Debug.LogWarning("GameManager not found! Start the game from the first scene to test.");
        }
    }
}