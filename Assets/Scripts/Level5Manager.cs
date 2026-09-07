using UnityEngine;

public class Level5Manager : MonoBehaviour
{
    [Header("Penalty Settings")]
    public int wrongShelfPenalty = 20;
    [SerializeField] private int correctSlot = 5;
    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip errorClip;

    public void CheckPosition(int slotNumber)
    {
        if (slotNumber == correctSlot)
        {
            Debug.Log("Correct shelf position!");

            LevelComplete();
        }
        else
        {
            Debug.Log("Wrong shelf position!");
            if (audioSource != null && errorClip != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(errorClip);
            }

            if (GameManager.instance != null)
                GameManager.instance.DecreaseGlobalQuality(wrongShelfPenalty);
        }
    }

    void LevelComplete()
    {
        Debug.Log("LEVEL 5 COMPLETE!");
        if (GameManager.instance != null)
            GameManager.instance.TriggerEnding();
        else
            Debug.LogWarning("GameManager missing: cannot trigger ending!");
    }
}