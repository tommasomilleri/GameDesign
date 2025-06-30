using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Will update the UI-visuals of each card, depending on it's data
/// </summary>
public class CardUI : MonoBehaviour
{
    #region Fields and Properties

    private Card _card;

    [Header("Prefab Elements")] //references to objects in the card prefab
    [SerializeField] private Image _cardImage;
    [SerializeField] private Image _elementBackground;
    [SerializeField] private Image _rarityBackground;

    [SerializeField] private TextMeshProUGUI _playCost;
    [SerializeField] private TextMeshProUGUI _cardName;
    [SerializeField] private TextMeshProUGUI _cardType;
    [SerializeField] private TextMeshProUGUI _cardDescription;

    [Header("Sprite Assets")] //references to the art folder in assets
    [SerializeField] private Sprite _fireElementBackground;
    [SerializeField] private Sprite _iceElementBackground;
    [SerializeField] private Sprite _lightningElementBackground;

    [SerializeField] private Sprite _commonRarityBackground;
    [SerializeField] private Sprite _rareRarityBackground;
    [SerializeField] private Sprite _epicRarityBackground;
    [SerializeField] private Sprite _legendaryRarityBackground;

    private readonly string EFFECTTYPE_TRAP = "Trap";
    private readonly string EFFECTTYPE_SPELL = "Spell";
    private readonly string EFFECTTYPE_MONSTER = "Monster";

    #endregion

    #region Methods

    private void Awake()
    {
        _card = GetComponent<Card>();
        SetCardUI();
    }

    //calls Awake every time the inspector/editor gets refreshed
    //- lets you see changes also in editor no need to start game
    private void OnValidate()
    {
        Awake();
    }

    public void SetCardUI()
    {
        if (_card != null && _card.CardData != null)
        {
            SetCardTexts();
            SetRarityBackground();
            SetElementFrame();
            SetCardImage();
        }
    }

    private void SetCardTexts()
    {
        SetCardEffectTypeText();

        _cardName.text = _card.CardData.CardName;
        _cardDescription.text = _card.CardData.CardDescription;
        _playCost.text = _card.CardData.PlayCost.ToString();
    }

    private void SetCardEffectTypeText()
    {
        switch (_card.CardData.EffectType)
        {
            case CardEffectType.Trap:
                _cardType.text = EFFECTTYPE_TRAP;
                break;
            case CardEffectType.Spell:
                _cardType.text = EFFECTTYPE_SPELL;
                break;
            case CardEffectType.Monster:
                _cardType.text = EFFECTTYPE_MONSTER;
                break;
        }
    }

    private void SetRarityBackground()
    {
        switch (_card.CardData.Rarity)
        {
            case CardRarity.Basic:
                _rareRarityBackground.GetComponent<Image>().enabled = false;
                break;
            case CardRarity.Common:
                _rarityBackground.sprite = _commonRarityBackground;
                break;
            case CardRarity.Rare:
                _rarityBackground.sprite = _rareRarityBackground;
                break;
            case CardRarity.Epic:
                _rarityBackground.sprite = _epicRarityBackground;
                break;
            case CardRarity.Legendary:
                _rarityBackground.sprite = _legendaryRarityBackground;
                break;
        }
    }

    private void SetElementFrame()
    {
        switch (_card.CardData.Element)
        {
            case CardElement.Basic:
                //do nothing - basic background
                break;
            case CardElement.Ice:
                _elementBackground.sprite = _iceElementBackground;
                break;
            case CardElement.Fire:
                _elementBackground.sprite = _fireElementBackground;
                break;
            case CardElement.Lightning:
                _elementBackground.sprite = _lightningElementBackground;
                break;
        }
    }

    private void SetCardImage()
    {
        _cardImage.sprite = _card.CardData.Image;
    }





    #endregion
}
