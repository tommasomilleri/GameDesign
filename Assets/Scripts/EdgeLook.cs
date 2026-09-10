using UnityEngine;

public class EdgeLook : MonoBehaviour
{
    [Tooltip("Gradi max di deviazione orizzontale/verticale")]
    public float maxYaw = 6f, maxPitch = 3f;
    [Tooltip("Frazione di schermo che attiva il pan (0.18 = 18% dal bordo)")]
    public float edgeZone = 0.18f;
    public float smooth = 4f;

    [Tooltip("Trascina qui i 5 LookTarget (da 1 a 5) in ordine esatto")]
    public Transform[] lookTargets;

    private Vector3[] basePos;
    private Vector2 cur;
    private PhysicsGrabber grabber;

    void Start()
    {
        // Salva le posizioni base di ogni bersaglio
        basePos = new Vector3[lookTargets.Length];
        for (int i = 0; i < lookTargets.Length; i++)
        {
            if (lookTargets[i] != null) basePos[i] = lookTargets[i].localPosition;
        }

        // Trova il grabber in scena
        grabber = Object.FindFirstObjectByType<PhysicsGrabber>();
    }

    void LateUpdate()
    {
        // Nessun movimento in pausa
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) return;

        bool isHolding = (grabber != null && grabber.IsHolding);

        // Se teniamo un oggetto, forziamo la camera a tornare al centro dolcemente
        if (isHolding)
        {
            cur = Vector2.Lerp(cur, Vector2.zero, Time.unscaledDeltaTime * smooth);
        }
        else
        {
            Vector2 m = new Vector2(
                Input.mousePosition.x / Screen.width,
                Input.mousePosition.y / Screen.height);

            // Calcola il pan: da -1 a 1 solo se siamo nella "edge zone", 0 se siamo al centro
            float x = Mathf.Clamp((Mathf.Abs(m.x - .5f) - (.5f - edgeZone)) / edgeZone, 0, 1) * Mathf.Sign(m.x - .5f);
            float y = Mathf.Clamp((Mathf.Abs(m.y - .5f) - (.5f - edgeZone)) / edgeZone, 0, 1) * Mathf.Sign(m.y - .5f);

            cur = Vector2.Lerp(cur, new Vector2(x, y), Time.unscaledDeltaTime * smooth);
        }

        int i = CameraDirector.Instance ? CameraDirector.Instance.Current : 0;
        if (i < 0 || i >= lookTargets.Length || lookTargets[i] == null) return;

        // Sposta fisicamente il bersaglio: spostandolo, la telecamera (in modalità Composer) ruoterà per seguirlo!
        // Il fattore 3f equivale circa alla distanza camera-bersaglio (regolalo a occhio se l'effetto è troppo forte/debole)
        lookTargets[i].localPosition = basePos[i] +
            new Vector3(cur.x * Mathf.Tan(maxYaw * Mathf.Deg2Rad) * 3f,
                        cur.y * Mathf.Tan(maxPitch * Mathf.Deg2Rad) * 3f, 0);
    }
}