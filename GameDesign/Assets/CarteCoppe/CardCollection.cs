using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A generic collection of CardData objects. Can be used as a deck or a booster, for example.
/// </summary>
[CreateAssetMenu(menuName = "Card Collection")]
public class CardCollection : ScriptableObject
{
    [field: SerializeField] public List<ScriptableCard> CardsInCollection { get; private set; }

    //optional: if you think you will need to edit the collection at runtime:

    public void RemoveCardFromCollection(ScriptableCard card)
    {
        if (CardsInCollection.Contains(card))
        {
            CardsInCollection.Remove(card);
        }
        else
        {
            Debug.LogWarning("CardData is not present in collection - cannot remove");
        }
    }

    //we can have multiples of the same card in a collection, if you don't want this add an if statement similar to above
    public void AddCardToCollection(ScriptableCard card)
    {
        CardsInCollection.Add(card);
    }
}
