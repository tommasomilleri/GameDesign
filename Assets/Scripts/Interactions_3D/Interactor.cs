using UnityEngine;

public class Interactor : MonoBehaviour
{
    Camera cam;

    void Awake() { cam = GetComponent<Camera>(); }

    void Update()
    {
        int btn = Input.GetMouseButtonDown(0) ? 0 :
                  Input.GetMouseButtonDown(1) ? 1 : -1;
        if (btn < 0) return;

        if (PauseMenuManager.Instance != null &&
            PauseMenuManager.Instance.isPaused) return;

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(r, out RaycastHit hit, 100f))
            hit.collider.GetComponentInParent<Clickable3D>()?.Click(btn);
    }
}