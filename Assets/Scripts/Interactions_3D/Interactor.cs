using UnityEngine;

public class Interactor : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        int btn = Input.GetMouseButtonDown(0) ? 0 : Input.GetMouseButtonDown(1) ? 1 : -1;
        if (btn < 0) return;

        // Se il gioco è in pausa, disabilita i click
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) return;

        Ray r = cam.ScreenPointToRay(Input.mousePosition);

        // CORREZIONE: Mathf.Infinity fa arrivare il raggio a qualsiasi distanza!
        if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
        {
            hit.collider.GetComponentInParent<Clickable3D>()?.Click(btn);
        }
    }
}