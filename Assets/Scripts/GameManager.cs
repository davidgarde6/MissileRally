using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public int numPlayers = 50;
    const int MAX_PLAYERS = 50;
    public bool raceStarted;
    GameObject camera;
    public GameObject player;

    public string circuit;

    public Vector3 posInicial;
    public List<Transform> runningPos = new List<Transform>();
    public List<Vector3> playersPos = new List<Vector3>();
    public List<GameObject> players = new List<GameObject>();
    public List<GameObject> ganadores = new List<GameObject>();
    private Queue<GameObject> playersPosAux = new Queue<GameObject>();
    private List<ulong> IDClientesDesconectados = new List<ulong>();
    private bool activeHost = true;

    NetworkManager _networkManager;
    GameObject _playerPrefab;
    public GameObject _misilPrefab;

    public RaceController currentRace;

    [Header("Interfaces error")]
    public GameObject intefazMaxJugadores;
    public GameObject intefazHostAbandona;
    public GameObject intefazLogIn;

    public static GameManager Instance { get; private set; }

    void Start()
    {
        _networkManager = NetworkManager.Singleton;
        _playerPrefab = _networkManager.NetworkConfig.Prefabs.Prefabs[0].Prefab;
        _misilPrefab = _networkManager.NetworkConfig.Prefabs.Prefabs[1].Prefab;
        _networkManager.OnServerStarted += OnServerStarted;
        _networkManager.OnServerStopped += OnServerStopped;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnServerStarted() 
    {
        print("Servidor listo");
    }
    
    private void OnServerStopped(bool wasHost) 
    {
        print("Servidor cerrado");
        activeHost = false;
        HostNotActiveClientRpc();
        _networkManager.Shutdown();
    }

    [ClientRpc]
    void HostNotActiveClientRpc()
    {
        activeHost = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateKillsServerRpc(int playerID)
    {
        players[playerID].GetComponent<Player>().netKills.Value += 1;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void UpdateDeathsServerRpc(int playerID)
    {
        Debug.Log("CLIENTE " + NetworkManager.Singleton.LocalClientId + " AUMENTA LAS MUERTES");
        Debug.Log("ANTES: " + players[playerID].GetComponent<Player>().netDeaths.Value);
        players[playerID].GetComponent<Player>().netDeaths.Value += 1;
        Debug.Log("DESPUÉS: " + players[playerID].GetComponent<Player>().netDeaths.Value);

        player.GetComponent<Player>().invencible.gameObject.SetActive(true);
        player.GetComponent<Player>().invencible.StartProtection();
    }

    [ClientRpc]
    public void SetPlayerClientRpc(ulong id, ulong objectId)
    {
        NetworkObject networkObject = _networkManager.SpawnManager.SpawnedObjects[objectId];
        if (_networkManager.LocalClientId == id)
        {
            player = networkObject.gameObject;
        }
    }

    private void OnClientConnected(ulong obj) 
    {
        if (!IsServer) return;
        UIManager.Instance.botonHost.SetActive(true);
        if (UIManager.Instance.netPlayersConnected.Value > 5)
        {
            intefazMaxJugadores.SetActive(true);
            IDClientesDesconectados.Add(obj);
            _networkManager.DisconnectClient(obj);
            return;
        }
        //Debug.Log("Cliente listo");

        if (runningPos.Count == 0)
        {
            GetSpawnPoints();
        }
        if (circuit == "Lobby")
        {
            var player = Instantiate(_playerPrefab, new Vector3(runningPos[UIManager.Instance.netPlayersConnected.Value].position.x, runningPos[UIManager.Instance.netPlayersConnected.Value].position.y + 1f, runningPos[UIManager.Instance.netPlayersConnected.Value].position.z), _playerPrefab.transform.rotation);
            player.GetComponent<Player>().netID.Value = UIManager.Instance.netPlayersConnected.Value;
            //player.GetComponent<Player>().netName.Value = UIManager.Instance.netPlayersConnected.Value;
            player.GetComponent<Player>().NetcodeID = obj;
            players.Add(player);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(obj);
            SetPlayerClientRpc(obj, player.GetComponent<NetworkObject>().NetworkObjectId);
            
            UIManager.Instance.netPlayersConnected.Value += 1;

            HideUIClientRpc();
        }
    }

    [ClientRpc]
    void HideUIClientRpc()
    {
        UIManager.Instance.pantallaCarga.SetActive(false);
        UIManager.Instance.interfazInfo.SetActive(true);
    }

    private void OnClientDisconnected(ulong obj)
    {
        if (!IDClientesDesconectados.Contains(obj) && IsServer)
        {
            UIManager.Instance.netPlayersConnected.Value -= 1;
            for(int i = 0; i < players.Count; i++)
            {
                if (players[i].GetComponent<Player>().NetcodeID == obj)
                {
                    players.Remove(players[i]);
                    CambiarIDs();
                }
            }

            GetSpawnPoints();
            UIManager.Instance.SpawnPlayersClientRpc(false);
        }
        else
        {
            // Remover el cliente rechazado de la lista ya que se ha desconectado
            IDClientesDesconectados.Remove(obj);
            return;
        }

        if (obj != _networkManager.LocalClientId || IsServer) return;
        if (!activeHost)
        {
            intefazHostAbandona.SetActive(true);
        }
        intefazLogIn.SetActive(true);
    }

    public void CambiarIDs()
    {
        for(int i = 0; i < players.Count; i++)
        {
            players[i].GetComponent<Player>().netID.Value = i;
        }
    }

    public void GetSpawnPoints()
    {
        runningPos.Clear();

        GameObject circuitManager = GameObject.Find("CircuitManager");

        Transform activeCircuit = null;
        foreach (Transform circuitos in circuitManager.transform)
        {
            if (circuitos.gameObject.activeSelf)
            {
                activeCircuit = circuitos;
                break;
            }
        }
        for (int i = 0; i < 6; i++)
        {
            runningPos.Add(activeCircuit.transform.Find("StartPos").GetChild(i).transform);
            SetPositionServerRpc(runningPos[i].position);
        }
        circuit = activeCircuit.gameObject.name;
    }

    [ServerRpc]
    public void SetPositionServerRpc(Vector3 pos)
    {
        playersPos.Add(pos);
        SetPositionClientRpc(pos);
    }
    
    [ClientRpc]
    public void SetPositionClientRpc(Vector3 pos)
    {
        playersPos.Add(pos);
    }

    public void GenerateBanner(ulong id, int playerID, float tiempoTotal, int kills, int deaths)
    {
        raceStarted = false;
        GenerateBannerServerRpc(id, playerID, tiempoTotal, kills, deaths);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GenerateBannerServerRpc(ulong id, int playerID, float tiempoTotal, int kills, int deaths)
    {
        ganadores.Add(players[playerID]);
        AddWinnersClientRpc(ganadores[ganadores.Count - 1].GetComponent<NetworkObject>().NetworkObjectId);
        Debug.Log("SERVER " + _networkManager.LocalClientId + " MUESTRA LOS GANADORES:");
        for (int i = 0; i < ganadores.Count; i++)
        {
            Debug.Log(ganadores[i].GetComponent<Player>().netName.Value);
            Debug.Log(ganadores[i].GetComponent<Player>().netTiempoTotal.Value);
        }
        string nombre = ganadores[ganadores.Count - 1].GetComponent<Player>().netName.Value.ToString();
        float tiempo = tiempoTotal;
        int eliminaciones = kills;
        int muertes = deaths;
        GenerateBannerClientRpc(id, nombre, tiempo, eliminaciones, muertes);
    }

    [ClientRpc]
    public void AddWinnersClientRpc(ulong winnerId)
    {
        if (IsServer) return;
        NetworkObject networkObject = _networkManager.SpawnManager.SpawnedObjects[winnerId];
        ganadores.Add(networkObject.gameObject);
        Debug.Log("CLIENTE " + _networkManager.LocalClientId +  " MUESTRA LOS GANADORES:");
        for (int i = 0; i < ganadores.Count; i++)
        {
            Debug.Log(ganadores[i].GetComponent<Player>().netName.Value);
            Debug.Log(ganadores[i].GetComponent<Player>().netTiempoTotal.Value);
        }
    }
    [ClientRpc]
    public void GenerateBannerClientRpc(ulong id, string nameToSet, float tiempoToSet, int killToSet, int deathsToSet)
    {
        if (raceStarted) return;

        UIManager.Instance.playerHUD.SetActive(false);
        camera.SetActive(true);

        if(ganadores.Count > 1)
        {
            for (int i = 0; i < ganadores.Count - 1; i++)
            {
                UIManager.Instance.bannersResultados[i].SetActive(true);
                UIManager.Instance.bannersResultados[i].transform.Find("Nombre").GetComponent<TextMeshProUGUI>().text = ganadores[i].GetComponent<Player>().netName.Value.ToString();

                float tiempo = ganadores[i].GetComponent<Player>().netTiempoTotal.Value;
                int minutos1 = (int)(tiempo / 60);
                int segundos1 = (int)(tiempo % 60);
                int centesimas1 = (int)((tiempo * 100) % 100);

                UIManager.Instance.bannersResultados[i].transform.Find("Tiempo").GetComponent<TextMeshProUGUI>().text = minutos1.ToString("00") + " : " + segundos1.ToString("00") + " : " + centesimas1.ToString("000");
                UIManager.Instance.bannersResultados[i].transform.Find("Kills").GetComponent<TextMeshProUGUI>().text = ganadores[i].GetComponent<Player>().netKills.Value.ToString("00");
                UIManager.Instance.bannersResultados[i].transform.Find("Muertes").GetComponent<TextMeshProUGUI>().text = ganadores[i].GetComponent<Player>().netDeaths.Value.ToString("00");
            }
        }

        UIManager.Instance.bannersResultados[ganadores.Count - 1].SetActive(true);
        UIManager.Instance.bannersResultados[ganadores.Count - 1].transform.Find("Nombre").GetComponent<TextMeshProUGUI>().text = nameToSet;
       
        int minutos = (int)(tiempoToSet / 60);
        int segundos = (int)(tiempoToSet % 60);
        int centesimas = (int)((tiempoToSet * 100) % 100);

        UIManager.Instance.bannersResultados[ganadores.Count - 1].transform.Find("Tiempo").GetComponent<TextMeshProUGUI>().text = minutos.ToString("00") + " : " + segundos.ToString("00") + " : " + centesimas.ToString("000");
        UIManager.Instance.bannersResultados[ganadores.Count - 1].transform.Find("Kills").GetComponent<TextMeshProUGUI>().text = killToSet.ToString("00");
        UIManager.Instance.bannersResultados[ganadores.Count - 1].transform.Find("Muertes").GetComponent<TextMeshProUGUI>().text = deathsToSet.ToString("00");
        //banner.transform.Find("Nombre").GetComponent<TextMeshProUGUI>().text = players[playerID].GetComponent<Player>().netName.Value.ToString();
        //
        //float tiempoTotal = players[playerID].GetComponent<Player>().netVuelta1.Value + players[playerID].GetComponent<Player>().netVuelta2.Value + players[playerID].GetComponent<Player>().netVuelta3.Value;
        //int minutos = (int)(tiempoTotal / 60);
        //int segundos = (int)(tiempoTotal % 60);
        //int centesimas = (int)((tiempoTotal * 100) % 100);
        //

        //banner.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        camera = GameObject.Find("CameraFinal").gameObject;
        camera.SetActive(false);
    }
}