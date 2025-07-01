using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Empty_Card : MonoBehaviour
{
    public int my_value;
    public string my_suit;
    public static int active_cards = 0;
    public AudioSource sfx_player;
    public AudioClip defense_pickup_sound;
    public AudioClip money_pickup_sound;
    public AudioClip life_pickup_sound;
    public AudioClip attack_pickup_sound;

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
                        sfx_player.PlayOneShot(attack_pickup_sound);
                        base_atk.SetPowerUpValue(my_value);
                        Destroy(gameObject);
                    }
                }
                break;
            case "denari":
                PlayerHealth base_money = other.GetComponent<PlayerHealth>();
                if (base_money != null)
                {
                    sfx_player.PlayOneShot(money_pickup_sound);
                    base_money.baseMoney += my_value;
                    base_money.myMoney.text = $"Oro: {base_money.baseMoney}";
                    Destroy(gameObject);
                }
                break;
            case "coppe":
                PlayerHealth base_life = other.GetComponent<PlayerHealth>();
                if (base_life != null)
                {
                    if (base_life.currentHealth < base_life.maxHealth)
                    {
                        sfx_player.PlayOneShot(life_pickup_sound);
                        if ((base_life.currentHealth + my_value) <= base_life.maxHealth)
                        { base_life.currentHealth += my_value; }
                        else { base_life.currentHealth = base_life.maxHealth; }
                        if (base_life.healthBar != null)
                        {
                            base_life.healthBar.value = base_life.currentHealth;
                            base_life.myLife.text = $"Vita: {base_life.currentHealth.ToString()}";
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
                        sfx_player.PlayOneShot(defense_pickup_sound);
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