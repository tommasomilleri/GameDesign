using UnityEngine;
public class IngredientID : MonoBehaviour
{
    [Tooltip("Deve combaciare ESATTAMENTE con il manuale (es. Starter Culture, Rennet)")]
    public string ingredientName;

    private Vector3 startPos;
    private Quaternion startRot;

    void Awake()
    {
        
        startPos = transform.position;
        startRot = transform.rotation;
    }
    public void ResetPosition(Rigidbody rb)
    {
        transform.position = startPos;
        transform.rotation = startRot;

        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}