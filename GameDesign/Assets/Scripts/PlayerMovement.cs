using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public KeyCode moveUp = KeyCode.W;
    public KeyCode moveDown = KeyCode.S;
    public KeyCode moveLeft = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;

    public float speed = 6f;
    public float rotationSpeed = 720f;

    public float gravity = -9.81f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask;

    public float fallThreshold = -10f; // Altezza sotto cui il player viene respawnato

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private Vector3 startPosition;

    private Animator animator;

    private PlayerHealth playerHealth;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        startPosition = transform.position;

        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath += HandleDeath;
        }

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
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            // Morte: blocca movimento
            return;
        }

        isGrounded = Physics.CheckSphere(transform.position - Vector3.up * 0.1f, groundCheckDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = 0f;
        float z = 0f;

        if (Input.GetKey(moveLeft)) x = -1f;
        if (Input.GetKey(moveRight)) x = 1f;
        if (Input.GetKey(moveUp)) z = 1f;
        if (Input.GetKey(moveDown)) z = -1f;

        Vector3 move = new Vector3(x, 0f, z).normalized;

        if (move.magnitude >= 0.1f)
        {
            controller.Move(move * speed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Aggiorna animazione
        if (animator != null)
        {
            animator.SetFloat("Speed", move.magnitude);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void HandleDeath()
    {
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetFloat("Speed", 0);
        }
    }

    void Respawn()
    {
        controller.enabled = false;
        transform.position = startPosition;
        velocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }

        if (playerHealth != null)
        {
            playerHealth.currentHealth = playerHealth.maxHealth;
            if (playerHealth.healthBar != null)
            {
                playerHealth.healthBar.value = playerHealth.currentHealth;
                playerHealth.myLife.text = $"Life: {playerHealth.currentHealth}";
            }
        }

        controller.enabled = true;
    }

    // Chiamare se cambi modello attivo in runtime
    public void RefreshAnimatorReference()
    {
        UpdateAnimatorReference();
    }
}
