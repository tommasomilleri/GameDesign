
using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("Impostazioni Presa")]
    public float grabSpeed = 15f;
    public float maxThrowSpeed = 3f;

    [Header("Profondita' (rotella mouse)")]
    [Tooltip("Velocita' della rotella. SCALA della scena: se gli oggetti sono a 150m, mettila a ~40")]
    public float scrollSpeed = 2f;
    [Tooltip("Distanza minima dalla camera. Tarare sulla scala della scena!")]
    public float minDepth = 0.8f;
    [Tooltip("Distanza massima. Regola: distanza camera-pentolone + margine")]
    public float maxDepth = 6f;

    [Header("Stabilita'")]
    [Tooltip("Sotto questa distanza dal target si ferma. Su scene grandi: ~1.0")]
    public float deadZone = 0.05f;

    [Header("Debug")]
    public bool debugLogs = true;

    public bool IsHolding { get; private set; }
    public void ConfigureDepth(float newMinDepth, float newMaxDepth, float newScrollSpeed)
    {
        minDepth = newMinDepth;
        maxDepth = newMaxDepth;
        scrollSpeed = newScrollSpeed;
        // Se stiamo gia' tenendo qualcosa, riclampa subito
        currentDepth = Mathf.Clamp(currentDepth, minDepth, maxDepth);
    }

    Camera cam;
    Rigidbody held;
    float currentDepth;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            Debug.LogError("[Grabber] Nessuna Camera su '" + name +
                           "'. Mettilo sulla Main Camera!", this);
    }

    void Update()
    {
        if (cam == null) return;

        bool paused = PauseMenuManager.Instance != null &&
                      PauseMenuManager.Instance.isPaused;
        bool noGameplay = GameManager.instance == null ||
                          !GameManager.instance.gameplayActive;
        bool changing = GameManager.instance != null &&
                        GameManager.instance.IsChangingLevel;
        bool blocked = paused || noGameplay || changing;

        if (blocked)
        {
            if (debugLogs && Input.GetMouseButtonDown(0))
                Debug.Log("[Grabber] BLOCCATO: paused=" + paused +
                          " gameplayActive=" + !noGameplay +
                          " isChangingLevel=" + changing);
            if (held != null) Release();
            return;
        }

        // ---- 1. AFFERRA ----
        if (Input.GetMouseButtonDown(0) && held == null)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            // RAGGIO INFINITO: su scene fuori scala 100m non bastavano
            if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.rigidbody != null && hit.rigidbody.CompareTag("Grabbable"))
                {
                    held = hit.rigidbody;
                    held.useGravity = false;
                    held.linearDamping = 10f;
                    currentDepth = Mathf.Clamp(
                        Vector3.Distance(cam.transform.position, held.position),
                        minDepth, maxDepth);
                    IsHolding = true;
                }
                else if (debugLogs)
                {
                    if (hit.rigidbody == null)
                        Debug.Log("[Grabber] colpito '" + hit.collider.name +
                                  "' a distanza " + hit.distance.ToString("F1") +
                                  " ma NON ha Rigidbody (etichetta/prop con " +
                                  "collider davanti alla fiala?)");
                    else
                        Debug.Log("[Grabber] colpito '" + hit.rigidbody.name +
                                  "' a distanza " + hit.distance.ToString("F1") +
                                  " ma il tag e' '" + hit.rigidbody.tag +
                                  "'. Il tag Grabbable va sul GameObject " +
                                  "che ha il RIGIDBODY!");
                }
            }
            else if (debugLogs)
            {
                Debug.Log("[Grabber] il raycast non ha colpito NULLA " +
                          "(nemmeno tavolo o muri: fiale/scena SENZA " +
                          "collider attivi)");
            }
        }

        // ---- 2. ROTELLA ----
        if (held != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                currentDepth = Mathf.Clamp(currentDepth + scroll * scrollSpeed,
                                           minDepth, maxDepth);
        }

        // ---- 3. RILASCIA ----
        if (Input.GetMouseButtonUp(0) && held != null) Release();
    }

    void Release()
    {
        if (held != null)
        {
            held.useGravity = true;
            held.linearDamping = 0f;
            held.linearVelocity = Vector3.ClampMagnitude(
                held.linearVelocity * 0.2f, maxThrowSpeed);
            held.angularVelocity = Vector3.zero;
        }
        held = null;
        IsHolding = false;
    }

    void FixedUpdate()
    {
        if (held == null) return;

        if (!held.gameObject.activeInHierarchy)
        {
            held = null;
            IsHolding = false;
            return;
        }

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 target = r.GetPoint(currentDepth);

        Vector3 dir = target - held.position;
        held.linearVelocity = dir.magnitude < deadZone
            ? Vector3.zero
            : dir * grabSpeed;
    }
}

