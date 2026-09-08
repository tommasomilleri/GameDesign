using UnityEngine;

public class PotTrigger : MonoBehaviour
{
    public Level3Manager manager;

    void OnTriggerEnter(Collider other)
    {
        // 1. Cerca il TUO componente IngredientID sulla fiala che è appena entrata
        var ingredient = other.GetComponentInParent<IngredientID>();

        // 2. Prende anche il Rigidbody della fiala (ci serve per il rimbalzo!)
        var rb = other.GetComponentInParent<Rigidbody>();

        // 3. Se è davvero un ingrediente e il manager è collegato...
        if (ingredient != null && manager != null)
        {
            // ...chiama la TUA funzione passando l'ID e la fisica!
            manager.CheckIngredient(ingredient, rb);
        }
    }
}