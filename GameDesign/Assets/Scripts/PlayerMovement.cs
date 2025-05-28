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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        startPosition = transform.position; // Memorizza la posizione iniziale
    }

    void Update()
    {
        // Controllo se il player è a terra
        isGrounded = Physics.CheckSphere(transform.position - Vector3.up * 0.1f, groundCheckDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Movimento
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

        // Gravità
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Controllo se è caduto nel vuoto
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        controller.enabled = false;
        transform.position = startPosition;
        velocity = Vector3.zero;
        controller.enabled = true;
    }
}

