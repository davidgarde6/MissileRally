using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILockAtCamera : MonoBehaviour
{
    [SerializeField] private Camera camara;
    void LateUpdate()
    {
        transform.LookAt(transform.position + camara.transform.forward);
    }
}
