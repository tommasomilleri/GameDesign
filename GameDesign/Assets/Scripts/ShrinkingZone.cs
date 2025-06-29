using UnityEngine;

public class ShrinkingZone : MonoBehaviour
{
    public float startRadius = 20f;
    public float endRadius = 5f;
    public float shrinkDuration = 60f;

    public float damagePerSecond = 5f;
    public float damageInterval = 1f;
    public float fixedHeight = 100f;  // Altezza fissa

    private float currentRadius;
    private float shrinkTimer = 0f;
    private float damageTimer = 0f;

    private Transform zoneVisual;

    void Start()
    {
        zoneVisual = transform;
        currentRadius = startRadius;
        UpdateScale();  // imposta scala con Y fissa
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
    }

    void UpdateScale()
    {
        zoneVisual.localScale = new Vector3(currentRadius * 2f, 200f, currentRadius * 2f);
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


