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

    [Header("Story Page Extras")]
    [Tooltip("Bottone 'Skip' dentro la StoryPage: visibile solo se la storia è già stata letta")]
    public GameObject storySkipButton;

    [Header("Levels")]
    public GameObject Lvl1;

    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
        {
            GameManager.instance.qualityBarContainer.SetActive(false);
        }
        Time.timeScale = 1.0f;

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

        // FIX: la storia si mostra SEMPRE. Chi l'ha già letta vede il bottone Skip.
        // (Prima l'auto-salto la nascondeva per sempre dopo la prima run.)
        if (StoryPage != null) StoryPage.SetActive(true);
        if (storySkipButton != null)
            storySkipButton.SetActive(PlayerPrefs.GetInt("storySeen", 0) == 1);
    }

    // NEXT BUTTON ON STORY PAGE (e anche il bottone Skip: stessa funzione)
    public void GoToPlayerSelection()
    {
        if (StoryPage != null) StoryPage.SetActive(false);
        if (PlayerSelectPage != null) PlayerSelectPage.SetActive(true);

        PlayerPrefs.SetInt("storySeen", 1);
        PlayerPrefs.Save();   // scrittura su disco esplicita
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

        if (Lvl1 == null)
        {
            Debug.LogWarning("Lvl1 is missing! Drag it into the MenuManager Inspector.");
            return;
        }

        // FIX PRINCIPALE: si passa dal flusso ufficiale del GameManager.
        // TransitionIntoFirstLevel fa: gameplayActive = true (sblocca i click
        // sulle mucche!), unlockedStation = 0, transizione cellulare + loading
        // screen, e spegne LUI il PlayerSelectPage al momento giusto.
        // NON spegnere PlayerSelectPage qui: sparirebbe prima della transizione.
        if (GameManager.instance != null)
        {
            GameManager.instance.TransitionIntoFirstLevel(PlayerSelectPage, Lvl1);
        }
        else
        {
            // Fallback d'emergenza senza GameManager
            if (PlayerSelectPage != null) PlayerSelectPage.SetActive(false);
            Lvl1.SetActive(true);
        }

        // Barra qualità e pausa si abilitano subito (la barra è overlay,
        // il loading la copre comunque)
        if (GameManager.instance != null && GameManager.instance.qualityBarContainer != null)
        {
            GameManager.instance.qualityBarContainer.SetActive(true);
        }
        if (PauseMenuManager.Instance != null)
        {
            PauseMenuManager.Instance.canPause = true;
        }
    }

    void OpenPlayer1Manual()
    {
        try
        {
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