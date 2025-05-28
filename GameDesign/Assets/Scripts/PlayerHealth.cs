using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    public Slider healthBar; // UI barra della vita
    public int baseDefense = 0;
    private int bonusDefense = 0; // da power-up

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(int incomingDamage)
    {
        int effectiveDefense = baseDefense + bonusDefense;
        int finalDamage = Mathf.Max(incomingDamage - effectiveDefense, 0);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.value = currentHealth;

        Debug.Log($"{gameObject.name} ha subito {finalDamage} danni (difesa: {effectiveDefense})");

        // Consuma il bonus se è stato utile
        if (bonusDefense > 0 && finalDamage > 0)
        {
            bonusDefense = 0;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetBonusDefense(int value)
    {
        bonusDefense = Mathf.Clamp(value, 1, 7);
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

}

