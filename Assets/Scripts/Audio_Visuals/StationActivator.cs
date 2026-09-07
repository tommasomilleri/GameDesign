using UnityEngine;

public class StationActivator : MonoBehaviour
{
    public int stationIndex;

    void OnEnable()
    {
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.GoTo(stationIndex);
    }
}