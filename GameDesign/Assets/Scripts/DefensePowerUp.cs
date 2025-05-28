using UnityEngine;

public class DefensePowerUp : MonoBehaviour
{
    public int defenseValue = 4; // da 1 a 7

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Blocca la raccolta se già possiede un bonus
            if (!health.HasActiveBonusDefense())
            {
                health.SetBonusDefense(defenseValue);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"{other.name} ha già un power-up di difesa attivo.");
            }
        }
    }
}

