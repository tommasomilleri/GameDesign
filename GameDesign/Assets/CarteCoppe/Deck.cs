using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a card deck and also governs the discard pile and works in concordance with the Hand script
/// Singleton
/// </summary>
public class Deck : MonoBehaviour
{
    #region Fields and Properties

    public static Deck Instance { get; private set; } //Singleton

    //now we need a reference to what a deck is, a.k.a. what cards it contains -> CardCollection
    //we will work with one deck for now, but you can easily add several choices for the player to pick from
    [SerializeField] private CardCollection _playerDeck;
    [SerializeField] private Card _cardPrefab; //our cardPrefab, of which we will make copies with the different CardData

    [SerializeField] private Canvas _cardCanvas;

    //now to represent the instantiated Cards
    private List<Card> _deckPile = new();
    private List<Card> _discardPile = new();

    public List<Card> HandCards { get; private set; } = new();

    //Alright, see you in the next tutorial! Probably about making the hand look nicer, maybe something else we'll see! Have a great time!

    #endregion

    #region Methods

    private void Awake()
    {
        //typical singleton declaration
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //we will instantiate the deck once, at the start of the game/level
        InstantiateDeck();
    }

    private void InstantiateDeck()
    {
        for (int i = 0; i < _playerDeck.CardsInCollection.Count; i++)
        {
            Card card = Instantiate(_cardPrefab, _cardCanvas.transform); //instantiates the Card Prefab as child of the Card Canvas == as UI
            card.SetUp(_playerDeck.CardsInCollection[i]);
            _deckPile.Add(card); //at the start, all cards are in the deck, none in hand, none in discard
            card.gameObject.SetActive(false); //we will later activate the cards when we draw them, for now we just want to build the pool
        }

        ShuffleDeck();
    }

    //call once at start and whenever deck count hits zero
    //uses the Fisher-Yates shuffle algorithm
    private void ShuffleDeck()
    {
        for (int i = _deckPile.Count - 1; i > 0; i--) 
        {
            int j = Random.Range(0, i + 1);
            var temp = _deckPile[i];
            _deckPile[i] = _deckPile[j];
            _deckPile[j] = temp;
        }
    }

    //puts amount cards in hand
    public void DrawHand(int amount = 5)
    {
        for (int i = 0; i < amount; i++)
        {
            if (_deckPile.Count <= 0)
            {
                _discardPile = _deckPile;
                _discardPile.Clear();
                ShuffleDeck();
            }

            //will rarely happen in a real game, but if all cards are in hand we get that error
            if (_deckPile.Count > 0)
            {
                HandCards.Add(_deckPile[0]);
                _deckPile[0].gameObject.SetActive(true);
                _deckPile.RemoveAt(0);
            }
        }
    }

    //we will assume no cards can be discarded directly from the deck to the discard pile
    //otherwise make two methods, one to discard from hand, one from deck
    public void DiscardCard(Card card)
    {
        if (HandCards.Contains(card))
        {
            HandCards.Remove(card);
            _discardPile.Add(card);
            card.gameObject.SetActive(false);
        }
    }

    #endregion
}
