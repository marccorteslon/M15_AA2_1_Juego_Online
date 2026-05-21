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
        Vector3 dir = transform.position - lastPos;

        Debug.DrawRay(lastPos, dir, Color.blue, disappearTime);

        RaycastHit hit;

        if (Physics.SphereCast(lastPos, radius, dir.normalized, out hit, dir.magnitude, layers))
        {
            Hitted(hit);
        }

        lastPos = transform.position;
    }

    void Hitted(RaycastHit hit)
    {
        Debug.Log($"La bala golpeó: {hit.collider.name}");
        Debug.Log($"Objeto padre: {hit.collider.transform.root.name}");

        PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            Debug.Log($"Disparo detectado contra jugador: {health.name}");
            health.TakeDamageServerRpc(damage, shooterClientId);
        }
        else
        {
            Debug.LogWarning(
                $"El objeto golpeado no tiene PlayerHealth. " +
                $"Collider: {hit.collider.name}, Root: {hit.collider.transform.root.name}"
            );
        }

        Destroy(gameObject);
    }
}