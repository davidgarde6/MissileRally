using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MisilController : NetworkBehaviour
{
    public Rigidbody rg;
    public float vel;
    public Vector3 forward;

    void Start()
    {
        forward = rg.transform.forward;
    }

    void FixedUpdate()
    {
        rg.velocity = forward * vel * Time.fixedDeltaTime;
    }

    void OnCollisionEnter(Collision col)
    {
        GameObject explosion = GameObject.Find("Explosion");
        explosion.SetActive(true);
        Instantiate(explosion, this.gameObject.transform.position, this.gameObject.transform.rotation);
        Destroy(this.gameObject);

        if(col.gameObject.tag == "Player")
        {
            Player player = GameManager.Instance.player.GetComponent<Player>();
            if (col.gameObject.GetComponentInParent<Player>().netVulnerable.Value)
            {
                GameManager.Instance.UpdateKillsServerRpc(player.netID.Value);
                col.gameObject.GetComponentInParent<Player>().SetVulnerable(col.gameObject.GetComponentInParent<Player>().netID.Value);
            }
        }
    }
}
