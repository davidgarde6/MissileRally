using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class UICountdown : MonoBehaviour
{
    public static UICountdown Instance;
    public bool started;
    public Animator animator;
    public GameObject countDown3;
    public TextMeshProUGUI texto;

    public List<GameObject> luces = new List<GameObject> ();

    private Color verde = new Color(0.5647058823529412f, 0.9333333333333333f, 0.5647058823529412f);

    void Awake()
    {
        Instance = this;
    }

    public void OnEnable() {

        luces.Clear();

        GameObject circuitManager = GameObject.Find("CircuitManager");
        print(circuitManager == null);
        Transform activeCircuit = null;
        foreach (Transform circuitos in circuitManager.transform)
        {
            if (circuitos.gameObject.activeSelf)
            {
                activeCircuit = circuitos;
                break;
            }
        }
        print(activeCircuit == null);
        for (int i = 0; i < 3; i++)
        {
            luces.Add(activeCircuit.transform.Find("Semaphore/Cube/LEDS").GetChild(i).gameObject);
            print(luces.Count);
        }
    }

    public void TurnOnLight1()
    {
        luces[0].gameObject.GetComponent<MeshRenderer>().material.color = verde;
    }

    public void Countdown2()
    {
        texto.text = "2";
        animator.Play("2");
        luces[1].gameObject.GetComponent<MeshRenderer>().material.color = verde;
    }

    public void Countdown1()
    {
        texto.text = "1";
        animator.Play("1");
        luces[2].gameObject.GetComponent<MeshRenderer>().material.color = verde;
    }

    public void CountdownGo()
    {
        texto.text = "¡YA!";
        animator.Play("¡YA!");
        UnlockPlayers();
    }

    public void UnlockPlayers()
    {

        for (int i = 0; i < UIManager.Instance.netPlayersConnected.Value; i++)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.players[i].GetComponent<Player>().ActivateControlClientRpc();
        }
    }
}
