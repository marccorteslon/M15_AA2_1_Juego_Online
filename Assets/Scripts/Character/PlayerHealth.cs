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

    // Puntuación y estado de muerte
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

    // Referencia estática al jugador local (tirador)
    public static PlayerHealth LocalPlayerInstance;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalPlayerInstance = this;

            // Asignar el RaycastLookAt del jugador local al UIManager
            UIManager ui = FindFirstObjectByType<UIManager>();
            if (ui != null)
            {
                ui.realAimLookAt = GetComponentInChildren<RaycastLookAt>();
                if (ui.cam == null)
                {
                    ui.cam = Camera.main;
                }
            }
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            score.Value = 0;
            isDead.Value = false;
        }

        // Suscribirse al estado de muerte
        isDead.OnValueChanged += OnDeadStateChanged;
        
        TogglePlayerState(!isDead.Value);
    }

    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeadStateChanged;
        
        if (IsOwner)
        {
            LocalPlayerInstance = null;
        }
    }

    private void OnDeadStateChanged(bool previousValue, bool newValue)
    {
        TogglePlayerState(!newValue);
    }

    private void TogglePlayerState(bool active)
    {
        // Ocultar/Mostrar visuales
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = active;
        }

        // Ocultar/Mostrar colisiones
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = active;
        }

        // Desactivar físicas al morir
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(int damage, ulong shooterClientId)
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

            // Sumar puntuación al asesino
            if (shooterClientId != OwnerClientId)
            {
                PlayerHealth killerHealth = null;
                
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client))
                {
                    if (client.PlayerObject != null)
                    {
                        killerHealth = client.PlayerObject.GetComponent<PlayerHealth>();
                    }
                }

                if (killerHealth == null)
                {
                    PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
                    foreach (PlayerHealth p in allPlayers)
                    {
                        if (p.OwnerClientId == shooterClientId)
                        {
                            killerHealth = p;
                            break;
                        }
                    }
                }

                if (killerHealth != null)
                {
                    killerHealth.score.Value += 100;
                    Debug.Log($"¡Jugador {shooterClientId} recibe +100 de puntuación por eliminar a Jugador {OwnerClientId}! Puntuación actual: {killerHealth.score.Value}");
                }
                else
                {
                    Debug.LogWarning($"No se encontró al asesino con ClientId: {shooterClientId} en la escena ni en la lista de clientes.");
                }
            }

            // Iniciar reaparición tras 3 segundos
            StartCoroutine(RespawnCoroutine());
        }
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);

        Vector3 spawnPos = Vector3.zero;

        // Buscar spawn points en la escena
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            spawnPos = spawnPoints[randomIndex].transform.position;
            Debug.Log($"[SERVIDOR] Reapareciendo a Jugador {OwnerClientId} en el SpawnPoint: '{spawnPoints[randomIndex].name}' ({spawnPos})");
        }
        else
        {
            // Fallback si no hay spawn points
            spawnPos = new Vector3(Random.Range(-8f, 8f), 1f, Random.Range(-8f, 8f));
            Debug.LogWarning($"[SERVIDOR] No se encontraron objetos con tag 'Respawn'. Usando posición fallback aleatoria: {spawnPos}");
        }

        RespawnClientRpc(spawnPos);

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

    [ServerRpc]
    public void ShootServerRpc(Vector3 position, Quaternion rotation)
    {
        ShootClientRpc(position, rotation, OwnerClientId);
    }

    [ClientRpc]
    private void ShootClientRpc(Vector3 position, Quaternion rotation, ulong shooterId)
    {
        // Instanciar la bala visualmente para los demás jugadores
        if (NetworkManager.Singleton.LocalClientId != shooterId)
        {
            GenericGun gun = GetComponentInChildren<GenericGun>();
            if (gun != null && gun.bullet != null)
            {
                GameObject bulletObject = Instantiate(gun.bullet, position, rotation);
                
                // Destruir Projectile en la bala visual para que no haga daño real
                Projectile projectile = bulletObject.GetComponentInChildren<Projectile>();
                if (projectile != null)
                {
                    Destroy(projectile);
                }

                Collider col = bulletObject.GetComponentInChildren<Collider>();
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
        if (!IsSpawned || !IsOwner) return;

        // Evitar UI duplicada de clones manuales
        if (NetworkManager.Singleton != null && 
            NetworkManager.Singleton.LocalClient != null && 
            NetworkManager.Singleton.LocalClient.PlayerObject != NetworkObject)
        {
            return;
        }

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
