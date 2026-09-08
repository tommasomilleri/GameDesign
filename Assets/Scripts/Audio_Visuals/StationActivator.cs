using UnityEngine;

public class StationActivator : MonoBehaviour
{
    [Header("Telecamera della Stanza")]
    [Tooltip("Trascina qui la VCam di Cinemachine corrispondente a questa postazione (es. VCam_Cows)")]
    public GameObject myVirtualCamera;

    void OnEnable()
    {
        Debug.Log("[STATION ACTIVATOR] La postazione " + gameObject.name + " è ATTIVA. Accendo la sua telecamera!");

        // Accende la telecamera di questa stanza appena il GameManager entra in questo livello
        if (myVirtualCamera != null)
        {
            myVirtualCamera.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Manca la VCam nello script StationActivator di " + gameObject.name);
        }
    }

    void OnDisable()
    {
        Debug.Log("[STATION ACTIVATOR] La postazione " + gameObject.name + " è SPENTA. Spengo la sua telecamera.");

        // Spegne la telecamera di questa stanza quando si cambia livello (es. premendo F12)
        if (myVirtualCamera != null)
        {
            myVirtualCamera.SetActive(false);
        }
    }
}