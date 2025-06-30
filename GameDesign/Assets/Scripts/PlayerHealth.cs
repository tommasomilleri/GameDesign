using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using System;

public class PlayerHealth : MonoBehaviour
{

    public int maxHealth = 100;
    public int currentHealth;

    public TextMeshProUGUI myLife;
    public Slider healthBar;
    public int baseDefense = 0;
    public TextMeshProUGUI myDef;
    private int bonusDefense = 0;
    public int baseMoney = 0;
    public TextMeshProUGUI myMoney;

    public TextMeshProUGUI messageText;

    public delegate void PlayerDeathDelegate();
    public event PlayerDeathDelegate OnPlayerDeath;
    public MonoBehaviour[] inputScriptsToDisable; // Inserisci PlayerMovement, PlayerAttack ecc.
    public GameObject gameOverScreen;
    public float delayBeforeGameOverScreen = 0.5f; // durata animazione morte

    // --- AGGIUNTA PER FLASH ROSSO ---
    public Color flashColorPrefab = Color.red;
    public float prefabFlashDuration = 0.2f;
    [Header("Mazzo-Vita")]
    public Deck deck;               // trascina in Inspector il tuo GameObject con lo script Deck.cs
    //public LifeDeckUI lifeDeckUI;   // l’UI che mostra le carte-vite

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            myLife.text = $"Life: {currentHealth}";
        }
        myMoney.text = "Mon: 0";
        myDef.text = $"Def: {baseDefense}";

        if (messageText != null)
            messageText.gameObject.SetActive(false);
        //lifeDeck = new LifeDeck(lifeFigure, maxLives);
        //lifeDeck.OnLivesChanged += remaining => lifeDeckUI.SetLives(remaining);
        //lifeDeck.FillDeck();
        //lifeDeckUI.Initialize(maxLives);
        // 1) prendo l’istanza single-ton di Deck
        if (Deck.Instance == null)
            Debug.LogError("PlayerHealth: non ho trovato nessun Deck in scena!");
        deck = Deck.Instance;
        lifeDeckUI.Initialize(deck.LivesRemaining);
        deck.OnDeckEmpty += GameOver;
        deck.OnCardDiscarded += _ => lifeDeckUI.SetLives(deck.LivesRemaining);
    }

    public void TakeDamage(int incomingDamage)
    {
        int effectiveDefense = baseDefense + bonusDefense;
        int finalDamage = Mathf.Max(incomingDamage - effectiveDefense, 0);

        currentHealth -= finalDamage;
        // currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (finalDamage > 0)
            deck.DiscardLives(finalDamage);
        //lifeDeck.DiscardLives(finalDamage);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            myLife.text = $"Life: {currentHealth}";
        }
        Debug.Log($"{gameObject.name} ha subito {finalDamage} danni (difesa: {effectiveDefense})");

        if (bonusDefense > 0 && finalDamage > 0)
        {
            bonusDefense = 0;
            myDef.text = $"Def: {baseDefense}";
        }

        if (finalDamage > 0)
        {
            StartCoroutine(FlashPrefabRenderers());
        }
        if (deck.LivesRemaining <= 0)
        //if (lifeDeck.LivesRemaining <= 0 /*currentHealth <= 0 */)
        {
            GameOver();
        }
    }

    public void SetBonusDefense(int value)
    {
        bonusDefense = Mathf.Clamp(value, 1, 7);
        myDef.text = $"Def: {(baseDefense + bonusDefense)}";
        Debug.Log($"{gameObject.name} ha ricevuto una difesa bonus di {bonusDefense}");
    }

    void GameOver()
    {
        Debug.Log($"{gameObject.name} è morto!");
        OnPlayerDeath?.Invoke();

        StartCoroutine(HandleGameOver());
    }

    public void ForceGameOver()
    {
        foreach (MonoBehaviour script in inputScriptsToDisable)
        {
            script.enabled = false;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetBool("IsDead", true);
    }

    IEnumerator HandleGameOver()
    {
        // Aspetta la durata dell'animazione morte
        yield return new WaitForSeconds(delayBeforeGameOverScreen);

        // Mostra pannello Game Over
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        // Disabilita input
        foreach (MonoBehaviour script in inputScriptsToDisable)
        {
            script.enabled = false;
        }
    }

    public bool HasActiveBonusDefense()
    {
        return bonusDefense > 0;
    }

    public bool TrySpendGold(int amount)
    {
        if (baseMoney >= amount)
        {
            baseMoney -= amount;
            UpdateGoldUI();
            return true;
        }
        else
        {
            if (messageText != null)
                StartCoroutine(ShowMessage("Non hai abbastanza oro!", 2f));
            return false;
        }
    }
    private void OnDisable()
    {
        if (deck != null)
        {
            deck.OnDeckEmpty -= GameOver;
            deck.OnCardDiscarded -= _ => lifeDeckUI.SetLives(deck.LivesRemaining);
        }
    }


    void UpdateGoldUI()
    {
        if (myMoney != null)
            myMoney.text = "Mon: " + baseMoney;
    }

    IEnumerator ShowMessage(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        messageText.gameObject.SetActive(false);
    }

    // --- METODO AGGIUNTO PER FLASH ROSSO ---
    IEnumerator FlashPrefabRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>()
            .Where(r => r.gameObject.activeInHierarchy)
            .ToArray();

        Material[][] originalMats = renderers
            .Select(r => r.materials.ToArray())
            .ToArray();

        // Applica materiale rosso temporaneo
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] flashingMats = originalMats[i]
                .Select(mat =>
                {
                    Material newMat = new Material(mat);
                    if (newMat.HasProperty("_Color"))
                        newMat.color = flashColorPrefab;
                    return newMat;
                }).ToArray();

            renderers[i].materials = flashingMats;
        }

        yield return new WaitForSeconds(prefabFlashDuration);

        // Ripristina materiali originali
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMats[i];
        }
    }
}
