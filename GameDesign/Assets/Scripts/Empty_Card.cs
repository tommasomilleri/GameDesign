using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Empty_Card : MonoBehaviour
{
    public int my_value;
    public string my_suit;
    public static int active_cards = 0;

    void OnTriggerEnter(Collider other)
    {
        switch (my_suit)
        {
            case "spade":
                PlayerAttack base_atk = other.GetComponent<PlayerAttack>();
                if (base_atk != null)
                {
                    if (!base_atk.HasActivePowerUp())
                    {
                        base_atk.SetPowerUpValue(my_value);
                        Destroy(gameObject);
                    }
                }
                break;
            case "denari":
                PlayerHealth base_money = other.GetComponent<PlayerHealth>();
                if (base_money != null)
                {
                    base_money.baseMoney += my_value;
                    base_money.myMoney.text = $"Mon: {base_money.baseMoney}";
                    Destroy(gameObject);
                }
                break;
            case "coppe":
                PlayerHealth base_life = other.GetComponent<PlayerHealth>();
                if (base_life != null)
                {
                    if (base_life.currentHealth < base_life.maxHealth)
                    {
                        if ((base_life.currentHealth + my_value) <= base_life.maxHealth)
                        { base_life.currentHealth += my_value; }
                        else { base_life.currentHealth = base_life.maxHealth; }
                        if (base_life.healthBar != null)
                        {
                            base_life.healthBar.value = base_life.currentHealth;
                            base_life.myLife.text = $"Life: {base_life.currentHealth.ToString()}";
                        }
                        Destroy(gameObject);
                    }
                }
                break;
            case "bastoni":
                PlayerHealth base_def = other.GetComponent<PlayerHealth>();
                if (base_def != null)
                {
                    if (!base_def.HasActiveBonusDefense())
                    {
                        base_def.SetBonusDefense(my_value);
                        Destroy(gameObject);
                    }
                }
                break;
            default:
                break;
        }
    }

    void OnEnable()
    {
        active_cards++;
    }

    void OnDisable()
    {
        active_cards--;
    }
}