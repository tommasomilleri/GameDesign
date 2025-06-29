using UnityEngine;

public class ShrinkingZone : MonoBehaviour
{
    public float startRadius = 20f;
    public float endRadius = 5f;
    public float shrinkDuration = 60f;

    public float damagePerSecond = 5f;
    public float damageInterval = 1f;
    public float fixedHeight = 100f;  // Altezza fissa

    public Door door;  // assegna la porta in inspector

    private float currentRadius;
    private float shrinkTimer = 0f;
    private float damageTimer = 0f;

    private Transform zoneVisual;
    private Renderer zoneRenderer;

    private bool doorOpenedOnShrink = false;

    void Start()
    {
        zoneVisual = transform;
        zoneRenderer = GetComponent<Renderer>(); // prendi il renderer del cilindro

        currentRadius = startRadius;
        UpdateScale();
    }

    void Update()
    {
        shrinkTimer += Time.deltaTime;
        float t = Mathf.Clamp01(shrinkTimer / shrinkDuration);
        currentRadius = Mathf.Lerp(startRadius, endRadius, t);
        UpdateScale();

        damageTimer += Time.deltaTime;
        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;
            ApplyDamageOutsideZone();
        }

        if (t >= 1f && door != null && !doorOpenedOnShrink)
        {
            door.ForceOpen();
            door.MoveCamera();
            doorOpenedOnShrink = true;
            Debug.Log("Cerchio finito: porta aperta, camera spostata");

            // Disattiva il renderer per nascondere il cilindro
            if (zoneRenderer != null)
            {
                zoneRenderer.enabled = false;
                Debug.Log("Renderer del cerchio disattivato");
            }
        }
    }

    void UpdateScale()
    {
        zoneVisual.localScale = new Vector3(currentRadius * 2f, fixedHeight, currentRadius * 2f);
    }

    void ApplyDamageOutsideZone()
    {
        GameObject[] players = AppendPlayers(GameObject.FindGameObjectsWithTag("Player"), "Player2");

        foreach (GameObject player in players)
        {
            float distance = Vector2.Distance(
                new Vector2(player.transform.position.x, player.transform.position.z),
                new Vector2(transform.position.x, transform.position.z));

            if (distance > currentRadius)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(Mathf.RoundToInt(damagePerSecond));
                    Debug.Log($"{player.name} è fuori dalla zona! Danni applicati.");
                }
            }
        }
    }

    GameObject[] AppendPlayers(GameObject[] baseList, string extraTag)
    {
        GameObject[] extra = GameObject.FindGameObjectsWithTag(extraTag);
        GameObject[] combined = new GameObject[baseList.Length + extra.Length];
        baseList.CopyTo(combined, 0);
        extra.CopyTo(combined, baseList.Length);
        return combined;
    }
}








