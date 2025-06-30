using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Defines what a card is and can be, will connect all data and behaviours
/// </summary>
[RequireComponent(typeof(CardUI))] //will automatically attack the CardUI Script to every object that is a card
[RequireComponent(typeof(CardMovement))] //will handle everything to do with perceived card movement
public class Card : MonoBehaviour
{
    #region Fields and Properties

    [field: SerializeField] public ScriptableCard CardData { get; private set; }

    #endregion

    #region Methods

    //Set the relevant card data at runtime and update the card's ui
    public void SetUp(ScriptableCard data)
    {
        CardData = data;
        GetComponent<CardUI>().SetCardUI();
    }

    #endregion


    //next time: How to build a deck system with a discard pile and a deck to draw from... Seeya!

}
