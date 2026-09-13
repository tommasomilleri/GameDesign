
/*using UnityEngine;

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
}*/
using UnityEngine;

public class PotTrigger : MonoBehaviour
{
    public Level3Manager manager;
    private PhysicsGrabber grabber;

    void Start()
    {
        // Sintassi sicura senza parentesi angolari
        grabber = (PhysicsGrabber)FindFirstObjectByType(typeof(PhysicsGrabber));
    }

    void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody == null) return;

        // Se l'oggetto è tenuto in mano dal giocatore, ignoralo
        if (grabber != null && grabber.IsHolding && grabber.HeldRigidbody == other.attachedRigidbody)
            return;

        // Sintassi sicura senza parentesi angolari
        var ingredient = (IngredientID)other.GetComponentInParent(typeof(IngredientID));

        if (ingredient == null || manager == null) return;

        manager.CheckIngredient(ingredient, other.attachedRigidbody);
    }
}
