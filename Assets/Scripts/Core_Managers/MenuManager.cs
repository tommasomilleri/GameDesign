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

        public void StartGame()
    {
        if (StartPage != null) StartPage.SetActive(false);

                        if (StoryPage != null) StoryPage.SetActive(true);
        if (storySkipButton != null)
            storySkipButton.SetActive(PlayerPrefs.GetInt("storySeen", 0) == 1);
    }

        public void GoToPlayerSelection()
    {
        if (StoryPage != null) StoryPage.SetActive(false);
        if (PlayerSelectPage != null) PlayerSelectPage.SetActive(true);

        PlayerPrefs.SetInt("storySeen", 1);
        PlayerPrefs.Save();       }

        public void OpenTutorial()
    {
        if (StartPage != null) StartPage.SetActive(false);
        if (TutorialPage != null) TutorialPage.SetActive(true);
    }

        public void BackToStart()
    {
        if (TutorialPage != null) TutorialPage.SetActive(false);
        if (StartPage != null) StartPage.SetActive(true);
    }

        public void SelectPlayer1()
    {
        Debug.Log("Player 1 (Reader) selected. Opening HTML Manual...");
        OpenPlayer1Manual();
            }

        public void SelectPlayer2()
    {
        Debug.Log("Player 2 (Chef) selected. Starting Level 1...");

        if (Lvl1 == null)
        {
            Debug.LogWarning("Lvl1 is missing! Drag it into the MenuManager Inspector.");
            return;
        }

                                                if (GameManager.instance != null)
        {
            GameManager.instance.TransitionIntoFirstLevel(PlayerSelectPage, Lvl1);
        }
        else
        {
                        if (PlayerSelectPage != null) PlayerSelectPage.SetActive(false);
            Lvl1.SetActive(true);
        }

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

        public void ExitGame()
    {
        Debug.Log("Exiting game...");
        Application.Quit();
    }
}