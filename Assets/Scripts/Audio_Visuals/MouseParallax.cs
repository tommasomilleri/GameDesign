using UnityEngine;

public class MouseParallax : MonoBehaviour
{
    [Tooltip("Intensità del movimento in pixel (Es: 8 per sfondi, 14 per oggetti vicini)")]
    public float parallaxStrength = 10f;

    [Tooltip("Quanto velocemente l'oggetto insegue il mouse")]
    public float smoothSpeed = 0.1f;

    private Vector3 startPos;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Posizione del mouse da 0 a 1 sullo schermo
        Vector2 mousePos01 = new Vector2(
            Input.mousePosition.x / Screen.width,
            Input.mousePosition.y / Screen.height
        );

        // Calcola l'offset (-0.5 / +0.5) moltiplicato per la forza
        Vector2 offset = (mousePos01 - new Vector2(0.5f, 0.5f)) * parallaxStrength;

        // Invertiamo l'asse per dare la sensazione che la "telecamera" giri
        Vector3 targetPos = startPos - new Vector3(offset.x, offset.y, 0f);

        // Movimento fluido elastico
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref velocity, smoothSpeed);
    }
}