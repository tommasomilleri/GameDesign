using UnityEngine;

public class PotTrigger : MonoBehaviour
{
    public Level3Manager manager;
    private PhysicsGrabber grabber;

    void Start()
    {
        grabber = (PhysicsGrabber)FindFirstObjectByType(typeof(PhysicsGrabber));
    }

    void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody == null) return;

        if (grabber != null && grabber.IsHolding && grabber.HeldRigidbody == other.attachedRigidbody)
            return;

        var ingredient = (IngredientID)other.GetComponentInParent(typeof(IngredientID));

        if (ingredient == null || manager == null) return;

        manager.CheckIngredient(ingredient, other.attachedRigidbody);
    }
}
