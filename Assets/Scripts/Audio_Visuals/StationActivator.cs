using UnityEngine;

public class StationActivator : MonoBehaviour
{
    [Header("Indice della Stazione")]
    [Tooltip("0=Mucche, 1=Pentola, 2=Fiale, 3=Pressa, 4=Mensole")]
    public int stationIndex;

    void OnEnable()
    {
        StartCoroutine(NotifyDirector());
    }

    System.Collections.IEnumerator NotifyDirector()
    {
        // aspetta che il Regista esista (race di Awake al primo load)
        while (CameraDirector.Instance == null) yield return null;
        Debug.Log("[STATION ACTIVATOR] " + gameObject.name + " → GoTo(" + stationIndex + ")");
        CameraDirector.Instance.GoTo(stationIndex);
    }

}