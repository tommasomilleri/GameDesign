using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all data for each individual card
/// </summary>

[CreateAssetMenu(menuName = "CardData")] //lets you create a new CardData Object with the right-click menu in the editor
public class ScriptableCard : ScriptableObject
{
    //field: SerializeField lets you reveal properties in the inspector like they were public fields
    [field: SerializeField] public string CardName { get; private set; } 
    [field: SerializeField, TextArea] public string CardDescription { get; private set; } //TextArea makes an input field in the inspector to write longer text in
    [field: SerializeField] public int PlayCost { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeField] public CardElement Element { get; private set; }
    [field: SerializeField] public CardEffectType EffectType { get; private set; }
    [field: SerializeField] public CardRarity Rarity { get; private set; }

                                                                           
}

public enum CardElement
{
    Basic,
    Ice,
    Fire,
    Lightning
}

public enum CardEffectType
{
    Trap,
    Spell,
    Monster
}

public enum CardRarity
{
    Basic,
    Common,
    Rare,
    Epic,
    Legendary
}
