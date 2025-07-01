using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class PlayerHealth : MonoBehaviour
{
    [Header("Valori base")]
    public int maxHealth = 100;
    public int currentHealth;
    public int baseDefense = 0;
    public int baseMoney = 0;

    [Header("UI")]
    public TextMeshProUGUI myLife;
    public Slider healthBar;
    public TextMeshProUGUI myDef;
    public TextMeshProUGUI myMoney;
    public TextMeshProUGUI messageText;

    [Header("Game Over")]
    public GameObject gameOverScreen;
    public float delayBeforeGameOverScreen = 0.5f;
    public MonoBehaviour[] inputScriptsToDisable;

    [Header("Riferimenti giocatori")]
    public PlayerHealth otherPlayer;
    public string playerDisplayName = "Giocatore";

    [Header("Flash rosso danno")]
    public Color flashColorPrefab = Color.red;
    public float prefabFlashDuration = 0.2f;

    public delegate void PlayerDeathDelegate();
    public event PlayerDeathDelegate OnPlayerDeath;

    private int bonusDefense = 0;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            myLife.text = $"Vita: {currentHealth}";
        }

        myMoney.text = "Oro: 0";
        myDef.text = $"Def: {baseDefense}";

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    public void TakeDamage(int incomingDamage)
    {
        int effectiveDefense = baseDefense + bonusDefense;
        int finalDamage = Mathf.Max(incomingDamage - effectiveDefense, 0);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            myLife.text = $"Vita: {currentHealth}";
        }

        Debug.Log($"{gameObject.name} ha subito {finalDamage} danni (difesa: {effectiveDefense})");

        if (bonusDefense > 0 && finalDamage > 0)
        {
            bonusDefense = 0;
            myDef.text = $"Def: {baseDefense}";
        }

        if (finalDamage > 0)
        {
            StartCoroutine(FlashPrefabRenderers());
        }

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    public void SetBonusDefense(int value)
    {
        bonusDefense = Mathf.Clamp(value, 1, 7);
        myDef.text = $"Def: {(baseDefense + bonusDefense)}";
        Debug.Log($"{gameObject.name} ha ricevuto una difesa bonus di {bonusDefense}");
    }

    void GameOver()
    {
        Debug.Log($"{gameObject.name} è morto!");
        OnPlayerDeath?.Invoke();

        if (messageText != null && otherPlayer != null && otherPlayer.currentHealth > 0)
        {
            messageText.text = $"{otherPlayer.playerDisplayName} ha vinto!";
            messageText.gameObject.SetActive(true);
        }

        StartCoroutine(HandleGameOver());
    }

    public void ForceGameOver()
    {
        foreach (MonoBehaviour script in inputScriptsToDisable)
        {
            script.enabled = false;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetBool("IsDead", true);
    }

    IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(delayBeforeGameOverScreen);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        foreach (MonoBehaviour script in inputScriptsToDisable)
        {
            script.enabled = false;
        }
    }

    public bool HasActiveBonusDefense()
    {
        return bonusDefense > 0;
    }

    public bool TrySpendGold(int amount)
    {
        if (baseMoney >= amount)
        {
            baseMoney -= amount;
            UpdateGoldUI();
            return true;
        }
        else
        {
            if (messageText != null)
                StartCoroutine(ShowMessage("Non hai abbastanza oro!", 2f));
            return false;
        }
    }

    void UpdateGoldUI()
    {
        if (myMoney != null)
            myMoney.text = "Oro: " + baseMoney;
    }

    IEnumerator ShowMessage(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        messageText.gameObject.SetActive(false);
    }

    IEnumerator FlashPrefabRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>()
            .Where(r => r.gameObject.activeInHierarchy)
            .ToArray();

        Material[][] originalMats = renderers
            .Select(r => r.materials.ToArray())
            .ToArray();

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] flashingMats = originalMats[i]
                .Select(mat =>
                {
                    Material newMat = new Material(mat);
                    if (newMat.HasProperty("_Color"))
                        newMat.color = flashColorPrefab;
                    return newMat;
                }).ToArray();

            renderers[i].materials = flashingMats;
        }

        yield return new WaitForSeconds(prefabFlashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMats[i];
        }
    }
}
