
using UnityEngine;

public class Shelfslot : MonoBehaviour
{
    [Header("Manager")]
    public Level5Manager level5Manager;

    [Tooltip("Numerati DAL BASSO: 1, 2, 3 - il manuale dice 'terza dal basso'")]
    public int slotNumber;

    [Tooltip("Secondi prima che lo stesso slot possa ri-penalizzare")]
    public float retriggerCooldown = 2f;

    float lastTriggerTime = -99f;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CheeseTag>() == null) return;
        if (level5Manager == null) return;

        // Anti-spam: il formaggio che rotola/rimbalza sul trigger
        // non deve mitragliare penalita' a ogni contatto
        if (Time.time - lastTriggerTime < retriggerCooldown) return;
        lastTriggerTime = Time.time;

        level5Manager.CheckPosition(slotNumber);
    }
}

