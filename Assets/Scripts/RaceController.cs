using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Unity.Netcode;

public class RaceController : NetworkBehaviour
{
    public int numPlayers;

    private readonly List<Player> _players = new(4);
    private CircuitController _circuitController;
    private GameObject[] _debuggingSpheres;
    public float cooldown;

    private Dictionary<int, float> _distances = new Dictionary<int, float>();

    private void Start()
    {
        if (_circuitController == null) _circuitController = GetComponent<CircuitController>();

        _debuggingSpheres = new GameObject[GameManager.Instance.numPlayers];
        for (int i = 0; i < UIManager.Instance.netPlayersConnected.Value; ++i)
        {
            _debuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _debuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
        }
    }

    private void Update()
    {
        if (_players.Count == 0)
            return;

        if (GameManager.Instance.player.GetComponent<Player>().lapCooldown > 0)
        {
            GameManager.Instance.player.GetComponent<Player>().lapCooldown -= Time.deltaTime;
        }
        UpdateRaceProgress();
    }

    public void AddPlayer(Player player)
    {
        _players.Add(player);
    }

    private class PlayerInfoComparer : Comparer<Player>
    {
        readonly float[] _arcLengths;

        public PlayerInfoComparer(float[] arcLengths)
        {
            _arcLengths = arcLengths;
        }

        //compara dos jugadores (posiciones)
        public override int Compare(Player x, Player y)
        {
            if (_arcLengths[x.ID] > _arcLengths[y.ID])
            {
                return -1; //devuelve -1 si x está por delante de y 
            }
            else if (_arcLengths[x.ID] < _arcLengths[y.ID])
            {
                return 1; //devuelve 1 si y está por delante de x
            }
            else
            {
                return -1; //devuelve 0 si están a la par 
            }
        }
    }

    int ComparePlayers(float x, float y)
    {
        //// Primero compara la cantidad de vueltas completadas
        //int lapComparison = y.GetComponent<Player>().currentLap.CompareTo(x.GetComponent<Player>().currentLap);
        //
        //if (lapComparison == 0)
        //{
        //    // Si la cantidad de vueltas es la misma, compara la distancia recorrida en la vuelta actual
        //    return y.GetComponent<Player>().arcLength.CompareTo(x.GetComponent<Player>().arcLength);
        //}
        //else
        //{
        //    return lapComparison;
        //}

        if (x > y)
        {
            return -1; //devuelve -1 si x está por delante de y 
        }
        else if (x < y)
        {
            return 1; //devuelve 1 si y está por delante de x
        }
        else
        {
            return 0; //devuelve 0 si están a la par 
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RacersInfoServerRpc(int indx, float distance, int vuelta)
    {

        _distances[indx] = distance + _circuitController.CircuitLength * vuelta;

        // Ordenar los jugadores por distancia
        var sortedPlayers = _distances.OrderByDescending(kv => kv.Value).ToArray();

        // Notificar a los clientes la posición ordenada
        for (int i = 0; i < sortedPlayers.Length; i++)
        {
            RacersInfoClientRpc(sortedPlayers[i].Value, sortedPlayers[i].Key, i + 1); // Envía posición basada en el orden

        }
    }

    [ClientRpc]
    public void RacersInfoClientRpc(float distance, int playerId, int position)
    {
        // Actualizar la posición del jugador local
        if (playerId == GameManager.Instance.player.GetComponent<Player>().netID.Value)
        {
            GameManager.Instance.player.GetComponent<Player>().netCurrentPosition.Value = position;
        }
    }

    public void UpdateRaceProgress()
    {
        // Update car arc-lengths
        GameManager.Instance.player.GetComponent<Player>().arcLength = ComputeCarArcLength();

        RacersInfoServerRpc(GameManager.Instance.player.GetComponent<Player>().netID.Value, GameManager.Instance.player.GetComponent<Player>().arcLength, GameManager.Instance.player.GetComponent<Player>().currentLap);

        if (GameManager.Instance.player.GetComponent<Player>().arcLength > _circuitController.CircuitLength - 1f && GameManager.Instance.player.GetComponent<Player>().lapCooldown <= 0 && GameManager.Instance.player.GetComponent<Player>().numTercios == 2)
        {
            GameManager.Instance.player.GetComponent<Player>().lapCooldown = 5f;
            GameManager.Instance.player.GetComponent<Player>().numTercios = 0;
            GameManager.Instance.player.GetComponent<Player>().currentLap++;

            if (GameManager.Instance.player.GetComponent<Player>().currentLap == 2)
            {
                GameManager.Instance.player.GetComponent<Player>().inLap1 = false;
                GameManager.Instance.player.GetComponent<Player>().inLap2 = true;
            }
            else if (GameManager.Instance.player.GetComponent<Player>().currentLap == 3)
            {
                GameManager.Instance.player.GetComponent<Player>().inLap2 = false;
                GameManager.Instance.player.GetComponent<Player>().inLap3 = true;
            }
            else if (GameManager.Instance.player.GetComponent<Player>().currentLap == 4)
            {
                GameManager.Instance.player.GetComponent<Player>().inLap3= false;
                GameManager.Instance.player.GetComponent<Player>().inLap4= true;
            }
        }

        if (GameManager.Instance.player.GetComponent<Player>().arcLength >= (_circuitController.CircuitLength / 3) - 1f  && GameManager.Instance.player.GetComponent<Player>().numTercios == 0 && GameManager.Instance.player.GetComponent<Player>().lapCooldown <= 0)
        {
            GameManager.Instance.player.GetComponent<Player>().numTercios = 1;
        }else if (GameManager.Instance.player.GetComponent<Player>().arcLength >= (_circuitController.CircuitLength  - (_circuitController.CircuitLength / 3)) - 1f && GameManager.Instance.player.GetComponent<Player>().numTercios == 1 && GameManager.Instance.player.GetComponent<Player>().lapCooldown <= 0)
        {
            GameManager.Instance.player.GetComponent<Player>().numTercios = 2;
        }

            //foreach (var player in _players)
            //{
            //    //Debug.Log("PRIMER TERCIO EN: " + _circuitController.CircuitLength / 3);
            //    //float third = _circuitController.CircuitLength - _circuitController.CircuitLength / 3;
            //    //Debug.Log("TERCER TERCIO EN: " + third);

            //    if (arcLengths[player.ID] > _circuitController.CircuitLength - 1f && player.lapCooldown <= 0 && player.numTercios == 2)
            //    {
            //        player.lapCooldown = 5f;
            //        player.numTercios = 0;
            //        player.currentLap++;

            //        if (player.currentLap == 2)
            //        {
            //            //Debug.Log("VUELTA 2");
            //            player.inLap1 = false;
            //            player.inLap2 = true;
            //        }
            //        else if (player.currentLap == 3)
            //        {
            //            //Debug.Log("VUELTA 3");
            //            player.inLap2 = false;
            //            player.inLap3 = true;
            //        }
            //        else if (player.currentLap == 4)
            //        {
            //            //Debug.Log("FIN CARRERA");
            //            player.inLap3= false;
            //            player.inLap4= true;
            //        }
            //    }

            //    if (arcLengths[player.ID] >= (_circuitController.CircuitLength / 3) - 1f && player.numTercios == 0 && cooldown <= 0)
            //    {
            //        player.numTercios = 1;
            //    }else if (arcLengths[player.ID] >= (_circuitController.CircuitLength  - (_circuitController.CircuitLength / 3)) - 1f && player.numTercios == 1 && cooldown <= 0)
            //    {
            //        player.numTercios = 2;
            //    }
            //    //Debug.Log(arcLengths[player.ID]);
            //    //Debug.Log(player.netNumTercios.Value);
            //    //Debug.Log(cooldown);

            //    //if(player.currentLap == 0)
            //    //{
            //    //    if (arcLengths[player.ID] > -0.1 && cooldown <= 0)
            //    //    {
            //    //        cooldown = 5f;
            //    //        Debug.Log("ENTRA1");
            //    //        player.currentLap++;
            //    //    }
            //    //}else if (arcLengths[player.ID] > _circuitController.CircuitLength *(player.currentLap - 1) - 0.1 && cooldown <= 0)
            //    //{
            //    //    cooldown = 5f;
            //    //    Debug.Log("ENTRA2");
            //    //    player.currentLap++;
            //    //    if (player.currentLap == 2)
            //    //    {
            //    //        player.inLap1 = false;
            //    //        player.inLap2 = true;
            //    //    }
            //    //    else if (player.currentLap == 3)
            //    //    {
            //    //        player.inLap2 = false;
            //    //        player.inLap3 = true;
            //    //    }
            //    //}
            //}
    }

    float ComputeCarArcLength()
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = GameManager.Instance.player.GetComponent<Player>().car.transform.position;


        float minArcL =
            this._circuitController.ComputeClosestPointArcLength(carPos, out _, out var carProj, out _);

        this._debuggingSpheres[GameManager.Instance.player.GetComponent<Player>().ID].transform.position = carProj;

        //if (this._players[id].currentLap == 0)
        //{
        //    minArcL -= _circuitController.CircuitLength;
        //}
        //else
        //{
        //    minArcL += _circuitController.CircuitLength *
        //               (_players[id].currentLap - 1);
        //}

        return minArcL;
    }
}