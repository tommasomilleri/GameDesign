using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading;

public class Screens : MonoBehaviour
{
    public GameObject game_over_screen, pause_menu_screen, settings_screen, beginning_screen;
    public GameObject player1, player2;
    public TextMeshProUGUI textp1, textp2;
    public GameObject Fante;
    public GameObject Cavallo;
    public GameObject Re;

    void Awake()
    {
        Time.timeScale = 0f;
        game_over_screen.SetActive(false);
        pause_menu_screen.SetActive(false);
        settings_screen.SetActive(false);
        beginning_screen.SetActive(true);
        if (SceneManager.GetActiveScene().name == "Gioco") BeginningScreenOperation();
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
        System.Random rnd = new System.Random();
        int p1, p2;
        p1 = rnd.Next(1, 4);
        p2 = rnd.Next(1, 4);
        switch (p1)
        {
            case 1:
                

                player1.GetComponent<PlayerHealth>().maxHealth = Stats_Fante.START_LIFE;
                player1.GetComponent<PlayerAttack>().baseDamage = Stats_Fante.START_ATTACK;
                player1.GetComponent<PlayerHealth>().baseDefense = Stats_Fante.START_DEFENSE;
                player1.GetComponent<PlayerMovement>().speed = Stats_Fante.START_MOVEMENT_SPEED;
                player1.GetComponent<PlayerHealth>().baseMoney = Stats_Fante.START_MONEY;
                player1.GetComponent<PlayerAttack>().attackRange = Stats_Fante.START_RANGE;
                //player1.GetComponent<PlayerAttack>().????? = Stats_Fante.START_ATTACK_SPEED;
                //player1.GetComponent<PlayerAttack>().????? = Stats_Fante.START_RELOAD_TIME;
                textp1.text = "Fante";

                //modello swap

                player1.GetComponent<ModelSwap>().ActivateModel("Fante");
                break;
            case 2:
                player1.GetComponent<PlayerHealth>().maxHealth = Stats_Cavallo.START_LIFE;
                player1.GetComponent<PlayerAttack>().baseDamage = Stats_Cavallo.START_ATTACK;
                player1.GetComponent<PlayerHealth>().baseDefense = Stats_Cavallo.START_DEFENSE;
                player1.GetComponent<PlayerMovement>().speed = Stats_Cavallo.START_MOVEMENT_SPEED;
                player1.GetComponent<PlayerHealth>().baseMoney = Stats_Cavallo.START_MONEY;
                player1.GetComponent<PlayerAttack>().attackRange = Stats_Cavallo.START_RANGE;
                //player1.GetComponent<PlayerAttack>().????? = Stats_Cavallo.START_ATTACK_SPEED;
                //player1.GetComponent<PlayerAttack>().????? = Stats_Cavallo.START_RELOAD_TIME;
                textp1.text = "Cavallo";

                //modello swap

                player1.GetComponent<ModelSwap>().ActivateModel("Cavallo");
                break;
            case 3:
                player1.GetComponent<PlayerHealth>().maxHealth = Stats_Re.START_LIFE;
                player1.GetComponent<PlayerAttack>().baseDamage = Stats_Re.START_ATTACK;
                player1.GetComponent<PlayerHealth>().baseDefense = Stats_Re.START_DEFENSE;
                player1.GetComponent<PlayerMovement>().speed = Stats_Re.START_MOVEMENT_SPEED;
                player1.GetComponent<PlayerHealth>().baseMoney = Stats_Re.START_MONEY;
                player1.GetComponent<PlayerAttack>().attackRange = Stats_Re.START_RANGE;
                //player1.GetComponent<PlayerAttack>().????? = Stats_Re.START_ATTACK_SPEED;
                //player1.GetComponent<PlayerAttack>().????? = Stats_Re.START_RELOAD_TIME;
                textp1.text = "Re";

                //modello swap

                player1.GetComponent<ModelSwap>().ActivateModel("Re");
                break;
            default:
                Application.Quit();
                break;
        }
        switch (p2)
        {
            case 1:
                

                player2.GetComponent<PlayerHealth>().maxHealth = Stats_Fante.START_LIFE;
                player2.GetComponent<PlayerAttack>().baseDamage = Stats_Fante.START_ATTACK;
                player2.GetComponent<PlayerHealth>().baseDefense = Stats_Fante.START_DEFENSE;
                player2.GetComponent<PlayerMovement>().speed = Stats_Fante.START_MOVEMENT_SPEED;
                player2.GetComponent<PlayerHealth>().baseMoney = Stats_Fante.START_MONEY;
                player2.GetComponent<PlayerAttack>().attackRange = Stats_Fante.START_RANGE;
                //player2.GetComponent<PlayerAttack>().????? = Stats_Fante.START_ATTACK_SPEED;
                //player2.GetComponent<PlayerAttack>().????? = Stats_Fante.START_RELOAD_TIME;
                textp2.text = "Fante";

                //modello swap

                player2.GetComponent<ModelSwap>().ActivateModel("Fante");
                break;
            case 2:
                player2.GetComponent<PlayerHealth>().maxHealth = Stats_Cavallo.START_LIFE;
                player2.GetComponent<PlayerAttack>().baseDamage = Stats_Cavallo.START_ATTACK;
                player2.GetComponent<PlayerHealth>().baseDefense = Stats_Cavallo.START_DEFENSE;
                player2.GetComponent<PlayerMovement>().speed = Stats_Cavallo.START_MOVEMENT_SPEED;
                player2.GetComponent<PlayerHealth>().baseMoney = Stats_Cavallo.START_MONEY;
                player2.GetComponent<PlayerAttack>().attackRange = Stats_Cavallo.START_RANGE;
                //player2.GetComponent<PlayerAttack>().????? = Stats_Cavallo.START_ATTACK_SPEED;
                //player2.GetComponent<PlayerAttack>().????? = Stats_Cavallo.START_RELOAD_TIME;
                textp2.text = "Cavallo";

                //modello swap

                player2.GetComponent<ModelSwap>().ActivateModel("Cavallo");
                break;
            case 3:
                player2.GetComponent<PlayerHealth>().maxHealth = Stats_Re.START_LIFE;
                player2.GetComponent<PlayerAttack>().baseDamage = Stats_Re.START_ATTACK;
                player2.GetComponent<PlayerHealth>().baseDefense = Stats_Re.START_DEFENSE;
                player2.GetComponent<PlayerMovement>().speed = Stats_Re.START_MOVEMENT_SPEED;
                player2.GetComponent<PlayerHealth>().baseMoney = Stats_Re.START_MONEY;
                player2.GetComponent<PlayerAttack>().attackRange = Stats_Re.START_RANGE;
                //player2.GetComponent<PlayerAttack>().????? = Stats_Re.START_ATTACK_SPEED;
                //player2.GetComponent<PlayerAttack>().????? = Stats_Re.START_RELOAD_TIME;
                textp2.text = "Re";

                //modello swap

                player2.GetComponent<ModelSwap>().ActivateModel("Re");
                break;
            default:
                Application.Quit();
                break;
        }
        beginning_screen.SetActive(false);
        Time.timeScale = 1f;
    }
}