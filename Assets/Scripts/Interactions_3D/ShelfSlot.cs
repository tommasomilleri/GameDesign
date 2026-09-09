using UnityEngine;

public class Shelfslot : MonoBehaviour 
{
    [Header("Manager")]
    public Level5Manager level5Manager;
    public int slotNumber;

    [Header("Fattori Ambientali 3D")]
    [Tooltip("Trascina qui l'oggetto 3D del Ventilatore")]
    public Transform fanObject;
    [Tooltip("Trascina qui l'oggetto 3D della Candela/Fuoco")]
    public Transform heaterObject;

    [Header("Tolleranze (Distanza in Metri 3D)")]
    public float fanMinDistance = 1f;
    public float fanMaxDistance = 3f;
    public float heaterMinDistance = 4f;
    public float heaterMaxDistance = 8f;

    void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto ha l'etichetta del formaggio
        if (other.GetComponentInParent<CheeseTag>() != null)
        {
            // Misura la distanza reale in 3D!
            float distToFan = Vector3.Distance(other.transform.position, fanObject.position);
            float distToHeater = Vector3.Distance(other.transform.position, heaterObject.position);

            // Stampa le distanze nella Console per aiutarti a bilanciare i numeri!
            Debug.Log($"Distanza Ventilatore: {distToFan} | Distanza Fuoco: {distToHeater}");

            bool fanIsCorrect = (distToFan >= fanMinDistance && distToFan <= fanMaxDistance);
            bool heaterIsCorrect = (distToHeater >= heaterMinDistance && distToHeater <= heaterMaxDistance);

            if (fanIsCorrect && heaterIsCorrect)
            {
                Debug.Log("VITTORIA! Formaggio posizionato perfettamente.");
                if (level5Manager != null) level5Manager.CheckPosition(slotNumber);
            }
            else
            {
                Debug.Log("Sulla mensola, ma ambiente sbagliato (troppo vicino o troppo lontano)!");
                // Qui puoi chiamare una penalità o far fare un rumore di errore
            }
        }
    }
}