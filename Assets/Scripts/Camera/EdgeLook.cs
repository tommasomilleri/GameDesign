using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Camera/Edge Look")]
public class EdgeLook : MonoBehaviour
{
    [System.Serializable]
    public class TargetSettings
    {
        public Transform target;

        [Tooltip("Nome descrittivo, solo per orientarsi nell'Inspector.")]
        public string label = "";

        [Tooltip("Se ON usa limiti e campo visivo specifici per questa " +
                 "inquadratura invece di quelli globali.")]
        public bool useOverrides = false;

        [Min(0f)] public float maxPanX = 1.5f;
        [Min(0f)] public float maxPanY = 1.0f;

        [Tooltip("Campo visivo in gradi per questa inquadratura. " +
                 "Zero lascia quello della virtual camera.")]
        [Range(0f, 120f)] public float fieldOfView = 0f;

        [Tooltip("Disattiva completamente il pan su questa inquadratura. " +
                 "Utile per primi piani o inquadrature fisse.")]
        public bool lockPan = false;
    }

    [Header("Bersagli")]
    [Tooltip("I LookTarget, nello stesso ordine delle virtual camera.")]
    public TargetSettings[] targets = new TargetSettings[0];

    [Header("Limiti globali")]
    [Tooltip("Spostamento massimo orizzontale, in unita' di scena. " +
             "Se la scena e' a scala 100, anche questo valore va scalato.")]
    [Min(0f)] public float maxPanX = 1.5f;

    [Min(0f)] public float maxPanY = 1.0f;

    [Header("Campo visivo")]
    [Tooltip("Se ON forza il campo visivo della camera attiva. " +
             "Serve a stringere l'inquadratura quando entrano nel campo " +
             "oggetti che non c'entrano con la scena.")]
    public bool overrideFieldOfView = false;

    [Range(10f, 120f)]
    [Tooltip("Campo visivo in gradi. Valori BASSI stringono l'inquadratura " +
             "e comprimono la prospettiva, che e' quasi sempre cio' che vuoi " +
             "per una scena curata. 35-50 e' il campo cinematografico.")]
    public float fieldOfView = 45f;

    [Tooltip("Per camere ortografiche: dimensione verticale invece del FOV.")]
    [Min(0.01f)] public float orthographicSize = 5f;

    [Header("Zona attiva")]
    [Tooltip("Spessore del bordo sensibile in PERCENTUALE dello schermo. " +
             "Usare una percentuale invece dei pixel mantiene lo stesso " +
             "comportamento a qualunque risoluzione.")]
    [Range(0.01f, 0.35f)] public float edgeZone = 0.08f;

    [Tooltip("Se ON l'intensita' cresce gradualmente entrando nella zona di " +
             "bordo, invece di scattare da zero a uno.")]
    public bool gradualEdge = true;

    [Range(0f, 0.4f)]
    [Tooltip("Zona morta attorno al centro dello schermo. Impedisce che " +
             "micro movimenti del mouse facciano vibrare l'inquadratura.")]
    public float deadZone = 0.02f;

    [Header("Velocita'")]
    [Min(0.01f)] public float panSpeed = 2f;

    [Tooltip("Quanto rapidamente raggiunge la velocita' piena. " +
             "Valori bassi danno una partenza piu' dolce.")]
    [Min(0.1f)] public float acceleration = 4f;

    [Tooltip("Quanto rapidamente si ferma quando lasci il bordo.")]
    [Min(0.1f)] public float deceleration = 6f;

    [Header("Ritorno al centro")]
    public bool autoCenter = true;

    [Min(0f)] public float centerReturnSpeed = 1.2f;

    [Tooltip("Secondi di attesa prima che inizi il rientro. Senza questa " +
             "pausa la camera rimbalza indietro appena ti fermi, e da' la " +
             "sensazione di combattere col controllo.")]
    [Min(0f)] public float centerDelay = 0.6f;

    [Header("Frenata ai limiti")]
    [Range(0f, 0.8f)]
    [Tooltip("Frazione del percorso su cui la camera decelera avvicinandosi " +
             "al limite. Senza, si schianta contro il limite di colpo.")]
    public float softLimitZone = 0.30f;

    [Header("Respiro")]
    [Tooltip("Oscillazione lentissima e continua, come una camera tenuta a " +
             "mano. Toglie la rigidita' del treppiede: e' il singolo " +
             "dettaglio che rende una camera 'viva' piu' di ogni altro.")]
    public bool idleSway = true;

    [Min(0f)]
    [Tooltip("Ampiezza del respiro, in unita' di scena. Deve essere una " +
             "frazione piccolissima di maxPan: se lo noti, e' troppo.")]
    public float swayAmount = 0.04f;

    [Min(0.01f)]
    [Tooltip("Periodo del respiro in secondi. 6-12 e' il ritmo naturale.")]
    public float swayPeriod = 8f;

    [Header("Blocco")]
    [Tooltip("Torna al centro e blocca il pan mentre tieni un oggetto.")]
    public bool lockWhileHolding = true;

    [Min(0f)] public float holdReturnSpeed = 4f;

    [Header("Anteprima (solo editor)")]
    [Tooltip("Sposta l'inquadratura senza premere Play, per verificare cosa " +
             "si vede ai limiti del pan. Da -1 a 1 su ciascun asse.")]
    public Vector2 previewOffset = Vector2.zero;

    [Tooltip("Applica il pan anche fuori dal Play, seguendo previewOffset.")]
    public bool livePreview = true;

    [Header("Debug")]
    public bool logWarnings = true;

    [Tooltip("Disegna nella Scene view il volume inquadrato ai limiti del pan.")]
    public bool drawFrustumGizmo = true;

    Vector3[] basePos;
    Vector2 offset;
    Vector2 velocity;
    Vector2 shakeOffset;
    float shakeAmplitude, shakeDecay;
    float idleTimer;
    float swaySeed;
    int lastIndex = -1;
    bool ready;

    PhysicsGrabber grabber;
    bool grabberSearched;

    void OnEnable()
    {
        swaySeed = Random.Range(0f, 100f);
        ready = Initialize();
        ApplyFieldOfView();
    }

    void OnDisable()
    {
        RestoreAllTargets();
        offset = Vector2.zero;
        velocity = Vector2.zero;
    }

    bool Initialize()
    {
        if (targets == null || targets.Length == 0)
        {
            if (logWarnings && Application.isPlaying)
                Debug.LogError("[EdgeLook] Nessun LookTarget assegnato.", this);
            return false;
        }

        basePos = new Vector3[targets.Length];

        bool anyValid = false;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || targets[i].target == null) continue;
            basePos[i] = targets[i].target.localPosition;
            anyValid = true;
        }

        return anyValid;
    }
    void LateUpdate()
    {
        if (!ready)
        {
            ready = Initialize();
            if (!ready) return;
        }
        if (!Application.isPlaying)
        {
            if (livePreview) ApplyPreview();
            return;
        }

        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        dt = Mathf.Min(dt, 0.05f);

        if (IsBlocked()) ReturnToCenter(dt, holdReturnSpeed);
        else UpdatePan(dt);

        UpdateShake(dt);
        ApplyToTarget();
    }
    void UpdatePan(float dt)
    {
        int index = GetActiveIndex();
        bool locked = index >= 0 && targets[index].lockPan;

        Vector2 input = locked ? Vector2.zero : ReadEdgeInput();

        float limX = GetMaxPanX(index);
        float limY = GetMaxPanY(index);

        Vector2 desired = input * panSpeed;

        desired.x *= LimitBrake(offset.x, limX, desired.x);
        desired.y *= LimitBrake(offset.y, limY, desired.y);

        bool pushing = input.sqrMagnitude > 0.0001f;
        float rate = pushing ? acceleration : deceleration;
        float k = 1f - Mathf.Exp(-rate * dt);
        velocity = Vector2.Lerp(velocity, desired, k);

        offset += velocity * dt;

        if (pushing)
        {
            idleTimer = 0f;
        }
        else
        {
            idleTimer += dt;

            if (autoCenter && centerReturnSpeed > 0f && idleTimer >= centerDelay)
            {
                float ck = 1f - Mathf.Exp(-centerReturnSpeed * dt);
                offset = Vector2.Lerp(offset, Vector2.zero, ck);

                velocity = Vector2.Lerp(velocity, Vector2.zero, ck);
            }
        }
        offset.x = Mathf.Clamp(offset.x, -limX, limX);
        offset.y = Mathf.Clamp(offset.y, -limY, limY);

        if (limX > 0f && Mathf.Abs(offset.x) >= limX - 0.0001f &&
            Mathf.Sign(velocity.x) == Mathf.Sign(offset.x)) velocity.x = 0f;

        if (limY > 0f && Mathf.Abs(offset.y) >= limY - 0.0001f &&
            Mathf.Sign(velocity.y) == Mathf.Sign(offset.y)) velocity.y = 0f;
    }

    Vector2 ReadEdgeInput()
    {
        Vector3 mouse = Input.mousePosition;

        if (mouse.x < 0f || mouse.y < 0f ||
            mouse.x > Screen.width || mouse.y > Screen.height)
            return Vector2.zero;

        if (Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;

        return new Vector2(
            AxisInput(mouse.x / Screen.width),
            AxisInput(mouse.y / Screen.height));
    }
    float AxisInput(float n)
    {
        if (Mathf.Abs(n - 0.5f) < deadZone * 0.5f) return 0f;

        float zone = Mathf.Clamp(edgeZone, 0.005f, 0.45f);

        if (n <= zone)
        {
            if (!gradualEdge) return -1f;
            return -Smooth(1f - (n / zone));
        }

        if (n >= 1f - zone)
        {
            if (!gradualEdge) return 1f;
            return Smooth((n - (1f - zone)) / zone);
        }

        return 0f;
    }
    float LimitBrake(float current, float max, float direction)
    {
        if (max <= 0f) return 0f;
        if (softLimitZone <= 0f) return 1f;

        if (Mathf.Sign(direction) != Mathf.Sign(current) &&
            Mathf.Abs(current) > 0.0001f) return 1f;

        float normalized = Mathf.Abs(current) / max;
        float brakeStart = 1f - softLimitZone;
        if (normalized <= brakeStart) return 1f;

        float t = (normalized - brakeStart) / softLimitZone;
        return Smooth(1f - Mathf.Clamp01(t));
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    void ReturnToCenter(float dt, float speed)
    {
        if (speed <= 0f)
        {
            offset = Vector2.zero;
            velocity = Vector2.zero;
            return;
        }

        float k = 1f - Mathf.Exp(-speed * dt);
        offset = Vector2.Lerp(offset, Vector2.zero, k);
        velocity = Vector2.Lerp(velocity, Vector2.zero, k);
        idleTimer = 0f;
    }
    Vector2 GetSway()
    {
        if (!idleSway || swayAmount <= 0f) return Vector2.zero;

        float t = (Application.isPlaying ? Time.unscaledTime : 0f)
                  / Mathf.Max(0.01f, swayPeriod);
        float x = (Mathf.PerlinNoise(t, swaySeed) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(t * 1.37f, swaySeed + 41f) - 0.5f) * 2f;

        return new Vector2(x, y * 0.7f) * swayAmount;
    }

    void UpdateShake(float dt)
    {
        if (shakeAmplitude <= 0.0001f)
        {
            shakeOffset = Vector2.zero;
            return;
        }

        float t = Time.unscaledTime * 25f;
        shakeOffset = new Vector2(
            (Mathf.PerlinNoise(t, 7.1f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(t, 31.7f) - 0.5f) * 2f) * shakeAmplitude;

        shakeAmplitude = Mathf.Max(0f, shakeAmplitude - shakeDecay * dt);
    }

    public void Shake(float amplitude, float duration)
    {
        shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
        shakeDecay = amplitude / Mathf.Max(0.05f, duration);
    }

    void ApplyToTarget()
    {
        int index = GetActiveIndex();
        if (index < 0) return;

        HandleCameraSwitch(index);

        Vector2 total = offset + GetSway() + shakeOffset;

        targets[index].target.localPosition =
            basePos[index] + new Vector3(total.x, total.y, 0f);
    }

    void ApplyPreview()
    {
        int index = GetActiveIndex();
        if (index < 0) return;

        HandleCameraSwitch(index);

        Vector2 p = new Vector2(
            Mathf.Clamp(previewOffset.x, -1f, 1f) * GetMaxPanX(index),
            Mathf.Clamp(previewOffset.y, -1f, 1f) * GetMaxPanY(index));

        targets[index].target.localPosition =
            basePos[index] + new Vector3(p.x, p.y, 0f);
    }
    void HandleCameraSwitch(int index)
    {
        if (lastIndex >= 0 && lastIndex != index &&
            lastIndex < targets.Length &&
            targets[lastIndex] != null && targets[lastIndex].target != null)
        {
            targets[lastIndex].target.localPosition = basePos[lastIndex];
        }

        if (lastIndex != index)
        {
            lastIndex = index;
            ApplyFieldOfView();
        }
    }

    void RestoreAllTargets()
    {
        if (basePos == null || targets == null) return;

        int n = Mathf.Min(basePos.Length, targets.Length);
        for (int i = 0; i < n; i++)
            if (targets[i] != null && targets[i].target != null)
                targets[i].target.localPosition = basePos[i];
    }
    public void ApplyFieldOfView()
    {
        float fov = fieldOfView;
        bool apply = overrideFieldOfView;

        int index = GetActiveIndex();
        if (index >= 0 && targets[index].useOverrides &&
            targets[index].fieldOfView > 0f)
        {
            fov = targets[index].fieldOfView;
            apply = true;
        }

        if (!apply) return;

        if (TrySetCinemachineFov(fov)) return;

        Camera cam = GetPreviewCamera();
        if (cam == null) return;

        if (cam.orthographic) cam.orthographicSize = orthographicSize;
        else cam.fieldOfView = fov;
    }

    bool TrySetCinemachineFov(float fov)
    {
        Component vcam = FindActiveVirtualCamera();
        if (vcam == null) return false;

        System.Type t = vcam.GetType();

        System.Reflection.FieldInfo lensField =
            t.GetField("m_Lens") ?? t.GetField("Lens");

        if (lensField == null) return false;

        object lens = lensField.GetValue(vcam);
        if (lens == null) return false;

        System.Reflection.FieldInfo fovField =
            lens.GetType().GetField("FieldOfView");

        System.Reflection.FieldInfo orthoField =
            lens.GetType().GetField("OrthographicSize");

        bool changed = false;

        if (fovField != null)
        {
            fovField.SetValue(lens, fov);
            changed = true;
        }

        if (orthoField != null)
        {
            orthoField.SetValue(lens, orthographicSize);
            changed = true;
        }

        if (!changed) return false;
        lensField.SetValue(vcam, lens);
        return true;
    }
    Component FindActiveVirtualCamera()
    {
        int index = GetActiveIndex();
        if (index < 0) return null;

        Transform t = targets[index].target;
        Transform search = t;
        for (int depth = 0; depth < 4 && search != null; depth++)
        {
            Component c = FindVcamOn(search.gameObject);
            if (c != null) return c;

            if (search.parent != null)
            {
                for (int i = 0; i < search.parent.childCount; i++)
                {
                    c = FindVcamOn(search.parent.GetChild(i).gameObject);
                    if (c != null) return c;
                }
            }

            search = search.parent;
        }

        return null;
    }

    static Component FindVcamOn(GameObject go)
    {
        Component[] comps = go.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;

            string n = comps[i].GetType().Name;
            if (n == "CinemachineVirtualCamera" || n == "CinemachineCamera")
                return comps[i];
        }
        return null;
    }
    public Camera GetPreviewCamera()
    {
        Camera cam = Camera.main;
        if (cam != null) return cam;

#if UNITY_2022_2_OR_NEWER
        Camera[] all = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
        Camera[] all = Object.FindObjectsOfType<Camera>();
#endif
        for (int i = 0; i < all.Length; i++)
            if (all[i].enabled && all[i].targetTexture == null) return all[i];

        return null;
    }
    int GetActiveIndex()
    {
        if (targets == null || targets.Length == 0) return -1;

        int i = 0;
        if (CameraDirector.Instance != null) i = CameraDirector.Instance.Current;

        if (i < 0 || i >= targets.Length) return -1;
        if (targets[i] == null || targets[i].target == null) return -1;

        if (basePos == null || basePos.Length != targets.Length) return -1;

        return i;
    }

    float GetMaxPanX(int index)
    {
        if (index >= 0 && targets[index].useOverrides) return targets[index].maxPanX;
        return maxPanX;
    }

    float GetMaxPanY(int index)
    {
        if (index >= 0 && targets[index].useOverrides) return targets[index].maxPanY;
        return maxPanY;
    }

    bool IsBlocked()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
            return true;

        if (!lockWhileHolding) return false;
        if (grabber == null)
        {
            if (!grabberSearched)
            {
                grabber = Object.FindFirstObjectByType<PhysicsGrabber>();
                grabberSearched = true;
            }
            return false;
        }

        return grabber.IsHolding;
    }
    public Vector2 CurrentOffset { get { return offset; } }

    public void SnapToCenter()
    {
        offset = Vector2.zero;
        velocity = Vector2.zero;
        shakeAmplitude = 0f;
        idleTimer = 0f;
        ApplyToTarget();
    }
    public void RecaptureBasePositions()
    {
        RestoreAllTargets();
        ready = Initialize();
        SnapToCenter();
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxPanX < 0f) maxPanX = 0f;
        if (maxPanY < 0f) maxPanY = 0f;
        if (panSpeed < 0.01f) panSpeed = 0.01f;
        if (acceleration < 0.1f) acceleration = 0.1f;
        if (deceleration < 0.1f) deceleration = 0.1f;
        if (swayPeriod < 0.01f) swayPeriod = 0.01f;

        previewOffset.x = Mathf.Clamp(previewOffset.x, -1f, 1f);
        previewOffset.y = Mathf.Clamp(previewOffset.y, -1f, 1f);

        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += DelayedEditorRefresh;
        }
    }

    void DelayedEditorRefresh()
    {
        if (this == null) return;

        ready = Initialize();
        ApplyFieldOfView();
        if (livePreview) ApplyPreview();

        SceneView.RepaintAll();
    }

    void OnDrawGizmosSelected()
    {
        if (!drawFrustumGizmo) return;

        int index = GetActiveIndex();
        if (index < 0) return;

        Transform t = targets[index].target;
        Vector3 center = t.parent != null
            ? t.parent.TransformPoint(basePos[index])
            : basePos[index];

        float lx = GetMaxPanX(index);
        float ly = GetMaxPanY(index);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = t.parent != null
            ? Matrix4x4.TRS(center, t.parent.rotation, Vector3.one)
            : Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(lx * 2f, ly * 2f, 0f));

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(lx * 2f, ly * 2f, 0.001f));

        Gizmos.matrix = old;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(t.position, Mathf.Max(lx, ly) * 0.06f);
    }
#endif
}