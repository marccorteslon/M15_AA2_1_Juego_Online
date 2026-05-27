using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // NUEVAS VARIABLES PARA PUNTUACIÓN Y ESTADO DE MUERTE
    public NetworkVariable<int> score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            score.Value = 0;
            isDead.Value = false;
        }

        // Suscribirse al cambio de estado de muerte para ocultar/mostrar el personaje
        isDead.OnValueChanged += OnDeadStateChanged;
        
        // Ejecutar inicialmente por si acaso ya estuviese muerto al spawnear
        TogglePlayerState(!isDead.Value);
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeadStateChanged;
    }

    private void OnDeadStateChanged(bool previousValue, bool newValue)
    {
        // Si newValue es true (está muerto), desactivamos físicas y visuales. Si es false, las activamos.
        TogglePlayerState(!newValue);
    }

    private void TogglePlayerState(bool active)
    {
        // Ocultar/Mostrar visuales (SkinnedMeshRenderers, MeshRenderers, etc.)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = active;
        }

        // Ocultar/Mostrar colisionadores para no poder dispararle ni chocar con él
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = active;
        }

        // Desactivar gravedad y físicas si está muerto para que no caiga al vacío
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = active;
            rb.isKinematic = !active;
            if (!active)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void DealDamageServerRpc(ulong targetClientId, int damage)
    {
        // Esto se ejecuta en el servidor. Buscamos al jugador dañado de forma ultra robusta.
        PlayerHealth targetHealth = null;
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth p in allPlayers)
        {
            if (p.OwnerClientId == targetClientId)
            {
                targetHealth = p;
                break;
            }
        }

        // Si existe en el servidor, le aplicamos el daño
        if (targetHealth != null)
        {
            targetHealth.ApplyDamageFromServer(damage, OwnerClientId);
        }
    }

    public void ApplyDamageFromServer(int damage, ulong shooterClientId)
    {
        // Esto se ejecuta estrictamente en el servidor
        if (isDead.Value || currentHealth.Value <= 0) return;

        currentHealth.Value -= damage;

        Debug.Log(
            $"[SERVIDOR] Jugador {OwnerClientId} recibió {damage} puntos de daño por parte de Jugador {shooterClientId}. " +
            $"Vida restante: {currentHealth.Value}"
        );

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            isDead.Value = true;

            // PUNTUACIÓN: Buscar al asesino y sumarle +100 puntos de forma robusta en el servidor
            if (shooterClientId != OwnerClientId)
            {
                PlayerHealth killerHealth = null;
                PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
                foreach (PlayerHealth p in allPlayers)
                {
                    if (p.OwnerClientId == shooterClientId)
                    {
                        killerHealth = p;
                        break;
                    }
                }

                if (killerHealth != null)
                {
                    killerHealth.score.Value += 100;
                    Debug.Log($"¡Jugador {shooterClientId} recibe +100 de puntuación por eliminar a Jugador {OwnerClientId}! Puntuación actual: {killerHealth.score.Value}");
                }
                else
                {
                    Debug.LogWarning($"No se encontró al asesino con ClientId: {shooterClientId} en la escena.");
                }
            }

            // Iniciar reaparición tras 3 segundos
            StartCoroutine(RespawnCoroutine());
        }
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);

        // Posición de respawn aleatoria cerca del centro del mapa
        Vector3 randomPos = new Vector3(Random.Range(-8f, 8f), 1f, Random.Range(-8f, 8f));

        // Teletransportar al cliente a través de una ClientRpc dirigida al dueño (Client-Side Authority)
        RespawnClientRpc(randomPos);

        // Restaurar salud y quitar estado de muerte en el servidor
        currentHealth.Value = maxHealth;
        isDead.Value = false;

        Debug.Log($"Jugador {OwnerClientId} ha reaparecido.");
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPosition)
    {
        if (IsOwner)
        {
            transform.position = spawnPosition;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    // RPCs PARA REPLICACIÓN DE DISPAROS (SINCRONIZACIÓN VISUAL DE BALAS)
    [ServerRpc]
    public void ShootServerRpc(Vector3 position, Quaternion rotation)
    {
        ShootClientRpc(position, rotation, OwnerClientId);
    }

    [ClientRpc]
    private void ShootClientRpc(Vector3 position, Quaternion rotation, ulong shooterId)
    {
        // Si no somos el tirador, instanciamos la bala visualmente
        if (NetworkManager.Singleton.LocalClientId != shooterId)
        {
            GenericGun gun = GetComponentInChildren<GenericGun>();
            if (gun != null && gun.bullet != null)
            {
                GameObject bulletObject = Instantiate(gun.bullet, position, rotation);
                
                // Desactivar el daño y la colisión física para la bala puramente visual
                Projectile projectile = bulletObject.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.enabled = false;
                }

                Collider col = bulletObject.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }

                Destroy(bulletObject, 10);
            }
        }
    }

    private void OnGUI()
    {
        // HUD visible únicamente para el jugador propietario
        if (!IsOwner) return;

        if (isDead.Value)
        {
            // Mensaje de muerte en el centro de la pantalla
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 30, 300, 60), "HAS MUERTO");
            GUI.Label(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 5, 280, 25), "Reapareciendo en 3 segundos...", new GUIStyle { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.red } });
            return;
        }

        // HUD simple de salud y puntuación
        GUI.Box(new Rect(20, 20, 200, 80), "ESTADO DEL JUGADOR");
        GUI.Label(new Rect(30, 45, 180, 20), "Vida: " + currentHealth.Value + " / " + maxHealth);
        GUI.Label(new Rect(30, 65, 180, 20), "Puntuación: " + score.Value);
    }
}
