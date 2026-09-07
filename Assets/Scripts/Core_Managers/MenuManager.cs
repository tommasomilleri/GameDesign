using System;
using System.IO;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject StartPage;
    public GameObject StoryPage;
    public GameObject TutorialPage;
    public GameObject PlayerSelectPage;

    [Header("Levels")]
    public GameObject Lvl1;

    void Start()
    {
        // 1. Nasconde il Quality Meter appena si apre il menu usando il NUOVO GameManager
        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
        {
            GameManager.instance.qualityBarContainer.SetActive(false);
        }
        Time.timeScale = 1.0f;

        // 2. FORZATURA DI SICUREZZA: Assicura che al riavvio della scena ci sia solo lo StartMenu
        if (StartPage != null) StartPage.SetActive(true);
        if (StoryPage != null) StoryPage.SetActive(false);
        if (TutorialPage != null) TutorialPage.SetActive(false);
        if (PlayerSelectPage != null) PlayerSelectPage.SetActive(false);
        if (Lvl1 != null) Lvl1.SetActive(false);
    }

    // START BUTTON
    public void StartGame()
    {
        if (StartPage != null) StartPage.SetActive(false);

        // AGGIORNAMENTO H5: Salto della storia per chi ha già giocato
        if (PlayerPrefs.GetInt("storySeen", 0) == 1)
        {
            if (PlayerSelectPage != null) PlayerSelectPage.SetActive(true);
        }
        else
        {
            if (StoryPage != null) StoryPage.SetActive(true);
        }
    }

    // NEXT BUTTON ON STORY PAGE
    public void GoToPlayerSelection()
    {
        if (StoryPage != null) StoryPage.SetActive(false);
        if (PlayerSelectPage != null) PlayerSelectPage.SetActive(true);

        // Salva in memoria che il giocatore ha letto la storia
        PlayerPrefs.SetInt("storySeen", 1);
    }

    // TUTORIAL BUTTON
    public void OpenTutorial()
    {
        if (StartPage != null) StartPage.SetActive(false);
        if (TutorialPage != null) TutorialPage.SetActive(true);
    }

    // BACK BUTTON ON TUTORIAL
    public void BackToStart()
    {
        if (TutorialPage != null) TutorialPage.SetActive(false);
        if (StartPage != null) StartPage.SetActive(true);
    }

    // PLAYER 1 (The Reader)
    public void SelectPlayer1()
    {
        Debug.Log("Player 1 (Reader) selected. Opening HTML Manual...");
        OpenPlayer1Manual();
        // IMPORTANT: We deliberately DO NOT hide the PlayerSelectPage here!
    }

    // PLAYER 2 (The Chef)
    public void SelectPlayer2()
    {
        Debug.Log("Player 2 (Chef) selected. Starting Level 1...");

        if (PlayerSelectPage != null)
        {
            PlayerSelectPage.SetActive(false);
        }

        if (Lvl1 != null)
        {
            Lvl1.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Lvl1 is missing! Drag it into the MenuManager Inspector.");
        }

        // Riaccende la barra della qualità usando il NUOVO GameManager
        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
        {
            GameManager.instance.qualityBarContainer.SetActive(true);
        }
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.canPause = true;
        }

    }

    // AGGIORNAMENTO I1: Sostituito PDF con il sito web locale
    void OpenPlayer1Manual()
    {
        try
        {
            // Cerca il file index.html dentro la cartella "manual" in StreamingAssets
            string manualPath = Path.Combine(Application.streamingAssetsPath, "manual/index.html");
            string manualURL = new Uri(manualPath).AbsoluteUri;
            Application.OpenURL(manualURL);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to open the HTML manual! Error: " + e.Message);
        }
    }

    // EXIT BUTTON
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        Application.Quit();
    }
}