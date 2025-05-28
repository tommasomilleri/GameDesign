using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public string opponentTag = "Player2";
    public KeyCode attackKey = KeyCode.F;
    public float attackRange = 2f;
    public int baseDamage = 5;

    private int powerUpValue = 0; // Valore del power-up raccolto, da 1 a 7

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            AttemptAttack();
        }
    }

    void AttemptAttack()
    {
        bool hitSomething = false;

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag(opponentTag))
            {
                PlayerHealth enemyHealth = hit.GetComponent<PlayerHealth>();
                if (enemyHealth != null)
                {
                    int totalDamage = baseDamage + powerUpValue;
                    enemyHealth.TakeDamage(totalDamage);
                    Debug.Log($"{gameObject.name} ha colpito {hit.name} infliggendo {totalDamage} danni!");

                    powerUpValue = 0; // Consuma il power-up solo se colpisce
                    hitSomething = true;
                }
            }
        }

        if (!hitSomething)
        {
            Debug.Log($"{gameObject.name} ha attaccato ma ha mancato.");
        }
    }

    // Metodo pubblico da chiamare quando si raccoglie un power-up
    public void SetPowerUpValue(int value)
    {
        powerUpValue = Mathf.Clamp(value, 1, 7); // Limita il valore da 1 a 7
        Debug.Log($"{gameObject.name} ha raccolto un power-up di valore {powerUpValue}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
    }
    public bool HasActivePowerUp()
    {
        return powerUpValue > 0;
    }

}
