using UnityEngine;

public class AttackEventRelay : MonoBehaviour
{
    public PlayerAttack playerAttackScript;

    public void OnAttackAnimationEnd()
    {
        if (playerAttackScript != null)
        {
            playerAttackScript.OnAttackAnimationEnd();
        }
    }

    public void AttemptAttack()
    {
        if (playerAttackScript != null)
        {
            playerAttackScript.AttemptAttack();
        }
    }
}

