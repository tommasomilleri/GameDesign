using UnityEngine;
using TMPro; // se usi TextMeshPro

public class MatchTimer : MonoBehaviour
{
    public float matchDuration = 90f; // durata in secondi (es. 1m30s)
    private float currentTime;

    public TextMeshProUGUI timerText;
    public GameObject gameOverScreen;

    public PlayerHealth player1Health;
    public PlayerHealth player2Health;

    private bool isGameOver = false;

    void Start()
    {
        currentTime = matchDuration;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (isGameOver) return;

        currentTime -= Time.deltaTime;
        currentTime = Mathf.Max(currentTime, 0f);
        UpdateTimerDisplay();

        if (currentTime <= 0f)
        {
            EndMatch();
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = $"Tempo: {minutes:00}:{seconds:00}";
    }

    void EndMatch()
    {
        isGameOver = true;

        // Disattiva input di entrambi
        player1Health.ForceGameOver();
        player2Health.ForceGameOver();

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        Debug.Log("Tempo scaduto!");

        if (player1Health.currentHealth > player2Health.currentHealth)
        Debug.Log("Player 1 ha vinto!");
        else if (player2Health.currentHealth > player1Health.currentHealth)
        Debug.Log("Player 2 ha vinto!");
        else
        Debug.Log("Pareggio!");
    }
}