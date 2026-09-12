
using UnityEngine;

public class PotTrigger : MonoBehaviour
{
    public Level3Manager manager;

    void OnTriggerEnter(Collider other)
    {
        // Filtro rapido: senza Rigidbody non e' un oggetto di gioco
        if (other.attachedRigidbody == null) return;

        var ingredient = other.GetComponentInParent<IngredientID>();
        if (ingredient == null || manager == null) return;

        manager.CheckIngredient(ingredient, other.attachedRigidbody);
    }
}
