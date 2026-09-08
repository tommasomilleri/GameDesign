using UnityEngine;

public class IngredientDropZone : MonoBehaviour
{
    [Tooltip("Trascina qui l'oggetto con il Level3Manager")]
    public Level3Manager level3Manager;

    // NEL 3D USIAMO I TRIGGER FISICI, NON IL MOUSE DROP UI!
    void OnTriggerEnter(Collider other)
    {
        // 1. Controlla se l'oggetto entrato è un ingrediente leggendo il suo ID
        IngredientID ingredient = other.GetComponent<IngredientID>();

        // 2. Se è davvero un ingrediente, lo inviamo al Manager
        if (ingredient != null && level3Manager != null)
        {
            // Prendiamo anche il Rigidbody così il Manager può farlo rimbalzare via se è sbagliato!
            Rigidbody rb = other.GetComponent<Rigidbody>();

            level3Manager.CheckIngredient(ingredient, rb);
        }
        else if (level3Manager == null)
        {
            Debug.LogWarning("Attenzione: Level3Manager non assegnato nella DropZone!");
        }
    }
}