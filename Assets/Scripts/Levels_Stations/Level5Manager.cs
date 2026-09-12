
using UnityEngine;

public class Level5Manager : MonoBehaviour
{
    [Header("Penalty Settings")]
    public int wrongShelfPenalty = 20;

    [Tooltip("Slot corretto: 3 = terza mensola dal basso (come da manuale!)")]
    [SerializeField] private int correctSlot = 3;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip errorClip;

    private bool levelCompleted = false;

    public void CheckPosition(int slotNumber)
    {
        if (levelCompleted) return;

        // Il formaggio che tocca uno slot al load della scena NON
        // deve costare qualita': si valuta solo a partita in corso.
        if (GameManager.instance == null ||
            !GameManager.instance.gameplayActive) return;



        if (slotNumber == correctSlot)
        {
            Debug.Log("Correct shelf position!");
            levelCompleted = true;

            // FIX: il successClip prima non veniva MAI suonato
            if (audioSource != null && successClip != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(successClip);
            }

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

