using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level3Manager : MonoBehaviour
{
    [Header("Recipe Settings")]
    [SerializeField] private string[] correctOrder = { "Starter Culture", "Rennet", "Salt", "Annatto" };
    private int currentStep = 0;

    [Header("Pot Visuals")]
    public Image potImageComponent;
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

        // Assicura che la pentola inizi vuota
        if (potImageComponent != null && potSprites.Length > 0)
        {
            potImageComponent.sprite = potSprites[0];
        }
    }

    public void CheckIngredient(DraggableIngredient ingredient)
    {
        if (currentStep >= correctOrder.Length) return;
        if (ingredient == null) return;

        // --- SE L'INGREDIENTE È CORRETTO ---
        if (ingredient.ingredientName == correctOrder[currentStep])
        {
            Debug.Log("Correct ingredient!");

            // 1. Aumenta il contatore SOLO se hai indovinato!
            currentStep++;

            // 2. Magia Visiva: aggiorna l'immagine fermandosi al marrone
            if (potImageComponent != null && potSprites.Length > 0)
            {
                int spriteIndex = Mathf.Min(currentStep, potSprites.Length - 1);
                potImageComponent.sprite = potSprites[spriteIndex];
            }

            // 3. Fai sparire il barattolo usato e aggiorna il testo
            ingredient.gameObject.SetActive(false);
            UpdateText();

            // 4. Controlla la vittoria
            if (currentStep == correctOrder.Length)
            {
                LevelComplete();
            }
        }
        // --- SE L'INGREDIENTE È SBAGLIATO ---
        else
        {
            Debug.Log("Wrong ingredient! Quality drops.");

            // Penalizza solo la qualità globale, il barattolo tornerà al suo posto da solo!
            if (GameManager.instance != null)
            {
                GameManager.instance.DecreaseGlobalQuality(wrongAnswerPenalty);
            }
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
        Debug.Log("LEVEL 3 COMPLETE! The cheese is ready for the cellar.");
        GoToNextLevel();
    }

    void GoToNextLevel()
    {
        if (NextLevel != null) NextLevel.SetActive(true);
        if (CurrentLevel != null) CurrentLevel.SetActive(false);
    }
}