using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Tooltip("Solo asse Y (consigliato per standee) o full")]
    public bool yAxisOnly = true;
    Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (yAxisOnly)
        {
            Vector3 dir = transform.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            transform.rotation = cam.transform.rotation;
        }
    }
}