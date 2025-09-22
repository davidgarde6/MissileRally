using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionChecker : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public Transform player;
    private Vector3[] linePositions;

    void OnEnable()
    {
        lineRenderer = GetLineRenderer();

        linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);
    }

    public LineRenderer GetLineRenderer()
    {
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

        return activeCircuit.gameObject.GetComponent<LineRenderer>();
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.raceStarted) return;
        // Encuentra el vértice más cercano al jugador
        int closestVertexIndex = 0;
        float closestDistance = Vector3.Distance(player.position, linePositions[0]);

        for (int i = 1; i < linePositions.Length; i++)
        {
            float distance = Vector3.Distance(player.position, linePositions[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestVertexIndex = i;
            }
        }

        // Encuentra el siguiente vértice
        int nextVertexIndex = (closestVertexIndex + 1) % linePositions.Length;

        // Define el segmento
        Vector3 segmentStart = linePositions[closestVertexIndex];
        Vector3 segmentEnd = linePositions[nextVertexIndex];
        Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;

        // Compara el vector dirección del segmento con el vector forward del transform del jugador
        float angle = Vector3.Angle(segmentDirection, player.forward);

        if (angle > 110.0f)
        {
            GameObject.Find("PlayerUI").transform.Find("Sentido Contrario/Texto").gameObject.SetActive(true);
        }
        else if (GameObject.Find("PlayerUI").transform.Find("Sentido Contrario/Texto").gameObject.activeSelf)
        {
            GameObject.Find("PlayerUI").transform.Find("Sentido Contrario/Texto").gameObject.SetActive(false);
        }
    }
}