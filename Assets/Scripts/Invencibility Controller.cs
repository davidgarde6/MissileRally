using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvencibilityController : MonoBehaviour
{
    public Animator animator;
    public Player player;

    public void StartProtection()
    {
        animator.Play("Protect");
        gameObject.SetActive(true);
        player.netVulnerable.Value = false;
    }

    public void EndProtection()
    {
        gameObject.SetActive(false);
        player.netVulnerable.Value = true;
    }
}
