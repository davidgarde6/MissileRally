using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using Unity.Collections;
using System;

public class Player : NetworkBehaviour
{
    CarController carController;
    NetworkManager _networkManager;
    public InvencibilityController invencible;
    // Player Info
    public string Name { get; set; }
    public int ID { get; set; }
    public Color color;
    [SerializeField] private TextMeshProUGUI nombreJugador;
    [SerializeField] private Transform carTransform;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private const float overturnedAngleThreshold = 45f;

    // Variables de red
    public NetworkVariable<FixedString128Bytes> netName;
    public NetworkVariable<int> netID;
    public NetworkVariable<int> netCurrentPosition;
    public NetworkVariable<Vector3> netPosition;
    public NetworkVariable<int> netKills;
    public NetworkVariable<int> netDeaths;
    public NetworkVariable<Color> netColor;
    public NetworkVariable<float> netSpeed;
    public NetworkVariable<int> netCurrentLap;
    public NetworkVariable<float> netVuelta1;
    public NetworkVariable<float> netVuelta2;
    public NetworkVariable<float> netVuelta3;
    public NetworkVariable<float> netTiempoTotal;
    public NetworkVariable<bool> netInLap1;
    public NetworkVariable<bool> netInLap2;
    public NetworkVariable<bool> netInLap3;
    public NetworkVariable<bool> netInLap4;
    public NetworkVariable<int> netNumTercios;
    public NetworkVariable<float> netLapCooldown;
    public NetworkVariable<bool> netVulnerable;

    // Race Info
    public GameObject car;
    public int currentPosition;
    public Vector3 position;
    public int kills;
    public int deaths;
    public float speed;
    public ulong NetcodeID;
    public int currentLap;
    public float tiempoVuelta1;
    public float tiempoVuelta2;
    public float tiempoVuelta3;
    public float tiempoTotal;
    public bool inLap1 = true;
    public bool inLap2 = false;
    public bool inLap3 = false;
    public bool inLap4 = false;
    public bool vulnerable = true;
    public int numTercios;
    public float lapCooldown;
    public float arcLength;

    public override void OnNetworkSpawn()
    {   
        base.OnNetworkSpawn();
        transform.Find("Misil").gameObject.SetActive(false);
        if (IsOwner)
        {
            UpdateValuesServerRpc(UIManager.Instance.name, UIManager.Instance.colorFinal);
        }
        nombreJugador.text = netName.Value.ToString();
        OnColorChange(Color.clear, netColor.Value);
    }

    [ServerRpc]
    void UpdateValuesServerRpc(string nameToSet, Color colorToSet)
    {
        netName.Value = nameToSet;
        netColor.Value = colorToSet;

    }

    [ClientRpc]
    public void ActivateCameraClientRpc()
    {
        if (!IsOwner) return;
        gameObject.transform.Find("Camera").gameObject.SetActive(true);
        GameObject.Find("CircuitManager").GetComponent<CircuitController>().enabled = true;
        GameObject.Find("CircuitManager").GetComponent<RaceController>().enabled = true;
        GetComponent<DirectionChecker>().enabled = true;
    }
    
    [ClientRpc]
    public void ActivateControlClientRpc()
    {
        if (!IsOwner) return;
        GetComponent<InputController>().ActivateControl();
        GameManager.Instance.raceStarted = true;
        StartInvencibilityServerRpc(netID.Value);
    }

    [ServerRpc]
    public void StartInvencibilityServerRpc(int idx)
    {
        StartInvencibilityClientRpc(idx);
    }
    
    [ClientRpc]
    public void StartInvencibilityClientRpc(int idx)
    {
        if (netID.Value != idx) return;
        netVulnerable.Value = true;
    }


    public void SetVulnerable(int idx) 
    {
        if(netID.Value != idx) return;
        netVulnerable.Value = false;
    }


    [ClientRpc]
    public void StartRaceClientRpc()
    {
        if (!IsOwner) return;
        GameManager.Instance.raceStarted = true;
    }

    [ServerRpc]
    public void ResetCarServerRpc()
    {
        carTransform.position = GameManager.Instance.runningPos[netID.Value].position;
        carTransform.rotation = startRotation;
        carTransform.GetComponent<Rigidbody>().velocity = Vector3.zero; // Detener el movimiento
        carTransform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero; // Detener la rotaci�n
        ResetCarClientRpc();
    }

    [ClientRpc]
    public void ResetCarClientRpc()
    {

        // Resetear la posici�n y rotaci�n del coche en todos los clientes
        if (IsServer) return;
        carTransform.position = GameManager.Instance.playersPos[netID.Value];
        carTransform.rotation = startRotation;
        carTransform.GetComponent<Rigidbody>().velocity = Vector3.zero; // Detener el movimiento
        carTransform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero; // Detener la rotaci�n
    }

    [ServerRpc]
    public void UpdatePlayerInfoServerRpc(string nameToSet, Color colorToSet)
    {
        netName.Value = nameToSet;
        netColor.Value = colorToSet;
    }

    public void OnEnable()
    {
        netID.OnValueChanged += OnIDChange;
        netCurrentPosition.OnValueChanged += OnCurrentPositionChange;
        netName.OnValueChanged += OnNameChange;
        netDeaths.OnValueChanged += OnDeathsChange;
        netKills.OnValueChanged += OnKillsChange;
        netColor.OnValueChanged += OnColorChange;
        netSpeed.OnValueChanged += OnSpeedChange;
        netCurrentLap.OnValueChanged += OnCurrentLapChange;
        netVuelta1.OnValueChanged += OnTiempoTotalChange;
        netInLap1.OnValueChanged += OnInLap1Change;
        netInLap2.OnValueChanged += OnInLap2Change;
        netInLap3.OnValueChanged += OnInLap3Change;
        netInLap4.OnValueChanged += OnInLap4Change;
        netVulnerable.OnValueChanged += OnVulnerabilityChange;
        netNumTercios.OnValueChanged += OnNumTercioChange;
        netPosition.OnValueChanged += OnPositionChange;
        netLapCooldown.OnValueChanged += OnLapCooldownChange;
    }

    public void OnDisable()
    {
        netID.OnValueChanged -= OnIDChange;
        netCurrentPosition.OnValueChanged -= OnCurrentPositionChange;
        netName.OnValueChanged -= OnNameChange;
        netDeaths.OnValueChanged -= OnDeathsChange;
        netKills.OnValueChanged -= OnKillsChange;
        netColor.OnValueChanged -= OnColorChange;
        netSpeed.OnValueChanged -= OnSpeedChange;
        netCurrentLap.OnValueChanged -= OnCurrentLapChange;
        netVuelta1.OnValueChanged -= OnTiempoTotalChange;
        netInLap1.OnValueChanged -= OnInLap1Change;
        netInLap2.OnValueChanged -= OnInLap2Change;
        netInLap3.OnValueChanged -= OnInLap3Change;
        netInLap4.OnValueChanged -= OnInLap4Change;
        netVulnerable.OnValueChanged -= OnVulnerabilityChange;
        netNumTercios.OnValueChanged -= OnNumTercioChange;
        netPosition.OnValueChanged -= OnPositionChange;
        netLapCooldown.OnValueChanged += OnLapCooldownChange;
    }

    void Awake()
    {
        numTercios = 2;
        _networkManager = NetworkManager.Singleton;
        netKills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netDeaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netColor = new NetworkVariable<Color>(new Color(0.990566f, 0.3027768f, 0.3027768f), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netSpeed = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netCurrentLap = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netCurrentPosition = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        netTiempoTotal = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        netInLap1 = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netInLap2 = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netInLap3 = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netInLap4 = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netVulnerable = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        netNumTercios = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        netLapCooldown = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    void OnIDChange(int previousValue, int newValue)
    {
        ID = newValue;
    }
    
    void OnNameChange(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        Name = newValue.ToString();
        nombreJugador.text = Name;
    }
    
    void OnColorChange(Color previousValue, Color newValue)
    {
        color = newValue;
        gameObject.transform.Find("Car").Find("body").GetComponent<MeshRenderer>().materials[1].color = color;
    }

    void OnDeathsChange(int previousValue, int newValue)
    {
        if (!IsOwner) return;
        deaths = newValue;
        GameObject.Find("PlayerUI").transform.Find("Muertes/Cantidad").GetComponent<TextMeshProUGUI>().text = deaths.ToString();
    }

    void OnKillsChange(int previousValue, int newValue)
    {
        if (!IsOwner) return;
        kills = newValue;
        GameObject.Find("PlayerUI").transform.Find("Eliminaciones/Cantidad").GetComponent<TextMeshProUGUI>().text = kills.ToString();
    }

    void OnSpeedChange(float previousValue, float newValue)
    {
        if (!GameManager.Instance.raceStarted) return;
        if (!IsOwner) return;
        speed = newValue;
        GameObject.Find("PlayerUI").transform.Find("Velocidad/Cantidad").GetComponent<TextMeshProUGUI>().text = speed.ToString("F2");
    }
    
    void OnPositionChange(Vector3 previousValue, Vector3 newValue)
    {
        if (!IsOwner) return;
        position = newValue;
        gameObject.transform.position = position;
    }
    
    void OnCurrentLapChange(int previousValue, int newValue)
    {
        if (!IsOwner) return;
        currentLap = newValue;
    }
    
    void OnTiempoTotalChange(float previousValue, float newValue)
    {
        if (!IsOwner) return;
        tiempoTotal = newValue;
        
    }

    void OnInLap1Change(bool previousValue, bool newValue)
    {
        if (!IsOwner) return;
        inLap1 = newValue;
    }
    
    void OnInLap2Change(bool previousValue, bool newValue)
    {
        if (!IsOwner) return;
        inLap2 = newValue;
    }
    
    void OnInLap3Change(bool previousValue, bool newValue)
    {
        if (!IsOwner) return;
        inLap3 = newValue;
    }
    
    void OnInLap4Change(bool previousValue, bool newValue)
    {
        if (!IsOwner) return;
        inLap4 = newValue;
        if (tiempoVuelta3 < tiempoVuelta2)
        {
            UIManager.Instance.vuelta3.color = Color.green;
        }
        else if (tiempoVuelta3 > tiempoVuelta2)
        {
            UIManager.Instance.vuelta3.color = Color.red;
        }
    }
    
    void OnVulnerabilityChange(bool previousValue, bool newValue)
    {
        if (!IsOwner) return;
        vulnerable = newValue;

        if (!vulnerable)
        {
            invencible.StartProtection();
        }
        else
        {
            invencible.EndProtection();
        }

    }

    void OnCurrentPositionChange(int previousValue, int newValue)
    {
        if (!IsOwner) return;
        currentPosition = newValue;

        if(currentPosition == 1)
        {
            GameObject.Find("PlayerUI").transform.Find("Posicion/Numero").GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.7261318f, 0f, 1f);
        }else if(currentPosition == 2)                                                                             
        {                                                                                                          
            GameObject.Find("PlayerUI").transform.Find("Posicion/Numero").GetComponent<TextMeshProUGUI>().color = new Color(0.5754717f, 0.5754717f, 0.5754717f, 1f);
        }else if(currentPosition == 3)                                                                            
        {                                                                                                         
            GameObject.Find("PlayerUI").transform.Find("Posicion/Numero").GetComponent<TextMeshProUGUI>().color = new Color(0.509434f, 0.3419021f, 0.0945176f, 1f);
        }
        else
        {
            GameObject.Find("PlayerUI").transform.Find("Posicion/Numero").GetComponent<TextMeshProUGUI>().color = Color.white;
        }
        GameObject.Find("PlayerUI").transform.Find("Posicion/Numero").GetComponent<TextMeshProUGUI>().text = currentPosition.ToString() + "º";
    }

    void OnNumTercioChange(int previousValue, int newValue)
    {
        if (!IsOwner) return;
        numTercios = newValue;
    }
    
    void OnLapCooldownChange(float previousValue, float newValue)
    {
        if (!IsOwner) return;
        lapCooldown = newValue;
    }

    public override string ToString()
    {
        return Name;
    }

    private void ActualizarVelocidad(float velocidadToSet)
    {
        if (!IsServer) return;
        netSpeed.Value = velocidadToSet;
    }

    public bool IsCarOverturned()
    {
        // Verificar si el coche est� volcado
        float angle = Vector3.Angle(carTransform.up, Vector3.up);
        return angle > overturnedAngleThreshold;
    }

    private void Start()
    {
        invencible = transform.Find("Car/Invencibilidad").GetComponent<InvencibilityController>();
        invencible.gameObject.SetActive(false);

        GameManager.Instance.currentRace.AddPlayer(this);

        carController = gameObject.transform.Find("Car").GetComponent<CarController>();

        carController.OnSpeedChangeEvent += ActualizarVelocidad;
    }

    private void Update() {

        if (!IsOwner) return;
        if (GameManager.Instance.raceStarted)
        {
            if (inLap1)
            {
                tiempoVuelta1 += Time.deltaTime;

                int minutos = (int)(tiempoVuelta1 / 60);
                int segundos = (int)(tiempoVuelta1 % 60);
                int centesimas = (int)((tiempoVuelta1 * 100) % 100);
            
                UIManager.Instance.vuelta1.text = minutos.ToString("00") + " : " + segundos.ToString("00") + " : " + centesimas.ToString("000");
            }
            else if (inLap2)
            {
                tiempoVuelta2 += Time.deltaTime;
            
                UIManager.Instance.indicadorVueltas.text = "2/3";
                int minutos = (int)(tiempoVuelta2 / 60);
                int segundos = (int)(tiempoVuelta2 % 60);
                int centesimas = (int)((tiempoVuelta2 * 100) % 100);
            
                UIManager.Instance.vuelta2.text = minutos.ToString("00") + " : " + segundos.ToString("00") + " : " + centesimas.ToString("000");
            }
            else if (inLap3)
            {
                tiempoVuelta3 += Time.deltaTime;
            
                UIManager.Instance.indicadorVueltas.text = "3/3";
            
                if (tiempoVuelta2 < tiempoVuelta1)
                {
                    UIManager.Instance.vuelta2.color = Color.green;
                }
                else if (tiempoVuelta2 > tiempoVuelta1)
                {
                    UIManager.Instance.vuelta2.color = Color.red;
                }
                int minutos = (int)(tiempoVuelta3 / 60);
                int segundos = (int)(tiempoVuelta3 % 60);
                int centesimas = (int)((tiempoVuelta3 * 100) % 100);
            
                UIManager.Instance.vuelta3.text = minutos.ToString("00") + " : " + segundos.ToString("00") + " : " + centesimas.ToString("000");
            }
            else if (inLap4)
            {
                netTiempoTotal.Value = tiempoVuelta1 + tiempoVuelta2 + tiempoVuelta3;
                GameManager.Instance.GenerateBanner(_networkManager.LocalClientId, netID.Value, netTiempoTotal.Value,netKills.Value, netDeaths.Value);
            }
            if (UIManager.Instance.playersConnected == 1)
            {
                netTiempoTotal.Value = tiempoVuelta1 + tiempoVuelta2 + tiempoVuelta3;
                GameManager.Instance.GenerateBanner(_networkManager.LocalClientId, netID.Value, netTiempoTotal.Value, kills, deaths);
            }
        }
    }
}