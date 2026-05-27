using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public int damage = 21;

    public float disappearTime = 5f;
    public Vector3 forceMin = new Vector3(-1, -1, 50);
    public Vector3 forceMax = new Vector3(1, 1, 100);
    public LayerMask layers;
    public float collisionForceMultiplier = 2f;
    public float radius = .1f;
    public GameObject spawnOnCollide;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public ulong shooterClientId;

    Vector3 lastPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.AddRelativeForce(new Vector3(
            Random.Range(forceMin.x, forceMax.x),
            Random.Range(forceMin.y, forceMax.y),
            Random.Range(forceMin.z, forceMax.z)
        ));

        Destroy(gameObject, disappearTime);
        lastPos = transform.position;
        transform.parent = null;
    }

    private void FixedUpdate()
    {
        // Omitir detección en las balas visuales de otros jugadores
        if (shooterClientId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        Vector3 dir = transform.position - lastPos;

        Debug.DrawRay(lastPos, dir, Color.blue, disappearTime);

        RaycastHit hit;

        if (Physics.SphereCast(lastPos, radius, dir.normalized, out hit, dir.magnitude))
        {
            Hitted(hit);
        }

        lastPos = transform.position;
    }

    void Hitted(RaycastHit hit)
    {
        PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();

        if (targetHealth != null)
        {
            if (targetHealth.OwnerClientId == shooterClientId)
            {
                return;
            }

            targetHealth.TakeDamageServerRpc(damage, shooterClientId);
        }
        else
        {
            if (hit.collider.isTrigger) return;
            Debug.Log($"Bala golpea entorno: {hit.collider.name}");
        }

        Destroy(gameObject);
    }
}
