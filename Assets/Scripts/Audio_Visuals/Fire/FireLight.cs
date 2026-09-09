using UnityEngine;

public class FireLight : MonoBehaviour
{
    public enum Kind { Candle, Torch, Fireplace }
    public Kind kind = Kind.Candle;

    [Header("Override (0 = usa preset)")]
    public float baseIntensity = 0f, baseRange = 0f;

    Light li;
    float seed, speed, amount, rangeAmt;
    Color cA, cB;
    Vector3 basePos; // Salva la posizione per far danzare le ombre

    void Awake()
    {
        li = GetComponent<Light>();
        basePos = transform.localPosition;

        // Seed molto più ampio per garantire che 2 candele vicine non siano MAI sincronizzate
        seed = Random.Range(0f, 10000f);

        switch (kind)
        {
            case Kind.Candle:     // Fiammella: veloce, piccola, colori molto contrastati
                Set(0.8f, 2.5f, 5.5f, .6f, .15f,
                    new Color(1f, .85f, .45f), new Color(1f, .50f, .15f)); break;
            case Kind.Torch:      // Media, nervosa, arancio vivo
                Set(1.6f, 5f, 8.0f, .5f, .20f,
                    new Color(1f, .65f, .30f), new Color(1f, .35f, .05f)); break;
            case Kind.Fireplace:  // Grande, profonda, rossa/arancio
                Set(2.8f, 9f, 2.5f, .4f, .25f,
                    new Color(1f, .60f, .28f), new Color(.85f, .25f, .02f)); break;
        }
        if (baseIntensity > 0) li.intensity = baseIntensity;
        if (baseRange > 0) li.range = baseRange;
    }

    void Set(float i, float r, float sp, float am, float ra, Color a, Color b)
    {
        li.intensity = i; li.range = r; speed = sp; amount = am;
        rangeAmt = ra; cA = a; cB = b;
    }

    void Update()
    {
        float t = Time.time;

        // 1. Respiro principale (morbido)
        float n = Mathf.PerlinNoise(t * speed, seed);
        // 2. Micro-tremolio (nervoso e scattante)
        float flicker = Mathf.PerlinNoise(seed, t * speed * 2.5f);
        // 3. Variazione del raggio della luce
        float n2 = Mathf.PerlinNoise(seed + 100f, t * speed * 0.7f);

        // Mixiamo il respiro con il tremolio per l'effetto definitivo
        float combinedNoise = Mathf.Lerp(n, flicker, 0.35f);

        float bi = baseIntensity > 0 ? baseIntensity : DefaultI();
        float br = baseRange > 0 ? baseRange : DefaultR();

        // Applica l'intensità nervosa
        li.intensity = bi * (1f + (combinedNoise - 0.5f) * 2f * amount);
        li.range = br * (1f + (n2 - 0.5f) * 2f * rangeAmt);

        // Il colore diventa più rosso/scuro quando l'intensità cala
        li.color = Color.Lerp(cB, cA, combinedNoise);

        // 4. TRUCCO PRO: Movimento fisico per far danzare le ombre
        float wobbleX = (Mathf.PerlinNoise(t * speed, seed + 50) - 0.5f) * 0.06f;
        float wobbleZ = (Mathf.PerlinNoise(t * speed, seed + 150) - 0.5f) * 0.06f;
        transform.localPosition = basePos + new Vector3(wobbleX, 0f, wobbleZ);
    }

    float DefaultI() => kind == Kind.Candle ? .8f : kind == Kind.Torch ? 1.6f : 2.8f;
    float DefaultR() => kind == Kind.Candle ? 2.5f : kind == Kind.Torch ? 5f : 9f;
}