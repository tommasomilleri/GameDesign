using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles drag and drop of cards, as well as their movement when zoomed out, etc.
/// </summary>
public class CardMovement : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    #region Fields and Properties

    private bool _isBeingDragged; //we will need this later
    private Canvas _cardCanvas; //we need to get this at runtime, assigning in the inspector wont work
    private RectTransform _rectTransform;
    private Card _card;

    private readonly string CANVAS_TAG = "CardCanvas";

    #endregion

    #region Methods

    private void Start()
    {
        _cardCanvas = GameObject.FindGameObjectWithTag(CANVAS_TAG).GetComponent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();
        _card = GetComponent<Card>();
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        _isBeingDragged = true;
    }

    #endregion
    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += (eventData.delta / _cardCanvas.scaleFactor);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isBeingDragged = false;
        Deck.Instance.DiscardCard(_card);
    }
}
