
using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("Impostazioni Presa")]
    public float grabSpeed = 15f;
    public float maxThrowSpeed = 3f;

    [Header("Profondita' (rotella mouse)")]
    public float scrollSpeed = 2f;
    public float minDepth = 0.8f;     // mai attaccato alla lente
    public float maxDepth = 6f;       // mai oltre il set (tarare dopo il punto 1!)

    [Header("Stabilita'")]
    [Tooltip("Sotto questa distanza dal target l'oggetto si ferma (no orbita)")]
    public float deadZone = 0.05f;

    public bool IsHolding { get; private set; }

    Camera cam;
    Rigidbody held;
    float currentDepth;               // distanza dalla camera, variabile con la rotella

    void Awake() { cam = GetComponent<Camera>(); }

    void Update()
    {
        // Pausa o menu: rilascio forzato e stop
        bool blocked =
            (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) ||
            (GameManager.instance != null && !GameManager.instance.gameplayActive);
        if (blocked) { if (held != null) Release(); return; }

        // 1. AFFERRA
        if (Input.GetMouseButtonDown(0) && held == null)
        {
            // Mai afferrare attraverso la UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(r, out RaycastHit hit, 100f) &&
                hit.rigidbody != null && hit.rigidbody.CompareTag("Grabbable"))
            {
                held = hit.rigidbody;
                held.useGravity = false;
                held.linearDamping = 10f;
                // La profondita' iniziale e' quella dell'oggetto al grab
                currentDepth = Vector3.Distance(cam.transform.position, held.position);
                IsHolding = true;
            }
        }

        // 2. ROTELLA: avvicina/allontana lungo lo sguardo
        if (held != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                currentDepth = Mathf.Clamp(currentDepth + scroll * scrollSpeed,
                                           minDepth, maxDepth);
        }

        // 3. RILASCIA
        if (Input.GetMouseButtonUp(0) && held != null) Release();
    }

    void Release()
    {
        held.useGravity = true;
        held.linearDamping = 0f;
        // FIX "scivolano via": velocita' di trascinamento ridotta all'80%
        // e rotazione azzerata. L'oggetto CADE dove lo molli, non vola.
        held.linearVelocity = Vector3.ClampMagnitude(held.linearVelocity * 0.2f,
                                                     maxThrowSpeed);
        held.angularVelocity = Vector3.zero;
        held = null;
        IsHolding = false;
    }

    void FixedUpdate()
    {
        if (held == null) return;

        // Target = punto sul raggio del mouse ALLA PROFONDITA' corrente
        // (niente piu' Plane fisso: la rotella muove currentDepth)
        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 target = r.GetPoint(currentDepth);

        Vector3 dir = target - held.position;
        // Dead zone: vicino al target si ferma invece di orbitare
        held.linearVelocity = dir.magnitude < deadZone
            ? Vector3.zero
            : dir * grabSpeed;
    }
}
