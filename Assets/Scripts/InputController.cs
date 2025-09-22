using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class InputController : NetworkBehaviour
{
    public static InputController Instance;
    private CarController car;
    public Animator animator;
    public GameObject misil;

    private bool canShoot;
    private float shootCooldown;

    void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        car = GetComponent<Player>().car.GetComponent<CarController>();
    }

    public void ActivateControl()
    {
        GetComponent<PlayerInput>().enabled = true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        var input = context.ReadValue<Vector2>();
        OnMoveServerRpc(input);
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        var input = context.ReadValue<float>();
        OnBrakeServerRpc(context.ReadValue<float>());
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (animator == null)
        {
            animator = GameObject.Find("PlayerUI").transform.Find("Misiles/Icono/Anim").GetComponent<Animator>();
        }

        if (canShoot)
        {
            Vector3 spawnPosition = GetComponent<Player>().car.transform.position + GetComponent<Player>().car.transform.forward * 2f + Vector3.up * 0.5f;

            GameObject instanciaMisil = Instantiate(GameManager.Instance._misilPrefab, spawnPosition, Quaternion.Euler(0, GetComponent<Player>().car.transform.rotation.eulerAngles.y, 0));
            instanciaMisil.SetActive(true);
            animator.Play("RecargaCohete");
            canShoot = false;
            shootCooldown = 1f;
            OnAttackServerRpc();
        }
    }

    public void OnReset(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsOwner) return;
        Player player = GetComponent<Player>();
        if (player.IsCarOverturned())
        {
            player.ResetCarServerRpc();
        }
    }


    [ServerRpc]
    public void OnMoveServerRpc(Vector2 input)
    {
        car.InputAcceleration = input.y;
        car.InputSteering = input.x;

        OnMoveClientRpc(input);
    }

    [ClientRpc]
    public void OnMoveClientRpc(Vector2 input)
    {
        car.InputAcceleration = input.y;
        car.InputSteering = input.x;
    }

    [ServerRpc]
    public void OnBrakeServerRpc(float input)
    {
        car.InputBrake = input;

        OnBrakeClientRpc(input);
    }

    [ClientRpc]
    public void OnBrakeClientRpc(float input)
    {
        car.InputBrake = input;
    }

    [ServerRpc]
    public void OnAttackServerRpc()
    {
        OnAttackClientRpc();
    }

    [ClientRpc]
    public void OnAttackClientRpc()
    {
        if (canShoot)
        {
            Vector3 spawnPosition = GetComponent<Player>().car.transform.position + GetComponent<Player>().car.transform.forward * 2f + Vector3.up * 0.5f;

            GameObject instanciaMisil = Instantiate(GameManager.Instance._misilPrefab, spawnPosition, Quaternion.Euler(0, GetComponent<Player>().car.transform.rotation.eulerAngles.y, 0));
            instanciaMisil.SetActive(true);
        }
    }

    void FixedUpdate()
    {
        if (!IsSpawned) return;
        
        if (shootCooldown > 0)
        {
            shootCooldown -= Time.fixedDeltaTime;
        }
        else
        {
            canShoot = true;
        }
        
    }
}