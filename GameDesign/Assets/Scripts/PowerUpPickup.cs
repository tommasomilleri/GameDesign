using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public int powerUpValue = 3; // da 1 a 7

    void OnTriggerEnter(Collider other)
    {
        PlayerAttack attack = other.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            // Blocca la raccolta se già possiede un power-up
            if (!attack.HasActivePowerUp())
            {
                attack.SetPowerUpValue(powerUpValue);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"{other.name} ha già un power-up d’attacco attivo.");
            }
        }
    }
}

