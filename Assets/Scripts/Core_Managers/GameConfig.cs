using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ChitChat/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Quality")]
    public int maxQuality = 100;
    public int standardPenalty = 20;
    public int lvl2OutOfZonePenalty = 5;

    [Header("Ending thresholds")]
    public int highEndingMin = 80;
    public int mediumEndingMin = 40;
}