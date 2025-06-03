using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public TextMeshProUGUI myLife;
    public Slider healthBar; // UI barra della vita
    public int baseDefense = 0;
    public TextMeshProUGUI myDef;
    private int bonusDefense = 0; // da power-up
    public int baseMoney = 0;
    public TextMeshProUGUI myMoney;
    //private int bonusMoney = 0;

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
    }

    public void TakeDamage(int incomingDamage)
    {
        int effectiveDefense = baseDefense + bonusDefense;
        int finalDamage = Mathf.Max(incomingDamage - effectiveDefense, 0);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.value = currentHealth;
            myLife.text = $"Life: {currentHealth.ToString()}";
        Debug.Log($"{gameObject.name} ha subito {finalDamage} danni (difesa: {effectiveDefense})");

        // Consuma il bonus se è stato utile
        if (bonusDefense > 0 && finalDamage > 0)
        {
            bonusDefense = 0;
            myDef.text = $"Def: {baseDefense.ToString()}";
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetBonusDefense(int value)
    {
        bonusDefense = Mathf.Clamp(value, 1, 7);
        myDef.text = $"Def: {(baseDefense + bonusDefense).ToString()}";
        Debug.Log($"{gameObject.name} ha ricevuto una difesa bonus di {bonusDefense}");
    }

    void Die()
    {
        gameObject.SetActive(false);
        Debug.Log($"{gameObject.name} è morto!");
    }
    public bool HasActiveBonusDefense()
    {
        return bonusDefense > 0;
    }

    /*public int SetMoney(int value)
    {

    }*/

    /*funzione per ripristinare vita quando si raccoglie una coppa*/

}

