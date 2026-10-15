
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIAudioUnpausable : MonoBehaviour
{
    void Awake()
    {
        GetComponent<AudioSource>().ignoreListenerPause = true;
    }
}
