using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level3Manager : MonoBehaviour
{
    [Header("Recipe Settings")]
    [SerializeField] private string[] correctOrder = { "Starter Culture", "Rennet", "Salt", "Annatto" };
    private int currentStep = 0;
    private bool levelCompleted = false;

    [Tooltip("Secondi di grazia dopo un errore: la fiala respinta che ricade nel trigger non ri-penalizza")]
    public float penaltyCooldown = 1.5f;
    private float lastPenaltyTime = -99f;


    [Header("Pot Visuals (Scegli uno dei due)")]
    [Tooltip("Usa questo se la pentola � in un Canvas3D")]
    public Image potImageComponent;
    [Tooltip("Usa questo se la pentola � un normale sprite nel mondo 3D")]
    public SpriteRenderer potSpriteRenderer;
    public Sprite[] potSprites;

    [Header("UI & Level Management")]
    public TMP_Text orderText;
    public GameObject NextLevel;
    public GameObject CurrentLevel;

    [Header("Penalty Settings")]
    public int wrongAnswerPenalty = 20;

    void Start()
    {
        UpdateText();
        UpdatePotVisuals();
    }
    void OnEnable()
    {
        levelCompleted = false;   // reset per il Replay
    }

    // --- NUOVA FIRMA FISICA: Usa l'ID e il Rigidbody 3D ---
    public void CheckIngredient(IngredientID ingredient, Rigidbody rb)
    {
        if (levelCompleted) return;
        if (currentStep >= correctOrder.Length) return;
        if (ingredient == null) return;


        // --- SE L'INGREDIENTE � CORRETTO ---
        if (ingredient.ingredientName == correctOrder[currentStep])
        {
            Debug.Log("Correct ingredient: " + ingredient.ingredientName);

            // 1. Aumenta il contatore
            currentStep++;

            // 2. Magia Visiva: aggiorna l'immagine della pentola
            UpdatePotVisuals();

            // 3. Fai sparire la boccetta usata e aggiorna il testo
            ingredient.gameObject.SetActive(false);
            UpdateText();

            // 4. Controlla la vittoria
            if (currentStep == correctOrder.Length)
            {
                LevelComplete();
            }
        }
        // --- SE L'INGREDIENTE � SBAGLIATO ---
        else
        {
            Debug.Log("Wrong ingredient! Viene sputato via.");

            // Cooldown: la stessa fiala che rimbalza e ricade nel
            // trigger non deve mitragliare penalita' (-20,-20,-20...)
            if (Time.time - lastPenaltyTime >= penaltyCooldown)
            {
                lastPenaltyTime = Time.time;
                if (GameManager.instance != null)
                {
                    GameManager.instance.DecreaseGlobalQuality(wrongAnswerPenalty);
                }
            }


            // Effetto Rimbalzo Fisico: la pentola lo respinge in aria!
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Ferma la caduta
                rb.AddForce(Vector3.up * 8f + Random.onUnitSphere * 2f, ForceMode.Impulse);
            }
        }
    }

    void UpdatePotVisuals()
    {
        if (potSprites != null && potSprites.Length > 0)
        {
            int spriteIndex = Mathf.Min(currentStep, potSprites.Length - 1);

            if (potImageComponent != null)
                potImageComponent.sprite = potSprites[spriteIndex];

            if (potSpriteRenderer != null)
                potSpriteRenderer.sprite = potSprites[spriteIndex];
        }
    }

    void UpdateText()
    {
        if (orderText != null)
        {
            orderText.text = "Added: " + currentStep + " / " + correctOrder.Length;
        }
    }

    void LevelComplete()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        Debug.Log("LEVEL 3 COMPLETE! The cheese is ready for the cellar.");
        GoToNextLevel();
    }


    void GoToNextLevel()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Usa il ponte universale del GameManager per le transizioni
        if (GameManager.instance != null)
        {
            GameManager.instance.TransitionToNextLevel(CurrentLevel, NextLevel);
        }
        else
        {
            if (NextLevel != null) NextLevel.SetActive(true);
            if (CurrentLevel != null) CurrentLevel.SetActive(false);
        }
    }
}