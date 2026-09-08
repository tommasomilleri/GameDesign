using UnityEngine;

public class PhysicsGrabber : MonoBehaviour
{
    public float maxThrowSpeed = 5f;
    Camera cam;
    Rigidbody held;
    Plane dragPlane;
    Vector3 lastPos;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && held == null)
        {
            Ray r = cam.ScreenPointToRay(Input.mousePosition);

            // CORREZIONE: Mathf.Infinity per poter afferrare oggetti lontani
            if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity) && hit.rigidbody != null && hit.rigidbody.CompareTag("Grabbable"))
            {
                held = hit.rigidbody;
                held.isKinematic = true;
                dragPlane = new Plane(-cam.transform.forward, held.position);
            }
        }

        if (held != null)
        {
            Ray r = cam.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(r, out float d))
            {
                Vector3 target = r.GetPoint(d);
                lastPos = held.position;
                held.MovePosition(Vector3.Lerp(held.position, target, 0.35f));
            }

            if (Input.GetMouseButtonUp(0))
            {
                held.isKinematic = false;
                Vector3 v = (held.position - lastPos) / Time.deltaTime;
                held.linearVelocity = Vector3.ClampMagnitude(v, maxThrowSpeed); // Aggiornato a linearVelocity per Unity 6
                held = null;
            }
        }
    }
}