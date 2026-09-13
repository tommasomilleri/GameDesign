using UnityEngine;

public class IngredientDropZone : MonoBehaviour
{
    [Tooltip("Trascina qui l'oggetto con il Level3Manager")]
    public Level3Manager level3Manager;
    void OnTriggerEnter(Collider other)
    {
        IngredientID ingredient = other.GetComponent<IngredientID>();

        if (ingredient != null && level3Manager != null)
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();

            level3Manager.CheckIngredient(ingredient, rb);
        }
        else if (level3Manager == null)
        {
            Debug.LogWarning("Attenzione: Level3Manager non assegnato nella DropZone!");
        }
    }
}