using UnityEngine;
using TMPro;

public class PlayerAttack : MonoBehaviour
{
    public string opponentTag = "Player2";
    public KeyCode attackKey = KeyCode.F;
    public float attackRange = 2f;
    public int baseDamage = 5;
    public TextMeshProUGUI myAtk;
    private int powerUpValue = 0;

    private Animator animator;
    private bool isAttacking = false;

    public AudioSource sfx_player;
    public AudioClip attacked_sound;
    public AudioClip missed_sound;

    void Start()
    {
        myAtk.text = $"Atk: {baseDamage}";
        UpdateAnimatorReference();
    }

    void UpdateAnimatorReference()
    {
        animator = null;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                Animator a = child.GetComponent<Animator>();
                if (a != null)
                {
                    animator = a;
                    Debug.Log($"Animator trovato sul modello attivo: {child.name}");
                    break;
                }
            }
        }

        if (animator == null)
        {
            Debug.LogWarning("Nessun Animator trovato tra i figli attivi!");
        }
    }

    void Update()
    {
        if (isAttacking) return;

        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
    }

    void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        isAttacking = true;
    }

    // Metodo chiamato tramite Animation Event a metà animazione
    public void AttemptAttack()
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

                    powerUpValue = 0;
                    myAtk.text = $"Atk: {baseDamage}";
                    hitSomething = true;
                    sfx_player.PlayOneShot(attacked_sound);
                }
            }
        }

        if (!hitSomething)
        {
            sfx_player.PlayOneShot(missed_sound);
            Debug.Log($"{gameObject.name} ha attaccato ma ha mancato.");
        }
    }

    // Metodo chiamato tramite Animation Event a fine animazione
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    public void SetPowerUpValue(int value)
    {
        powerUpValue = Mathf.Clamp(value, 1, 7);
        myAtk.text = $"Atk: {(baseDamage + powerUpValue)}";
        Debug.Log($"{gameObject.name} ha raccolto un power-up di valore {powerUpValue}");
    }

    // Chiamare se cambi modello attivo in runtime
    public void RefreshAnimatorReference()
    {
        UpdateAnimatorReference();
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

