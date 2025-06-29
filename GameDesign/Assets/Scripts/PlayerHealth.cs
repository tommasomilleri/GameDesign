using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    }

    public void TakeDamage(int incomingDamage)
    {
        int effectiveDefense = baseDefense + bonusDefense;
        int finalDamage = Mathf.Max(incomingDamage - effectiveDefense, 0);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

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

        if (currentHealth <= 0)
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
}



