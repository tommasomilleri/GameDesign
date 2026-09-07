using UnityEngine;

public class Level1Manager : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int correctImage;
    public int wrongAnswerPenalty = 20;

    public GameObject NextLevel;
    public GameObject CurrentLevel;
    public GameObject canvas;

    void Start()
    {
        if (GameManager.instance != null)
        {
            if (GameManager.instance.qualityBarContainer != null)
                GameManager.instance.qualityBarContainer.SetActive(true);
            if (GameManager.instance.globalQualityBar != null)
                GameManager.instance.globalQualityBar.gameObject.SetActive(true);
        }
        // COLLEGAMENTO VIA CODICE: bypassa gli OnClick serializzati corrotti
        if (CurrentLevel != null)
        {
            UnityEngine.UI.Button[] cows = CurrentLevel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (int i = 0; i < cows.Length; i++)
            {
                int num = i + 1;
                cows[i].onClick.AddListener(delegate { selectImage(num); });
            }
            Debug.Log("MUCCHE COLLEGATE VIA CODICE: " + cows.Length);
        }
    }
    // Triggered by the UI Buttons
    public void selectImage(int imageNumber)
    {
        if (imageNumber == correctImage)
        {
            Debug.Log("Correct choice! The cheesemaking process continues.");
            // Selezionando l'oggetto appena cliccato
            GameObject clickedCow = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (clickedCow != null)
            {
                StartCoroutine(JumpJoy(clickedCow.GetComponent<RectTransform>()));
            }
            else
            {
                GoToNextLevel(); // Fallback se qualcosa va storto
            }
        }
        else
        {
            Debug.Log("Oh no! Wrong ingredient, cheese quality drops.");

            // Chiama il GameManager globale invece del CheeseQualityManager!
            if (GameManager.instance != null)
            {
                GameManager.instance.DecreaseGlobalQuality(wrongAnswerPenalty);
            }
            else
            {
                Debug.LogWarning("GameManager is missing from the scene!");
            }
        }
    }

    void GoToNextLevel()
    {
        // Sblocca il cursore per sicurezza
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // ECCO IL COLLEGAMENTO AL PONTE UNIVERSALE!
        if (GameManager.instance != null)
        {
            GameManager.instance.TransitionToNextLevel(CurrentLevel, NextLevel);
        }
        else
        {
            // Fallback di emergenza
            if (NextLevel != null) NextLevel.SetActive(true);
            if (CurrentLevel != null) CurrentLevel.SetActive(false);
        }
    }
    private System.Collections.IEnumerator JumpJoy(RectTransform target)
    {
        if (target == null) yield break;
        Vector2 originalPos = target.anchoredPosition;
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Equazione per un salto morbido (ease out bounce)
            float yOffset = Mathf.Sin(t * Mathf.PI) * 20f;
            target.anchoredPosition = originalPos + new Vector2(0, yOffset);
            yield return null;
        }
        target.anchoredPosition = originalPos;
        GoToNextLevel(); // Finisce il salto e cambia livello!
    }

    // Aggiungi questa chiamata se il giocatore sbaglia mucca:
    // StartCoroutine(HeadShake(clickedCow.GetComponent<RectTransform>()));
    private System.Collections.IEnumerator HeadShake(RectTransform target)
    {
        if (target == null) yield break;
        Quaternion originalRot = target.localRotation;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            // Oscillazione smorzata (3 colpi)
            float zOffset = Mathf.Sin(elapsed * 30f) * 6f * (1f - (elapsed / duration));
            target.localRotation = originalRot * Quaternion.Euler(0, 0, zOffset);
            yield return null;
        }
        target.localRotation = originalRot;
    }
}