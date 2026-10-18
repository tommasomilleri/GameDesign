using UnityEngine;


public class DioramaNavigator : MonoBehaviour
{
    [Header("Area navigabile")]
    [Tooltip("BoxCollider (trigger) che delimita l'area. Obbligatorio.")]
    public BoxCollider navBounds;

    [Header("Movimento")]
    public float moveSpeed = 2.5f;
    [Tooltip("Secondi per raggiungere la velocita' piena")]
    public float accelTime = 0.18f;
    [Tooltip("Il movimento e' relativo alla camera (W = avanti visivo)")]
    public bool cameraRelative = true;
    [Tooltip("Blocca l'altezza? (consigliato per i diorami)")]
    public bool lockY = true;

    [Header("Gating")]
    [Tooltip("Indice stazione nel CameraDirector: attivo solo se e' la corrente")]
    public int stationIndex = -1;

    Vector3 velocity;       
    Vector3 velRef;     
    Vector3 home;   
    float fixedY;

    void Start()
    {
        home = transform.position;
        fixedY = home.y;
        if (navBounds == null)
            Debug.LogError("[DioramaNavigator] navBounds mancante su " + name, this);
    }

    void OnEnable()
    {
        transform.position = home;
        velocity = Vector3.zero;
    }

    void Update()
    {
        if (navBounds == null) return;
        if (PauseMenuManager.Instance != null &&
            PauseMenuManager.Instance.isPaused) return;
        if (GameManager.instance == null ||
            !GameManager.instance.gameplayActive) return;
        if (GameManager.instance.IsChangingLevel) return;
        if (stationIndex >= 0 && CameraDirector.Instance != null &&
            CameraDirector.Instance.Current != stationIndex) return;

        float h = Input.GetAxisRaw("Horizontal");   
        float v = Input.GetAxisRaw("Vertical");    
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();  

        Vector3 wish;
        if (cameraRelative && Camera.main != null)
        {
            Vector3 fwd = Camera.main.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Camera.main.transform.right; right.y = 0f; right.Normalize();
            wish = fwd * input.z + right * input.x;
        }
        else wish = input;

        Vector3 targetVel = wish * moveSpeed;
        velocity = Vector3.SmoothDamp(velocity, targetVel, ref velRef, accelTime);

        Vector3 next = transform.position + velocity * Time.deltaTime;

        Bounds b = navBounds.bounds;
        next.x = Mathf.Clamp(next.x, b.min.x, b.max.x);
        next.z = Mathf.Clamp(next.z, b.min.z, b.max.z);
        next.y = lockY ? fixedY : Mathf.Clamp(next.y, b.min.y, b.max.y);

        if (Mathf.Approximately(next.x, b.min.x) ||
            Mathf.Approximately(next.x, b.max.x)) velocity.x = 0f;
        if (Mathf.Approximately(next.z, b.min.z) ||
            Mathf.Approximately(next.z, b.max.z)) velocity.z = 0f;

        transform.position = next;
    }

    public void ResetView()
    {
        transform.position = home;
        velocity = Vector3.zero;
    }
}