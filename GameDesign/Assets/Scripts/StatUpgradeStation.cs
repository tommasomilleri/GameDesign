using UnityEngine;
using TMPro;
using System.Collections;

public class StatUpgradeStation : MonoBehaviour
{
    public enum StatType { Health, Attack, Defense, Speed }

    [Header("Impostazioni Upgrade")]
    public StatType statType;
    public int goldCost = 20;
    public float cooldownTime = 10f;
    public int upgradeAmount = 10;
    public float buffDuration = 10f;

    [Header("Testo 3D sopra l’oggetto")]
    public TextMeshPro cooldownText3D;

    [Header("Tasti personalizzati")]
    public KeyCode player1Key = KeyCode.E;
    public KeyCode player2Key = KeyCode.RightShift;

    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    private void Start()
    {
        if (cooldownText3D != null)
        {
            cooldownText3D.text = $"Costo Buff: {goldCost}";
        }
    }

    private void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                if (cooldownText3D != null)
                    cooldownText3D.text = $"Costo: {goldCost}";
            }
            else
            {
                if (cooldownText3D != null)
                    cooldownText3D.text = $"Ricarica: {Mathf.CeilToInt(cooldownTimer)}s";
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isOnCooldown) return;

        bool isPlayer1 = other.CompareTag("Player");
        bool isPlayer2 = other.CompareTag("Player2");

        if (!isPlayer1 && !isPlayer2) return;

        bool keyPressed = (isPlayer1 && Input.GetKeyDown(player1Key)) || (isPlayer2 && Input.GetKeyDown(player2Key));
        if (!keyPressed) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        PlayerAttack pa = other.GetComponent<PlayerAttack>();
        PlayerMovement pm = other.GetComponent<PlayerMovement>();

        if (ph != null && ph.TrySpendGold(goldCost))
        {
            StartCoroutine(ApplyTemporaryBuff(ph, pa, pm));
            cooldownTimer = cooldownTime;
            isOnCooldown = true;
            if (cooldownText3D != null)
                cooldownText3D.text = $"Ricarica: {Mathf.CeilToInt(cooldownTimer)}s";
        }
    }

    IEnumerator ApplyTemporaryBuff(PlayerHealth ph, PlayerAttack pa, PlayerMovement pm)
    {
        switch (statType)
        {
            case StatType.Health:
                int originalMaxHealth = ph.maxHealth;
                ph.maxHealth += upgradeAmount;
                ph.currentHealth = ph.maxHealth;
                ph.healthBar.maxValue = ph.maxHealth;
                ph.healthBar.value = ph.currentHealth;
                ph.myLife.text = $"Life: {ph.currentHealth}";
                yield return new WaitForSeconds(buffDuration);
                ph.maxHealth = originalMaxHealth;
                ph.currentHealth = Mathf.Min(ph.currentHealth, ph.maxHealth);
                ph.healthBar.maxValue = ph.maxHealth;
                ph.healthBar.value = ph.currentHealth;
                ph.myLife.text = $"Life: {ph.currentHealth}";
                break;

            case StatType.Attack:
                if (pa != null)
                {
                    int originalAttack = pa.baseDamage;
                    pa.baseDamage += upgradeAmount;
                    pa.myAtk.text = $"Atk: {pa.baseDamage}";
                    yield return new WaitForSeconds(buffDuration);
                    pa.baseDamage = originalAttack;
                    pa.myAtk.text = $"Atk: {pa.baseDamage}";
                }
                break;

            case StatType.Defense:
                int originalDefense = ph.baseDefense;
                ph.baseDefense += upgradeAmount;
                ph.myDef.text = $"Def: {ph.baseDefense}";
                yield return new WaitForSeconds(buffDuration);
                ph.baseDefense = originalDefense;
                ph.myDef.text = $"Def: {ph.baseDefense}";
                break;

            case StatType.Speed:
                if (pm != null)
                {
                    float originalSpeed = pm.speed;
                    pm.speed += upgradeAmount;
                    yield return new WaitForSeconds(buffDuration);
                    pm.speed = originalSpeed;
                }
                break;
        }
    }
}

