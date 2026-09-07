using UnityEngine;
public class DebugProbe : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            string gm = (GameManager.instance != null) ? "OK" : "NULL";
            string qb = (GameManager.instance != null && GameManager.instance.globalQualityBar != null) ? "OK" : "NULL";
            string gf = (GameFeel.Instance != null) ? "OK" : "NULL";
            string es = (UnityEngine.EventSystems.EventSystem.current != null) ? "OK" : "NULL";
            Debug.Log("[PROBE] timeScale=" + Time.timeScale
                + " | GameManager=" + gm
                + " | qualityBar=" + qb
                + " | GameFeel=" + gf
                + " | EventSystem=" + es);
        }
    }
}