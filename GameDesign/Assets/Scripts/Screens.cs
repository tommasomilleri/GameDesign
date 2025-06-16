using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Screens : MonoBehaviour
{
    public GameObject game_over_screen, pause_menu_screen, settings_screen, beginning_screen;
    public GameObject player1, player2;

    void Awake()
    {
        Time.timeScale = 0f;
        game_over_screen.SetActive(false);
        pause_menu_screen.SetActive(false);
        settings_screen.SetActive(false);
        beginning_screen.SetActive(true);
        BeginningScreenOperation();
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

    public void BeginningScreenOperation()
    {
        player1.GetComponent<PlayerHealth>().maxHealth = Stats_Fante.START_LIFE;
        player2.GetComponent<PlayerHealth>().maxHealth = Stats_Cavallo.START_LIFE;
        beginning_screen.SetActive(false);
        Time.timeScale = 1f;
    }

}