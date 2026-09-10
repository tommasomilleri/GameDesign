using UnityEngine;

[RequireComponent(typeof(Light))]
public class StationLight : MonoBehaviour
{
    [Tooltip("Indice della stazione (0 = ST1, 1 = ST2, ecc.)")]
    public int stationIndex;
    public float activeIntensity = 1.0f;
    public float inactiveIntensity = 0.4f;
    public float lerpSpeed = 3f;

    private Light myLight;

    void Awake()
    {
        myLight = GetComponent<Light>();
    }

    void Update()
    {
        // Trova quale stazione sta guardando attualmente il regista
        int currentStation = CameraDirector.Instance != null ? CameraDirector.Instance.Current : 0;

        // Scegli l'intensità in base a dove ci troviamo
        float target = (currentStation == stationIndex) ? activeIntensity : inactiveIntensity;

        // Transizione morbida (il respiro della stanza quando cambi visuale)
        myLight.intensity = Mathf.Lerp(myLight.intensity, target, Time.deltaTime * lerpSpeed);
    }
}