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
    [Header("Grab Tuning (fiale)")]
    [Tooltip("Distanza MINIMA della fiala dalla camera: ALZALA se la fiala copre lo schermo!")]
    public float vialMinDepth = 2.5f;
    [Tooltip("Distanza massima (deve coprire il pentolone)")]
    public float vialMaxDepth = 8f;
    [Tooltip("Sensibilita' della rotella: piu' alto = avvicinamento piu' rapido")]
    public float vialScrollSensitivity = 2f;

    void Start()
    {
        UpdateText();
        UpdatePotVisuals();
    }
    void OnEnable()
    {
        levelCompleted = false;   
                var grabber = FindFirstObjectByType<PhysicsGrabber>();
        if (grabber != null)
            grabber.ConfigureDepth(vialMinDepth, vialMaxDepth, vialScrollSensitivity);
    }


        public void CheckIngredient(IngredientID ingredient, Rigidbody rb)
    {
        if (levelCompleted) return;
        if (currentStep >= correctOrder.Length) return;
        if (ingredient == null) return;


                if (ingredient.ingredientName == correctOrder[currentStep])
        {
            Debug.Log("Correct ingredient: " + ingredient.ingredientName);

                        currentStep++;

                        UpdatePotVisuals();

                        ingredient.gameObject.SetActive(false);
            UpdateText();

                        if (currentStep == correctOrder.Length)
            {
                LevelComplete();
            }
        }
                else
        {
            Debug.Log("Wrong ingredient! Viene sputato via.");

                                    if (Time.time - lastPenaltyTime >= penaltyCooldown)
            {
                lastPenaltyTime = Time.time;
                if (GameManager.instance != null)
                {
                    GameManager.instance.DecreaseGlobalQuality(wrongAnswerPenalty);
                }
            }

            ingredient.ResetPosition(rb);
                        /*if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;                 rb.AddForce(Vector3.up * 8f + Random.onUnitSphere * 2f, ForceMode.Impulse);
            }*/
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