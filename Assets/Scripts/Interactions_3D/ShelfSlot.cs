using UnityEngine;

public class Shelfslot : MonoBehaviour
{
    [Tooltip("Il numero di questa mensola (es. 1, 2, 3 dal basso verso l'alto)")]
    public int slotNumber;

    public Level5Manager level5Manager;

    // Questa funzione scatta automaticamente quando un oggetto 3D attraversa il sensore
    void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto che è appena atterrato ha la nostra "etichetta" del formaggio
        if (other.GetComponentInParent<CheeseTag>() != null && level5Manager != null)
        {
            // Se è il formaggio, comunica al manager su quale mensola (1, 2 o 3) si è appoggiato!
            level5Manager.CheckPosition(slotNumber);
        }
    }
}