
using UnityEngine;
public class Interactor : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            Debug.LogError("[Interactor] Nessuna Camera su '" + name +
                           "'. Mettilo sulla Main Camera!", this);
    }
    void Update()
    {
        int btn = Input.GetMouseButtonDown(0) ? 0 : Input.GetMouseButtonDown(1) ? 1 : -1;
        if (btn < 0) return;

        if (cam == null || !cam.isActiveAndEnabled) return;

        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (GameManager.instance == null || !GameManager.instance.gameplayActive)
            return;

        if (SimpleCellularTransition.Instance != null &&
            SimpleCellularTransition.Instance.IsBusy) return;

        if (GameManager.instance.IsChangingLevel) return;

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
        {
            hit.collider.GetComponentInParent<Clickable3D>()?.Click(btn);
        }
    }
}

