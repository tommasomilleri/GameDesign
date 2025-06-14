using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Screens : MonoBehaviour
{
    public GameObject game_over_screen, pause_menu_screen, settings_screen/*, beginning_screen*/;
    public GameObject player1, player2;

    void Awake()
    {
        Time.timeScale = 1f;
        game_over_screen.SetActive(false);
        pause_menu_screen.SetActive(false);
        settings_screen.SetActive(false);
    }

    void Update()
    {
        if (!player1.activeSelf || !player2.activeSelf)
        {
            Time.timeScale = 0f;
            game_over_screen.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pause_menu_screen.activeSelf)
            {
                ResumePressed();
            }
            else
            {
                Time.timeScale = 0f;
                pause_menu_screen.SetActive(true);
            }
        }
    }

    public void ResumePressed()
    {
        Time.timeScale = 1f;
        pause_menu_screen.SetActive(false);
    }

    public void BackPressed()
    {
        pause_menu_screen.SetActive(true);
        settings_screen.SetActive(false);
    }

    public void SettingsPressed()
    {
        pause_menu_screen.SetActive(false);
        settings_screen.SetActive(true);
    }

    public void GoToMainMenuPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void NewGamePressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Gioco");
    }

    public void ExitGamePressed()
    {
        Application.Quit();
    }

}
