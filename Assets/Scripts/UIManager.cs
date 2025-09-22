using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class UIManager : NetworkBehaviour
{
    public static UIManager Instance;
    const int maxConnections = 50;
    string joinCode = "Enter room code.";
    public string name = "";
    public Color colorFinal = new Color(0.990566f, 0.3027768f, 0.3027768f);

    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private TextMeshProUGUI textoAviso2;
    [SerializeField] private TMP_InputField textoEntrada;
    [SerializeField] private TMP_InputField entradaCodigo;

    [Header("Interfaces")]
    [SerializeField] private GameObject interfazInicio;
    [SerializeField] private GameObject interfazColor;
    [SerializeField] private GameObject interfazLobby;
    [SerializeField] public GameObject playerHUD;
    [SerializeField] private GameObject LobbyHUD;
    [SerializeField] public GameObject botonHost;
    [SerializeField] private GameObject interfazHost;
    [SerializeField] public GameObject interfazInfo;
    [SerializeField] public GameObject pantallaCarga;
    [SerializeField] public GameObject[] bannersResultados;
    

    [Header("Objetos")]
    [SerializeField] private GameObject[] circuitos;
    [SerializeField] private GameObject cocheUI;

    [Header("Información partida")]
    private int circuitIndex;
    public NetworkVariable<FixedString128Bytes> netCircuito;
    public NetworkVariable<int> netPlayersConnected;
    public int playersConnected;
    private string circuito;
    public TextMeshProUGUI nombreCircuito;
    public TextMeshProUGUI numConectados;
    private bool playersSpawned;
    private float cuentaAtrasSemaforos = 5f;
    public TextMeshProUGUI vuelta1;
    public TextMeshProUGUI vuelta2;
    public TextMeshProUGUI vuelta3;
    public TextMeshProUGUI indicadorVueltas;

    //void OnGUI()
    //{
    //    GUILayout.BeginArea(new Rect(10, 10, 300, 300));
    //    if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
    //    {
    //        //StartButtons();
    //    }
    //    else
    //    {
    //        //StatusLabels();
    //    }

    //    GUILayout.EndArea();
    //}

    //void StartButtons()
    //{
    //    if (GUILayout.Button("Host")) StartHost();
    //    if (GUILayout.Button("Client")) StartClient();
    //    joinCode = GUILayout.TextField(joinCode);
    //}

    void OnEnable()
    {
        netCircuito.OnValueChanged += OnRaceChange;
        netPlayersConnected.OnValueChanged += OnPlayersChange;
    }
    
    void OnDisable()
    {
        netCircuito.OnValueChanged -= OnRaceChange;
        netPlayersConnected.OnValueChanged -= OnPlayersChange;
    }
    void Awake()
    {
        Instance = this;
        netCircuito = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netPlayersConnected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public void OnRaceChange(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        if (!IsOwner) return;
        circuito = newValue.ToString();
        nombreCircuito.text = circuito;
        ChangeRaceNameClientRpc(circuito.ToString());
    }
    
    public void OnPlayersChange(int previousValue, int newValue)
    {
        if (!IsOwner) return;
        ChangePlayerCountClientRpc(newValue);
    }

    [ClientRpc]
    public void ChangeRaceNameClientRpc(string name)
    {
        circuito = name;
        nombreCircuito.text = circuito; 
        interfazInfo.SetActive(true);
        interfazHost.SetActive(false);
    }

    [ClientRpc]
    public void ChangePlayerCountClientRpc(int value)
    {
        playersConnected = value;
        numConectados.text = playersConnected.ToString() + "/6";
        if(playersConnected > 1)
        {
            numConectados.color = Color.white;
        }
        else
        {
            numConectados.color = Color.red;
        }
    }

    public async void StartHost()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        LobbyHUD.SetActive(true);
        pantallaCarga.SetActive(true);
        NetworkManager.Singleton.StartHost();
        HideUI();
    }

    public async void StartClient()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
        LobbyHUD.SetActive(true);
        pantallaCarga.SetActive(true);
        NetworkManager.Singleton.StartClient();
        HideUI();
    }

    public void HideUI()
    {
        interfazLobby.SetActive(false);

        GameObject.Find("LobbyUI").transform.Find("Info/Jugador/Nombre").GetComponent<TextMeshProUGUI>().text = name;
        GameObject.Find("LobbyUI").transform.Find("Info/Código/Contraseña").GetComponent<TextMeshProUGUI>().text = "CÓDIGO:" + joinCode;
    }

    //void StatusLabels()
    //{
    //    var mode = NetworkManager.Singleton.IsHost ?
    //        "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

    //    GUILayout.Label("Transport: " +
    //        NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
    //    GUILayout.Label("Mode: " + mode);

    //    GUILayout.Label("Room: " + joinCode);
    //}

    public void CambioDeNombre()
    {
        textoAviso2.gameObject.SetActive(false);
        textoAviso.gameObject.SetActive(false);
    }

    public void CambioDeCodigo()
    {
        joinCode = entradaCodigo.text;
    }

    public void RegistrarNombre()
    {
        name = textoEntrada.text;
        textoAviso2.gameObject.SetActive(true);
    }

    public void CambiarInterfaz(int index)
    {
        switch(index)
        {
            case 0:
                interfazInicio.SetActive(true);
                interfazColor.SetActive(false);
                break;

            case 1:
                interfazInicio.SetActive(false);
                interfazColor.SetActive(true);
                textoAviso.gameObject.SetActive(false);
                textoAviso2.gameObject.SetActive(false);
                break;

            case 2:

                if(name == "")
                {
                    textoAviso.gameObject.SetActive(true);
                    textoAviso2.gameObject.SetActive(false);
                }
                else
                {
                    interfazColor.SetActive(false);
                    interfazLobby.SetActive(true);
                    name = textoEntrada.text;
                }
                break;
            
            case 3:
                interfazColor.SetActive(true);
                interfazLobby.SetActive(false);
                textoAviso.gameObject.SetActive(false);
                textoAviso2.gameObject.SetActive(false);
                break;
        }
    }

    public void CambiarColor(int index)
    {
        Color colorCoche = Color.white;

        switch (index)
        {
            case 0:
                colorCoche = new Color(0.990566f, 0.3027768f, 0.3027768f);
                break;
            case 1:
                colorCoche = new Color(1f, 0.6113151f, 0.2679245f);
                break;
            case 2:
                colorCoche = new Color(0.9433962f, 0.907187f, 0.3239587f);
                break;
            case 3:
                colorCoche = new Color(0.5673946f, 1f, 0.3528302f);
                break;
            case 4:
                colorCoche = new Color(0.4572445f, 0.8448635f, 0.9811321f);
                break;
            case 5:
                colorCoche = new Color(0.5193139f, 0.2732645f, 0.9528302f);
                break;
        }
        cocheUI.transform.Find("body").gameObject.GetComponent<Renderer>().material.color = colorCoche;
        colorFinal = colorCoche;
    }

    public void ElegirCircuito(int index)
    {
        circuitIndex = index;
        SetIndexClientRpc(index);
        switch (circuitIndex)
        {
            case 0:
                netCircuito.Value = "OWLPLAINS";
                break;
            case 1:
                netCircuito.Value = "RAINY";
                break;
            case 2:
                netCircuito.Value = "OASIS";
                break;
            case 3:
                netCircuito.Value = "NASCAR";
                break;
        }
    }

    [ClientRpc]
    public void SetIndexClientRpc(int index)
    {
        circuitIndex = index;
    }

    public void InterfazSeleccion(bool activo)
    {
        interfazHost.SetActive(activo);
        interfazInfo.SetActive(!activo);
    }

    //public void ActivarCircuito()
    //{
    //    foreach (GameObject circuito in circuitos)
    //    {
    //        circuito.SetActive(false);
    //    }
    //    Debug.Log(circuitIndex);
    //    circuitos[circuitIndex].SetActive(true);
    //    interfazLobby.SetActive(false);
    //}

    [ClientRpc]
    public void ActivarCircuitoClientRpc()
    {
        foreach (GameObject circuito in circuitos)
        {
            circuito.SetActive(false);
        }
        circuitos[circuitIndex].SetActive(true);
        interfazLobby.SetActive(false);
        playerHUD.SetActive(true);
        LobbyHUD.SetActive(false);
    }
    
    [ClientRpc]
    public void SpawnPlayersClientRpc(bool activarCamaras)
    {
        for (int i = 0; i < netPlayersConnected.Value; i++)
        {
            if (!IsServer) continue;
            Vector3 pos = new Vector3(GameManager.Instance.runningPos[i].position.x, GameManager.Instance.runningPos[i].position.y + 2f, GameManager.Instance.runningPos[i].position.z - 1f);
            GameManager.Instance.players[i].gameObject.transform.position = pos;
            if (activarCamaras)
            {
                GameManager.Instance.players[i].GetComponent<Player>().ActivateCameraClientRpc();
            }
        }

        if (activarCamaras)
        {
            playersSpawned = true;
        }
    }

    public void ComenzarPartida()
    {
        if(playersConnected > 1)
        {
            ActivarCircuitoClientRpc();
            GameManager.Instance.GetSpawnPoints();
            SpawnPlayersClientRpc(true);
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        if (playersSpawned)
        {
            cuentaAtrasSemaforos -= Time.fixedDeltaTime;
        }

        if(cuentaAtrasSemaforos < 0)
        {
            UICountdown.Instance.started = true;
        }
    }
}
