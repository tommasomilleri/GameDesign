using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // NECESSARIO per IEnumerator

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public TextMeshProUGUI myLife;
    public Slider healthBar; // UI barra della vita
    public int baseDefense = 0;
    public TextMeshProUGUI myDef;
    private int bonusDefense = 0; // da power-up
    public int baseMoney = 0;
    public TextMeshProUGUI myMoney;

    // OPTIONAL: per messaggi tipo "non hai abbastanza oro"
    public TextMeshProUGUI messageText;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            myLife.text = $"Life: {currentHealth.ToString()}";
        }
        myMoney.text = "Mon: 0";
        myDef.text = $"Def: {baseDefense.ToString()}";

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
            myLife.text = $"Life: {currentHealth.ToString()}";
        }
        Debug.Log($"{gameObject.name} ha subito {finalDamage} danni (difesa: {effectiveDefense})");

        // Consuma il bonus se è stato utile
        if (bonusDefense > 0 && finalDamage > 0)
        {
            bonusDefense = 0;
            myDef.text = $"Def: {baseDefense.ToString()}";
        }

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    public void SetBonusDefense(int value)
    {
        bonusDefense = Mathf.Clamp(value, 1, 7);
        myDef.text = $"Def: {(baseDefense + bonusDefense).ToString()}";
        Debug.Log($"{gameObject.name} ha ricevuto una difesa bonus di {bonusDefense}");
    }

    void GameOver()
    {
        gameObject.SetActive(false);
        Debug.Log($"{gameObject.name} è morto!");
    }

    public bool HasActiveBonusDefense()
    {
        return bonusDefense > 0;
    }

    // ✅ AGGIUNTA: Meccanica della porta — usa TrySpendGold da altri script
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

    // 🟠 NOTA: La classe PlayerGold interna non è più necessaria se usi TrySpendGold
}

