
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

        // Se il gioco e' in pausa, disabilita i click
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) return;

        // Mai raycast nel mondo se il click e' su un elemento UI
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        // Mai raycast se il gameplay non e' iniziato (siamo nei menu)
        if (GameManager.instance == null || !GameManager.instance.gameplayActive)
            return;

        // Transizione a bolle in corso? Niente click sul mondo.
        // (PRIMA del raycast: a fine metodo non serviva a nulla!)
        if (SimpleCellularTransition.Instance != null &&
            SimpleCellularTransition.Instance.IsBusy) return;

        // Cambio livello in corso?
        if (GameManager.instance.IsChangingLevel) return;

        Ray r = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity))
        {
            hit.collider.GetComponentInParent<Clickable3D>()?.Click(btn);
        }
    }
}

