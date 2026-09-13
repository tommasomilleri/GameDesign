using UnityEngine;

public class IngredientID : MonoBehaviour
{
    [Tooltip("Deve combaciare ESATTAMENTE con il manuale (es. Starter Culture, Rennet)")]
    public string ingredientName;

    private Vector3 startPos;
    private Quaternion startRot;

    void Awake()
    {
        // Memorizza la posizione e rotazione iniziale all'avvio del livello
        startPos = transform.position;
        startRot = transform.rotation;
    }

    public void ResetPosition(Rigidbody rb)
    {
        // Riporta l'oggetto al suo posto
        transform.position = startPos;
        transform.rotation = startRot;

        // Azzera la fisica per evitare che continui a muoversi
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}