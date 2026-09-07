using UnityEngine;

public class Level1Manager : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int correctImage;
    public int wrongAnswerPenalty = 20;

    public GameObject NextLevel;
    public GameObject CurrentLevel;
    public GameObject canvas; // Lasciato per retrocompatibilità

    [Header("3D Setup")]
    public GameObject lastClickedCow; // Sostituisce l'EventSystem: memorizza quale mucca 3D è stata cliccata

    void Start()
    {
        // Riaccende la barra della qualità (logica intatta!)
        if (GameManager.instance != null)
        {
            if (GameManager.instance.qualityBarContainer != null)
                GameManager.instance.qualityBarContainer.SetActive(true);
            if (GameManager.instance.globalQualityBar != null)
                GameManager.instance.globalQualityBar.gameObject.SetActive(true);
        }

        // HO ELIMINATO il blocco che cercava gli "UnityEngine.UI.Button". 
        // Ora il collegamento avviene tramite i nostri nuovi script 3D (CowRef e Clickable3D).
    }

    // --- NUOVO: Helper per ricevere il click dal mondo 3D ---
    public void SelectCow(GameObject cow, int n)
    {
        lastClickedCow = cow;
        selectImage(n);
    }

    public void selectImage(int imageNumber)
    {
        if (imageNumber == correctImage)
        {
            Debug.Log("Correct choice! The cheesemaking process continues.");

            if (lastClickedCow != null)
            {
                // Avvia l'animazione di vittoria passando il Transform 3D
                StartCoroutine(JumpJoy(lastClickedCow.transform));
            }
            else
            {
                GoToNextLevel(); // Fallback
            }
        }
        else
        {
            Debug.Log("Oh no! Wrong ingredient, cheese quality drops.");

            if (lastClickedCow != null)
            {
                // Avvia l'animazione di errore sul modello 3D
                StartCoroutine(HeadShake(lastClickedCow.transform));
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.DecreaseGlobalQuality(wrongAnswerPenalty);
            }
        }
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

    // ==========================================
    // ANIMAZIONI CONVERTITE AL 3D (Transform invece di RectTransform)
    // ==========================================
    private System.Collections.IEnumerator JumpJoy(Transform target)
    {
        if (target == null) yield break;
        Vector3 originalPos = target.position;
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float yOffset = Mathf.Sin(t * Mathf.PI) * 20f;

            // Scaliamo il salto per il mondo 3D (moltiplichiamo per 0.02f), 
            // altrimenti la mucca salta alta 20 metri!
            target.position = originalPos + new Vector3(0, yOffset * 0.02f, 0);
            yield return null;
        }
        target.position = originalPos;
        GoToNextLevel();
    }

    private System.Collections.IEnumerator HeadShake(Transform target)
    {
        if (target == null) yield break;
        Quaternion originalRot = target.localRotation;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float zOffset = Mathf.Sin(elapsed * 30f) * 6f * (1f - (elapsed / duration));
            target.localRotation = originalRot * Quaternion.Euler(0, 0, zOffset);
            yield return null;
        }
        target.localRotation = originalRot;
    }
}