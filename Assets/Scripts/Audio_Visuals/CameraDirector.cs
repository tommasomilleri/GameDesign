using UnityEngine;
using Unity.Cinemachine; // <- IL NUOVO NAMESPACE DI CINEMACHINE 3

public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance;

    // Nelle nuove versioni non è più VirtualCamera, ma CinemachineCamera
    public CinemachineCamera[] cams;   // VC_ST1..5 in ordine
    public int Current { get; private set; }

    void Awake() { Instance = this; }

    public void GoTo(int i)
    {
        if (i < 0 || i >= cams.Length) return;
        if (GameManager.instance != null &&
            i > GameManager.instance.unlockedStation) return;

        Current = i;
        for (int k = 0; k < cams.Length; k++)
            cams[k].Priority = (k == i) ? 20 : 10;
    }
    public void Next() { GoTo(Current + 1); }
    public void Prev() { GoTo(Current - 1); }
}