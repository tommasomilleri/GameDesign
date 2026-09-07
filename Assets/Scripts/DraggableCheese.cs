using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections; // Needed for the falling Coroutine

public class DraggableCheese : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Canvas rootCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    [Header("Invisible Shelves Targets")]
    public RectTransform[] shelves;
    public float shelfTolerance = 50f;

    [Tooltip("Increase this number to push the cheese UP so it sits perfectly ON TOP of the white rectangle")]
    public float yOffset = 80f; // <-- Use this in the Inspector to fix the visual overlap!

    [Header("Environmental Targets")]
    public RectTransform fanObject;
    public RectTransform heaterObject;
    public RectTransform herbsObject;

    [Header("Winning Tolerances (Distance in Pixels)")]
    public float fanMinDistance = 100f;
    public float fanMaxDistance = 300f;

    public float heaterMinDistance = 400f;
    public float heaterMaxDistance = 800f;

    private bool isFalling = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalPosition = rectTransform.anchoredPosition;
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isFalling) return;

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isFalling) return;

        rectTransform.anchoredPosition +=
            eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isFalling) return;
        CheckWinConditions();
    }

    void CheckWinConditions()
    {
        bool isPlacedOnShelf = false;

        // 1. Check if the cheese was dropped near a shelf
        foreach (RectTransform shelf in shelves)
        {
            float verticalDistance = Mathf.Abs(rectTransform.position.y - shelf.position.y);

            if (verticalDistance <= shelfTolerance)
            {
                isPlacedOnShelf = true;

                // Snap the cheese to the shelf, adding the Y Offset to make it sit ON TOP
                rectTransform.position = new Vector3(rectTransform.position.x, shelf.position.y + yOffset, rectTransform.position.z);
                break;
            }
        }

        // If dropped in mid-air, it falls back to the start!
        if (!isPlacedOnShelf)
        {
            Debug.Log("Dropped in the void! The cheese falls down.");
            StartCoroutine(FallAndReset());
            return;
        }

        // IF WE REACH THIS POINT: The cheese is safely ON THE SHELF.
        // We re-enable clicks so the player can slide it around again to find the perfect spot!
        canvasGroup.blocksRaycasts = true;

        // 2. Check if the environment distances are correct
        float distToFan = Vector2.Distance(rectTransform.position, fanObject.position);
        float distToHeater = Vector2.Distance(rectTransform.position, heaterObject.position);

        Debug.Log("Distance to Fan: " + distToFan + " | Heater: " + distToHeater);

        bool fanIsCorrect = (distToFan >= fanMinDistance && distToFan <= fanMaxDistance);
        bool heaterIsCorrect = (distToHeater >= heaterMinDistance && distToHeater <= heaterMaxDistance);

        if (fanIsCorrect && heaterIsCorrect)
        {
            Debug.Log("PERFECT PLACEMENT! The cheese is on the right shelf and aging beautifully.");

            // TODO: Call your GameManager to complete the level here!
        }
        else
        {
            // NO FALLING ANIMATION HERE! 
            // It just stays on the shelf, waiting for the player to drag it again.
            Debug.Log("On the shelf, but wrong environment! Move it somewhere else.");
        }
    }

    // --- COROUTINE: FALLING ANIMATION ---
    IEnumerator FallAndReset()
    {
        isFalling = true;
        canvasGroup.blocksRaycasts = false;

        float fallSpeed = 0f;
        float gravity = 1500f;
        float timer = 0f;

        while (timer < 1.2f)
        {
            fallSpeed += gravity * Time.deltaTime;
            rectTransform.anchoredPosition -= new Vector2(0, fallSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        canvasGroup.blocksRaycasts = true;
        isFalling = false;
    }
    void OnDisable()
    {
        StopAllCoroutines();
        isFalling = false;
        if (GetComponent<CanvasGroup>() != null) GetComponent<CanvasGroup>().blocksRaycasts = true;
    }
}