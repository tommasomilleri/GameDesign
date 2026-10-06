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
        while (CameraDirector.Instance == null) yield return null;
        Debug.Log("[STATION ACTIVATOR] " + gameObject.name + " → GoTo(" + stationIndex + ")");

        // La station 0 passa prima dalla carrellata introduttiva.
        // Versione a prova di bug visivo: non usa < e >
        var intro = (IntroDolly)FindFirstObjectByType(typeof(IntroDolly));

        if (stationIndex == 0 && intro != null)
            intro.PlayIntro();                 // fara' lui GoTo(0) alla fine
        else
            CameraDirector.Instance.GoTo(stationIndex);
    }
}