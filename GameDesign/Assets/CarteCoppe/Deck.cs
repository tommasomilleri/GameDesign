using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
   
    // restituisce quante carte rimangono ancora nel mazzo prima di pescare
    public int LivesRemaining => _deckPile.Count;
    /// <summary>
    /// Rimuove le prime `n` carte da _deckPile (le sposta in _discardPile e le disattiva).
    /// Se al termine non resta niente né in deck né in discard, invoca OnDeckEmpty.
    /// </summary>
    public void DiscardLives(int n)
    {
        for (int i = 0; i < n && _deckPile.Count > 0; i++)
        {
            var c = _deckPile[0];
            _deckPile.RemoveAt(0);
            _discardPile.Add(c);
            c.gameObject.SetActive(false);
        }

        // Notifico la UI (se la stai usando)
        OnCardDiscarded?.Invoke(null); // o un evento ad hoc che passi LivesRemaining

        // Se davvero non resta più nulla
        if (_deckPile.Count == 0 && _discardPile.Count == 0)
            OnDeckEmpty?.Invoke();
    }

    public static Deck Instance { get; private set; }

    [SerializeField] private CardCollection _playerDeck;
    [SerializeField] private Card _cardPrefab;
    [SerializeField] private Canvas _cardCanvas;

    private readonly List<Card> _deckPile = new();
    private readonly List<Card> _discardPile = new();
    public List<Card> HandCards { get; private set; } = new();

    // 1) Eventi esistenti per draw & discard
    public event Action<Card> OnCardDrawn;
    public event Action<Card> OnCardDiscarded;

    // 2) Nuovo evento per deck empty
    public event Action OnDeckEmpty;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InstantiateDeck();
    }

    private void InstantiateDeck()
    {
        foreach (var data in _playerDeck.CardsInCollection)
        {
            var card = Instantiate(_cardPrefab, _cardCanvas.transform);
            card.SetUp(data);
            card.gameObject.SetActive(false);
            _deckPile.Add(card);
        }
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = _deckPile.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_deckPile[i], _deckPile[j]) = (_deckPile[j], _deckPile[i]);
        }
    }

    /// <summary>
    /// Pesca e restituisce 1 carta; se non ci sono più carte né in deck né in discard,
    /// emette l’evento OnDeckEmpty e restituisce null.
    /// </summary>
    public Card DrawCard()
    {
        // Se finito entrambe le pile => deck empty!
        if (_deckPile.Count == 0 && _discardPile.Count == 0)
        {
            OnDeckEmpty?.Invoke();
            return null;
        }

        // Riciclo automatico delle scartate, se necessario
        if (_deckPile.Count == 0)
        {
            _deckPile.AddRange(_discardPile);
            _discardPile.Clear();
            ShuffleDeck();
        }

        // Pesca la carta
        var card = _deckPile[0];
        _deckPile.RemoveAt(0);
        HandCards.Add(card);
        card.gameObject.SetActive(true);

        OnCardDrawn?.Invoke(card);
        return card;
    }

    /// <summary>
    /// Scarta una carta da mano (o da schermo) e emette OnCardDiscarded.
    /// </summary>
    public void DiscardCard(Card card)
    {
        if (!HandCards.Remove(card))
            return;

        _discardPile.Add(card);
        card.gameObject.SetActive(false);
        OnCardDiscarded?.Invoke(card);
    }

    /// <summary>
    /// Pesca N carte in mano (usa DrawCard internamente).
    /// </summary>
    public void DrawHand(int amount = 5)
    {
        for (int i = 0; i < amount; i++)
            DrawCard();
    }
}
