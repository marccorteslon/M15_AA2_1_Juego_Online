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
    public LayerMask layers; // Conservada por compatibilidad en inspector
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

        // Usamos SphereCastAll para obtener TODOS los impactos en la trayectoria de la bala.
        // Esto evita que si la bala "nace" dentro del colisionador del propio tirador, el raycast se bloquee y no detecte nada más.
        RaycastHit[] hits = Physics.SphereCastAll(lastPos, radius, dir.normalized, dir.magnitude);
        
        foreach (RaycastHit hit in hits)
        {
            PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();

            if (targetHealth != null)
            {
                // Ignorar al propio tirador que disparó la bala
                if (targetHealth.OwnerClientId == shooterClientId)
                {
                    continue; // Sigue buscando en la trayectoria
                }

                Hitted(hit, targetHealth);
                break; // Detener bala en el primer jugador válido
            }
            else
            {
                // Si es un trigger y no es un jugador, lo ignoramos y dejamos que la bala continúe
                if (hit.collider.isTrigger) continue;

                Hitted(hit, null);
                break; // Detener bala contra el entorno (paredes, etc.)
            }
        }

        lastPos = transform.position;
    }

    void Hitted(RaycastHit hit, PlayerHealth targetHealth)
    {
        if (targetHealth != null)
        {
            // Buscar al jugador local (el dueño que disparó) para lanzar el ServerRpc desde SU objeto
            PlayerHealth localShooter = null;
            PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (PlayerHealth p in players)
            {
                if (p.IsOwner)
                {
                    localShooter = p;
                    break;
                }
            }

            if (localShooter != null)
            {
                Debug.Log($"¡Bala golpea a jugador: {targetHealth.name}! Enviando solicitud de daño al servidor.");
                localShooter.DealDamageServerRpc(targetHealth.OwnerClientId, damage);
            }
        }
        else
        {
            Debug.Log($"Bala golpea entorno: {hit.collider.name}");
        }

        // Destruir la bala tras el impacto
        Destroy(gameObject);
    }
}
