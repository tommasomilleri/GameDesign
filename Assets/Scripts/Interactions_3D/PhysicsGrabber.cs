using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("Impostazioni Presa")]
    public float grabSpeed = 15f;
    public float maxThrowSpeed = 5f;
    public bool IsHolding { get; private set; }
    Camera cam;
    Rigidbody held;
    Plane dragPlane;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // 1. QUANDO CLICCHI (Afferra)
        if (Input.GetMouseButtonDown(0) && held == null)
        {
            Ray r = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
            {
                // Controlla che sia un oggetto fisico con il tag Grabbable[cite: 3]
                if (hit.rigidbody != null && hit.rigidbody.CompareTag("Grabbable"))
                {
                    held = hit.rigidbody;

                    // TOGLIAMO la gravità per non farlo cadere, ma MANTENIAMO la fisica attiva!
                    held.useGravity = false;

                    // Aumentiamo la frizione dell'aria per non farlo impazzire mentre lo teniamo
                    held.linearDamping = 10f;

                    dragPlane = new Plane(-cam.transform.forward, held.position);
                    IsHolding = true;
                }
            }
        }

        // 2. QUANDO RILASCI (Lancia / Fai cadere)
        if (Input.GetMouseButtonUp(0) && held != null)
        {
            // Rimettiamo tutto alla normalità
            held.useGravity = true;
            held.linearDamping = 0f;

            // Limitiamo la velocità di lancio
            held.linearVelocity = Vector3.ClampMagnitude(held.linearVelocity, maxThrowSpeed);
            held = null;
            IsHolding = false;
        }
    }

    // 3. LA VERA FISICA (Si usa FixedUpdate, non Update!)
    void FixedUpdate()
    {
        if (held != null)
        {
            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(r, out float d))
            {
                Vector3 targetPosition = r.GetPoint(d);

                // Invece di teletrasportarlo, calcoliamo la direzione verso il mouse...
                Vector3 direction = targetPosition - held.position;

                // ...e usiamo la velocità fisica per "tirarlo" verso il bersaglio!
                // (Se sbatte contro uno scaffale, si bloccherà in automatico)
                held.linearVelocity = direction * grabSpeed;
            }
        }
    }
}