using UnityEngine;

public class StationActivator : MonoBehaviour
{
    [Header("Indice della Stazione")]
    [Tooltip("0=Mucche, 1=Pentola, 2=Fiale, 3=Pressa, 4=Mensole")]
    public int stationIndex;

    void OnEnable()
    {
        Debug.Log("[STATION ACTIVATOR] La postazione " + gameObject.name + " è ATTIVA. Avviso il Regista!");

        // Invece di accendere la camera a mano, diciamo al Regista globale di andarci dolcemente!
        if (CameraDirector.Instance != null)
        {
            CameraDirector.Instance.GoTo(stationIndex);
        }
    }
}